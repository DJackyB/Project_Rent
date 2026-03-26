using Martian.Feedback;
using Martian.Feedback.Runtime;
using NUnit.Framework;

namespace Martian.Tests.Feedback
{
    public class FeedbackServiceLocatorTests
    {
        [TearDown]
        public void TearDown()
        {
            FeedbackServiceLocator.Reset();
        }

        [Test]
        public void Reset_UsesNoOpService()
        {
            FeedbackServiceLocator.Reset();

            Assert.IsFalse(FeedbackServiceLocator.Current.IsAvailable);
        }

        [Test]
        public void SetService_ReturnsProvidedImplementation()
        {
            var service = new FakeFeedbackService();

            FeedbackServiceLocator.SetService(service);

            Assert.AreSame(service, FeedbackServiceLocator.Current);
            Assert.IsTrue(FeedbackServiceLocator.Current.IsAvailable);
        }

        private sealed class FakeFeedbackService : IFeedbackService
        {
            public bool IsAvailable => true;

            public FeedbackPlaybackHandle Publish(FeedbackRequest request)
            {
                return new FeedbackPlaybackHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
            {
                return new FeedbackPlaybackHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }
        }
    }
}
