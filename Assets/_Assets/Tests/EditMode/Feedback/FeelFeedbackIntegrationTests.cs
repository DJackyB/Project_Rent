using System.Reflection;
using BaoZuPo.Integration.Feel;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BaoZuPo.Tests.Feedback
{
    public class FeelFeedbackIntegrationTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
                _host = null;
            }
        }

        [Test]
        public void CompositeBackend_ReturnsPrimaryHandle()
        {
            var expectedHandle = new FeedbackPlaybackHandle("lane:primary", "target:primary");
            var primary = new RecordingBackend(expectedHandle);
            var secondary = new RecordingBackend(null);
            var composite = new CompositeFeedbackPlaybackBackend(primary, secondary);

            var request = new FeedbackRequest
            {
                LaneKey = "lane:test",
                TargetKey = "target:test",
                Text = "+12"
            };

            var actualHandle = composite.Publish(request);

            Assert.AreSame(expectedHandle, actualHandle);
            Assert.AreEqual(1, primary.PublishCount);
            Assert.AreEqual(1, secondary.PublishCount);
        }

        [Test]
        public void FeelBackend_PlaySlotAt_MissingSlot_DoesNotThrow()
        {
            _host = new GameObject("FeelHost", typeof(RectTransform), typeof(Canvas));
            _host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var backend = new FeelFeedbackBackend();
            backend.Attach(_host.transform);
            backend.Configure(new FeedbackRuntimeOptions { EnableFeedback = true });

            Assert.DoesNotThrow(() => backend.PlaySlotAt("missing", new Vector3(12f, 34f, 0f), "MissingSlot"));
            Assert.IsNull(backend.Publish(new FeedbackRequest
            {
                Category = BaoZuPoFeedbackCategories.Money,
                TargetKey = "hud:money",
                Text = "+1"
            }));
        }

        [Test]
        public void Installer_AwakeWithoutBootstrap_DisablesSelf()
        {
            _host = new GameObject("FeelInstallerHost", typeof(RectTransform), typeof(BaoZuPoFeelFeedbackInstaller));
            var installer = _host.GetComponent<BaoZuPoFeelFeedbackInstaller>();
            var awake = typeof(BaoZuPoFeelFeedbackInstaller).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(awake);
            LogAssert.Expect(LogType.Error, "[FeelInstaller] FeedbackBootstrap is not assigned. Feel integration has been disabled.");

            awake.Invoke(installer, null);

            Assert.IsFalse(installer.enabled);
        }

        private sealed class RecordingBackend : IFeedbackPlaybackBackend
        {
            private readonly FeedbackPlaybackHandle _publishHandle;

            public RecordingBackend(FeedbackPlaybackHandle publishHandle)
            {
                _publishHandle = publishHandle;
            }

            public int PublishCount { get; private set; }

            public bool IsAvailable => true;

            public event System.Action AllPlaybackCompleted
            {
                add { }
                remove { }
            }

            public void Attach(Transform host)
            {
            }

            public void Configure(FeedbackRuntimeOptions options)
            {
            }

            public FeedbackPlaybackHandle Publish(FeedbackRequest request)
            {
                PublishCount++;
                return _publishHandle;
            }

            public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
            {
                return _publishHandle;
            }

            public void Clear()
            {
            }
        }
    }
}
