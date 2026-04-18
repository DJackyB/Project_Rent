using System;
using System.Collections.Generic;
using System.IO;
using Martian.Save;
using Martian.Save.Runtime;
using NUnit.Framework;

namespace Martian.Tests.Save
{
    public class SaveServiceTests
    {
        private string _tempDirectory;
        private SaveService _saveService;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MartianSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            _saveService = new SaveService(
                new UnityJsonSaveSerializer(),
                new PersistentDataPathSaveStorage(_tempDirectory),
                new SaveRuntimeOptions
                {
                    currentSchemaVersion = 3,
                    prettyPrintJson = true
                });
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(_tempDirectory) && Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void Save_ThenPrepareLoad_RoundTripsMultipleSections()
        {
            var alpha = new RecordingSection("alpha", new TestState { number = 7, text = "first" });
            var beta = new RecordingSection("beta", new TestState { number = 13, text = "second" });

            Assert.IsTrue(_saveService.Save("slots/slot-a.json", "slot-a", "Slot A", new ISaveSection[] { alpha, beta }, out var saveError), saveError);
            Assert.IsTrue(_saveService.TryPrepareLoad("slots/slot-a.json", new ISaveSection[] { alpha, beta }, out var envelope, out var preparedSections, out var loadError), loadError);
            Assert.IsTrue(_saveService.ApplyPreparedLoad(preparedSections, out var applyError), applyError);

            Assert.NotNull(envelope);
            Assert.AreEqual("slot-a", envelope.slotId);
            Assert.AreEqual("Slot A", envelope.displayName);
            Assert.AreEqual(2, preparedSections.Count);
            Assert.AreEqual(7, alpha.AppliedState.number);
            Assert.AreEqual("first", alpha.AppliedState.text);
            Assert.AreEqual(13, beta.AppliedState.number);
            Assert.AreEqual("second", beta.AppliedState.text);
        }

        [Test]
        public void TryPrepareLoad_FailsWhenRequiredSectionIsMissing()
        {
            var envelope = new SaveEnvelope
            {
                schemaVersion = 3,
                slotId = "slot-b",
                displayName = "Slot B",
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                appVersion = "test",
                sections = new List<SaveSectionRecord>
                {
                    new()
                    {
                        key = "alpha",
                        jsonPayload = new UnityJsonSaveSerializer().Serialize(new TestState { number = 1, text = "only" })
                    }
                }
            };

            WriteRaw("slots/slot-b.json", new UnityJsonSaveSerializer().Serialize(envelope, true));

            var alpha = new RecordingSection("alpha");
            var beta = new RecordingSection("beta");

            Assert.IsFalse(_saveService.TryPrepareLoad("slots/slot-b.json", new ISaveSection[] { alpha, beta }, out _, out _, out var error));
            StringAssert.Contains("Missing required save section 'beta'", error);
        }

        [Test]
        public void TryPrepareLoad_IgnoresUnknownSections()
        {
            var serializer = new UnityJsonSaveSerializer();
            var envelope = new SaveEnvelope
            {
                schemaVersion = 3,
                slotId = "slot-c",
                displayName = "Slot C",
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                appVersion = "test",
                sections = new List<SaveSectionRecord>
                {
                    new()
                    {
                        key = "alpha",
                        jsonPayload = serializer.Serialize(new TestState { number = 8, text = "known" })
                    },
                    new()
                    {
                        key = "unknown",
                        jsonPayload = serializer.Serialize(new TestState { number = 999, text = "ignored" })
                    }
                }
            };

            WriteRaw("slots/slot-c.json", serializer.Serialize(envelope, true));

            var alpha = new RecordingSection("alpha");
            Assert.IsTrue(_saveService.TryPrepareLoad("slots/slot-c.json", new ISaveSection[] { alpha }, out _, out var prepared, out var error), error);
            Assert.AreEqual(1, prepared.Count);
        }

        [Test]
        public void TryPrepareLoad_RejectsFutureSchema()
        {
            var serializer = new UnityJsonSaveSerializer();
            var envelope = new SaveEnvelope
            {
                schemaVersion = 99,
                slotId = "slot-d",
                displayName = "Slot D",
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                appVersion = "test",
                sections = new List<SaveSectionRecord>
                {
                    new()
                    {
                        key = "alpha",
                        jsonPayload = serializer.Serialize(new TestState { number = 5, text = "future" })
                    }
                }
            };

            WriteRaw("slots/slot-d.json", serializer.Serialize(envelope, true));

            var alpha = new RecordingSection("alpha");
            Assert.IsFalse(_saveService.TryPrepareLoad("slots/slot-d.json", new ISaveSection[] { alpha }, out _, out _, out var error));
            StringAssert.Contains("newer than supported schema", error);
        }

        private void WriteRaw(string relativePath, string content)
        {
            var fullPath = Path.Combine(_tempDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);
        }

        [Serializable]
        private sealed class TestState
        {
            public int number;
            public string text;
        }

        private sealed class RecordingSection : SaveSectionBase<TestState>
        {
            private readonly TestState _capturedState;

            public RecordingSection(string key, TestState capturedState = null)
            {
                Key = key;
                _capturedState = capturedState ?? new TestState();
            }

            public override string Key { get; }
            public TestState AppliedState { get; private set; }

            protected override TestState CaptureTypedState()
            {
                return new TestState
                {
                    number = _capturedState.number,
                    text = _capturedState.text
                };
            }

            protected override bool TryValidateTypedState(TestState state, out string error)
            {
                error = state == null ? "State is null." : null;
                return state != null;
            }

            protected override void ApplyTypedState(TestState state)
            {
                AppliedState = state;
            }
        }
    }
}
