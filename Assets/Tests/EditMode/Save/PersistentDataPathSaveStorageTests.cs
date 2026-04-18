using System;
using System.IO;
using Martian.Save.Runtime;
using NUnit.Framework;

namespace Martian.Tests.Save
{
    public class PersistentDataPathSaveStorageTests
    {
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MartianSaveStorageTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
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
        public void WriteAllText_WhenOverwriting_PreservesBackup()
        {
            var storage = new PersistentDataPathSaveStorage(_tempDirectory);

            storage.WriteAllText("slots/slot-a.json", "first", ".bak");
            storage.WriteAllText("slots/slot-a.json", "second", ".bak");

            var mainPath = Path.Combine(_tempDirectory, "slots", "slot-a.json");
            var backupPath = Path.Combine(_tempDirectory, "slots", "slot-a.json.bak");

            Assert.AreEqual("second", File.ReadAllText(mainPath));
            Assert.IsTrue(File.Exists(backupPath));
            Assert.AreEqual("first", File.ReadAllText(backupPath));
        }
    }
}
