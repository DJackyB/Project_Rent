using System;
using System.Collections.Generic;
using System.Globalization;
using Martian.Save;
using UnityEngine;

namespace Martian.Save.Runtime
{
    public sealed class SaveService
    {
        private readonly ISaveSerializer _serializer;
        private readonly ISaveStorage _storage;
        private readonly SaveRuntimeOptions _options;

        public SaveService(ISaveSerializer serializer, ISaveStorage storage, SaveRuntimeOptions options = null)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _options = options ?? new SaveRuntimeOptions();
        }

        public bool Save(string relativePath, string slotId, string displayName, IReadOnlyList<ISaveSection> sections, out string error)
        {
            error = null;

            if (!TryBuildEnvelope(slotId, displayName, sections, out var envelope, out error))
            {
                return false;
            }

            try
            {
                var json = _serializer.Serialize(envelope, _options.prettyPrintJson);
                _storage.WriteAllText(relativePath, json, _options.backupFileExtension);
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to save '{slotId}': {exception.Message}";
                return false;
            }
        }

        public bool TryPrepareLoad(
            string relativePath,
            IReadOnlyList<ISaveSection> sections,
            out SaveEnvelope envelope,
            out IReadOnlyList<PreparedSaveSectionState> preparedSections,
            out string error)
        {
            envelope = null;
            preparedSections = null;
            error = null;

            if (sections == null || sections.Count == 0)
            {
                error = "No save sections were provided.";
                return false;
            }

            if (!_storage.Exists(relativePath))
            {
                error = $"Save file not found: {relativePath}";
                return false;
            }

            try
            {
                var rawJson = _storage.ReadAllText(relativePath);
                envelope = _serializer.Deserialize<SaveEnvelope>(rawJson);
            }
            catch (Exception exception)
            {
                error = $"Failed to read save file '{relativePath}': {exception.Message}";
                return false;
            }

            if (!TryValidateEnvelope(envelope, out error))
            {
                return false;
            }

            var recordLookup = new Dictionary<string, SaveSectionRecord>(StringComparer.Ordinal);
            for (int i = 0; i < envelope.sections.Count; i++)
            {
                var record = envelope.sections[i];
                if (record == null || string.IsNullOrWhiteSpace(record.key))
                {
                    error = $"Save file '{relativePath}' contains an invalid section entry.";
                    return false;
                }

                if (!recordLookup.TryAdd(record.key, record))
                {
                    error = $"Save file '{relativePath}' contains duplicate section '{record.key}'.";
                    return false;
                }
            }

            var prepared = new List<PreparedSaveSectionState>(sections.Count);
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section == null)
                {
                    error = "Encountered a null save section.";
                    return false;
                }

                if (!recordLookup.TryGetValue(section.Key, out var record))
                {
                    if (section.IsRequired)
                    {
                        error = $"Missing required save section '{section.Key}'.";
                        return false;
                    }

                    continue;
                }

                object state;
                try
                {
                    state = _serializer.Deserialize(record.jsonPayload, section.StateType);
                }
                catch (Exception exception)
                {
                    error = $"Failed to deserialize section '{section.Key}': {exception.Message}";
                    return false;
                }

                if (state == null)
                {
                    error = $"Section '{section.Key}' could not be deserialized.";
                    return false;
                }

                if (!section.TryValidate(state, out error))
                {
                    error = string.IsNullOrWhiteSpace(error)
                        ? $"Section '{section.Key}' failed validation."
                        : error;
                    return false;
                }

