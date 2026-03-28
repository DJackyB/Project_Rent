using NUnit.Framework;

namespace Martian.Audio.Tests
{
    public class AudioServicesTests
    {
        [SetUp]
        public void SetUp()
        {
            AudioRuntimeInstaller.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            AudioRuntimeInstaller.ResetForTests();
        }

        [Test]
        public void Current_DefaultsToNoOpService_WhenNotInstalled()
        {
            Assert.IsInstanceOf<NoOpAudioService>(AudioServices.Current);
            Assert.IsFalse(AudioServices.Current.IsAvailable);
        }

        [Test]
        public void Install_WithNullConfig_LeavesNoOpService()
        {
            var result = AudioRuntimeInstaller.Install(null, null);

            Assert.IsInstanceOf<NoOpAudioService>(result);
            Assert.IsInstanceOf<NoOpAudioService>(AudioServices.Current);
        }
    }
}
