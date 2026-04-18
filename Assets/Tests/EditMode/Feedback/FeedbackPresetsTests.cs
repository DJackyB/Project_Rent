using Martian.Feedback.Presets;
using Martian.Feedback.Runtime;
using NUnit.Framework;

namespace Martian.Tests.Feedback
{
    public class FeedbackPresetsTests
    {
        [Test]
        public void SignedNumber_BuildsFormattedText()
        {
            var request = FeedbackPresets.SignedNumber("player", 12);

            Assert.AreEqual("player", request.TargetKey);
            Assert.AreEqual("+12", request.Text);
        }

        [Test]
        public void Sequence_PreservesStepOrder()
        {
            var sequence = FeedbackPresets.Sequence(
                "player",
                FeedbackPresets.Step("A", 10),
                FeedbackPresets.Step("B", -5),
                FeedbackPresets.Step("C", 150, isMultiplier: true));

            var playback = FeedbackPlaybackFormatting.Create(new Martian.Feedback.FeedbackRuntimeOptions(), sequence);

            Assert.NotNull(playback);
            Assert.AreEqual(3, playback.Steps.Count);
            Assert.AreEqual("A +10", playback.Steps[0].Text);
            Assert.AreEqual("B -5", playback.Steps[1].Text);
            Assert.AreEqual("C x1.5", playback.Steps[2].Text);
        }
    }
}