                prepared.Add(new PreparedSaveSectionState(section, state));
            }

            preparedSections = prepared;
            return true;
        }

        public bool ApplyPreparedLoad(IReadOnlyList<PreparedSaveSectionState> preparedSections, out string error)
        {
            error = null;

            if (preparedSections == null)
            {
                error = "No prepared save sections were provided.";
                return false;
            }

            try
            {
                for (int i = 0; i < preparedSections.Count; i++)
                {
                    var preparedSection = preparedSections[i];
                    preparedSection.Section.ApplyState(preparedSection.State);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to apply save state: {exception.Message}";
                return false;
            }
        }

        public IReadOnlyList<SaveSlotSummary> ListSlots(string relativeDirectory)
        {
            var files = _storage.ListFiles(relativeDirectory, _options.saveFileExtension);
            var results = new List<SaveSlotSummary>(files.Count);

            for (int i = 0; i < files.Count; i++)
            {
                if (!TryReadEnvelope(files[i], out var envelope, out _))
                {
                    continue;
                }

                results.Add(new SaveSlotSummary
                {
                    slotId = envelope.slotId,
                    displayName = string.IsNullOrWhiteSpace(envelope.displayName) ? envelope.slotId : envelope.displayName,
                    savedAtUtc = envelope.savedAtUtc,
                    schemaVersion = envelope.schemaVersion,
                    appVersion = envelope.appVersion
                });
            }

            results.Sort((left, right) => CompareSavedTimes(right.savedAtUtc, left.savedAtUtc));
            return results;
        }

        public bool Delete(string relativePath, out string error)
        {
            error = null;

            try
            {
                _storage.Delete(relativePath, _options.backupFileExtension);
                return true;
            }
            catch (Exception exception)
            {
                error = $"Failed to delete save '{relativePath}': {exception.Message}";
                return false;
            }
        }

        public bool TryReadEnvelope(string relativePath, out SaveEnvelope envelope, out string error)
        {
            envelope = null;
            error = null;

            if (!_storage.Exists(relativePath))
            {
                error = $"Save file not found: {relativePath}";
                return false;
            }

            try
            {
                envelope = _serializer.Deserialize<SaveEnvelope>(_storage.ReadAllText(relativePath));
            }
            catch (Exception exception)
            {
                error = $"Failed to read save file '{relativePath}': {exception.Message}";
                return false;
            }

            return TryValidateEnvelope(envelope, out error);
        }

        private bool TryBuildEnvelope(string slotId, string displayName, IReadOnlyList<ISaveSection> sections, out SaveEnvelope envelope, out string error)
        {
            envelope = null;
            error = null;

            if (string.IsNullOrWhiteSpace(slotId))
            {
                error = "slotId must not be empty.";
                return false;
            }

            if (sections == null || sections.Count == 0)
            {
                error = "No save sections were provided.";
                return false;
            }

            envelope = new SaveEnvelope
            {
                schemaVersion = _options.currentSchemaVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                slotId = slotId,
                displayName = string.IsNullOrWhiteSpace(displayName) ? slotId : displayName,
                appVersion = Application.version
            };

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section == null)
                {
                    error = "Encountered a null save section.";
                    return false;
                }

                if (!keys.Add(section.Key))
                {
                    error = $"Duplicate save section key '{section.Key}'.";
                    return false;
                }

                object state;
                try
                {
                    state = section.CaptureState();
                }
                catch (Exception exception)
                {
                    error = $"Failed to capture section '{section.Key}': {exception.Message}";
                    return false;
                }

                if (state == null)
                {
                    error = $"Section '{section.Key}' returned a null state.";
                    return false;
                }

                envelope.sections.Add(new SaveSectionRecord
                {
                    key = section.Key,
                    jsonPayload = _serializer.Serialize(state, _options.prettyPrintJson)
                });
            }

            return true;
        }

        private bool TryValidateEnvelope(SaveEnvelope envelope, out string error)
        {
            error = null;

            if (envelope == null)
            {
                error = "Save envelope is missing.";
                return false;
            }

            if (envelope.schemaVersion > _options.currentSchemaVersion)
            {
                error = $"Save schema {envelope.schemaVersion} is newer than supported schema {_options.currentSchemaVersion}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(envelope.slotId))
            {
                error = "Save envelope is missing slotId.";
                return false;
            }

            if (envelope.sections == null)
            {
                error = "Save envelope is missing sections.";
                return false;
            }

            return true;
        }

        private static int CompareSavedTimes(string left, string right)
        {
            var hasLeft = DateTime.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var leftTime);
            var hasRight = DateTime.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var rightTime);

            if (!hasLeft && !hasRight)
            {
                return 0;
            }

            if (!hasLeft)
            {
                return -1;
            }

            if (!hasRight)
            {
                return 1;
            }

            return leftTime.CompareTo(rightTime);
        }
    }
}
