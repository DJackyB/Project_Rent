using System;
using BaoZuPo.UI;
using Martian.Save;
using Martian.Save.Runtime;

namespace BaoZuPo.Save
{
    public sealed class SettingsSaveFacade
    {
        private const string SettingsSlotId = "settings";
        private const string SettingsDisplayName = "Settings";

        private readonly SaveService _saveService;
        private readonly string _relativePath;

        public SettingsSaveFacade(SaveService saveService, string relativePath = "settings/app-settings.json")
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _relativePath = string.IsNullOrWhiteSpace(relativePath) ? "settings/app-settings.json" : relativePath;
        }

        public static SettingsSaveFacade Shared { get; private set; } = CreateDefault();

        public bool Save(out string error)
        {
            return _saveService.Save(_relativePath, SettingsSlotId, SettingsDisplayName, BuildSections(), out error);
        }

        public bool Load(out string error)
        {
            if (!_saveService.TryPrepareLoad(_relativePath, BuildSections(), out _, out var preparedSections, out error))
            {
                return false;
            }

            return _saveService.ApplyPreparedLoad(preparedSections, out error);
        }

        public bool Reset(out string error)
        {
            error = null;
            LocalizationManager.ClearLegacyLanguagePreference();
            return _saveService.Delete(_relativePath, out error);
        }

        public bool TryReadState(out SettingsSaveState state)
        {
            return TryReadState(out state, out _);
        }

        public bool TryReadState(out SettingsSaveState state, out string error)
        {
            state = null;
            if (!_saveService.TryPrepareLoad(_relativePath, BuildSections(), out _, out var preparedSections, out error))
            {
                return false;
            }

            for (int i = 0; i < preparedSections.Count; i++)
            {
                if (preparedSections[i].State is SettingsSaveState settingsState)
                {
                    state = settingsState;
                    return true;
                }
            }

            error = "Settings file did not contain a settings state.";
            return false;
        }

        private ISaveSection[] BuildSections()
        {
            return new ISaveSection[]
            {
                new SettingsSaveSection()
            };
        }

        public static void SetSharedInstance(SettingsSaveFacade facade)
        {
            Shared = facade ?? CreateDefault();
        }

        private static SettingsSaveFacade CreateDefault()
        {
            return new SettingsSaveFacade(
                new SaveService(
                    new UnityJsonSaveSerializer(),
                    new PersistentDataPathSaveStorage(),
                    new SaveRuntimeOptions()),
                "settings/app-settings.json");
        }
    }
}
