using Martian.Feedback;
using Martian.Feedback.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Martian.Tests.Feedback
{
    public class FloatingTextFeedbackBackendTests
    {
        private GameObject _host;
        private FloatingTextFeedbackBackend _backend;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("FeedbackHost", typeof(RectTransform), typeof(Canvas));
            _backend = new FloatingTextFeedbackBackend();
            _backend.Attach(_host.transform);
            _backend.Configure(new FeedbackRuntimeOptions
            {
                EnableFeedback = true
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_backend != null)
            {
                _backend.Clear();
                _backend = null;
            }

            if (_host != null)
            {
                Object.DestroyImmediate(_host);
                _host = null;
            }
        }

        [Test]
        public void PublishQueuesSameTargetIntoSingleTrack()
        {
            _backend.Publish(new FeedbackRequest { TargetKey = "player", Text = "+1" });
            _backend.Publish(new FeedbackRequest { TargetKey = "player", Text = "+2" });

            Assert.AreEqual(1, _backend.ActiveTrackCount);
            Assert.AreEqual(2, _backend.GetPendingCount("player"));
        }

        [Test]
        public void CompleteTrackForTesting_DrainsQueueAndFinishes()
        {
            int completedCount = 0;
            _backend.AllPlaybackCompleted += () => completedCount++;

            _backend.Publish(new FeedbackRequest { TargetKey = "player", Text = "+1" });
            _backend.Publish(new FeedbackRequest { TargetKey = "player", Text = "+2" });

            _backend.CompleteTrackForTesting("player");
            Assert.AreEqual(1, _backend.ActiveTrackCount);
            Assert.AreEqual(1, _backend.GetPendingCount("player"));
            Assert.AreEqual(0, completedCount);

            _backend.CompleteTrackForTesting("player");
            Assert.AreEqual(0, _backend.ActiveTrackCount);
            Assert.AreEqual(0, _backend.GetPendingCount("player"));
            Assert.AreEqual(1, completedCount);
        }

        [Test]
        public void PublishSequence_PreservesStepOrder()
        {
            var request = new FeedbackSequenceRequest
            {
                TargetKey = "player",
                Steps =
                {
                    new FeedbackStep { Label = "First", Amount = 10 },
                    new FeedbackStep { Label = "Second", Amount = -5 },
                    new FeedbackStep { Label = "Third", Amount = 150, IsMultiplier = true }
                }
            };

            var playback = FeedbackPlaybackFormatting.Create(new FeedbackRuntimeOptions(), request);

            Assert.NotNull(playback);
            Assert.AreEqual(3, playback.Steps.Count);
            Assert.AreEqual("First +10", playback.Steps[0].Text);
            Assert.AreEqual("Second -5", playback.Steps[1].Text);
            Assert.AreEqual("Third x1.5", playback.Steps[2].Text);
        }

        [Test]
        public void Clear_RemovesActiveTracksAndPendingRequests()
        {
            _backend.Publish(new FeedbackRequest { TargetKey = "player", Text = "+1" });
            _backend.Publish(new FeedbackRequest { TargetKey = "player", Text = "+2" });

            Assert.AreEqual(1, _backend.ActiveTrackCount);
            Assert.AreEqual(2, _backend.GetPendingCount("player"));

            _backend.Clear();

            Assert.AreEqual(0, _backend.ActiveTrackCount);
            Assert.AreEqual(0, _backend.GetPendingCount("player"));
        }
    }
}
