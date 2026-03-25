using Martian.Feedback;
using Martian.Feedback.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Martian.Tests.Feedback
{
    public class FeedbackBootstrapTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            FeedbackServiceLocator.Reset();

            if (_host != null)
            {
                Object.DestroyImmediate(_host);
                _host = null;
            }
        }

        [Test]
        public void SetBackend_ReplacesRuntimeBackend()
        {
            _host = new GameObject("FeedbackHost", typeof(RectTransform), typeof(Canvas), typeof(FeedbackBootstrap));
            var bootstrap = _host.GetComponent<FeedbackBootstrap>();
            var backend = new FakeBackend();

            bootstrap.Configure(new FeedbackRuntimeOptions
            {
                EnableFeedback = true
            });
            bootstrap.SetBackend(backend);

            FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                TargetKey = "player",
                Text = "+12"
            });

            Assert.AreEqual(1, backend.PublishCount);
        }

        [Test]
        public void Configure_UsesRuntimeOptionsForFormatting()
        {
            var options = new FeedbackRuntimeOptions
            {
                DefaultScreenOffset = new Vector2(0f, 144f),
                SequenceScreenOffset = new Vector2(0f, 168f)
            };

            var single = FeedbackPlaybackFormatting.Create(options, new FeedbackRequest
            {
                TargetKey = "player",
                Text = "+12"
            });

            var sequence = FeedbackPlaybackFormatting.Create(options, new FeedbackSequenceRequest
            {
                TargetKey = "player",
                Steps =
                {
                    new FeedbackStep { Label = "A", Amount = 10 },
                    new FeedbackStep { Label = "B", Amount = 20 }
                }
            });

            Assert.NotNull(single);
            Assert.AreEqual(new Vector2(0f, 144f), single.ScreenOffset);
            Assert.NotNull(sequence);
            Assert.AreEqual(new Vector2(0f, 168f), sequence.ScreenOffset);
        }

        private sealed class FakeBackend : IFeedbackPlaybackBackend
        {
            public int PublishCount { get; private set; }

            public bool IsAvailable => true;

            public event System.Action AllPlaybackCompleted;

            public void Attach(Transform host)
            {
            }

            public void Configure(FeedbackRuntimeOptions options)
            {
            }

            public void Publish(FeedbackRequest request)
            {
                PublishCount++;
            }

            public void PublishSequence(FeedbackSequenceRequest request)
            {
            }

            public void Clear()
            {
            }
        }
    }
}
