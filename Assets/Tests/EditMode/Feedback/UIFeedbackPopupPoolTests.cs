using System.Reflection;
using BaoZuPo.UI.Common.FeedbackPopup;
using NUnit.Framework;
using UnityEngine;

namespace Martian.Tests.Feedback
{
    public sealed class UIFeedbackPopupPoolTests
    {
        private GameObject _canvasObject;

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
            {
                Object.DestroyImmediate(_canvasObject);
                _canvasObject = null;
            }
        }

        [Test]
        public void Show_ReusesCompletedPopupView()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            var layer = UIFeedbackPopupLayer.GetOrCreate(canvas);
            int completedCount = 0;

            var first = layer.Show(new UIFeedbackPopupRequest
            {
                Text = "+1",
                TextConfigurator = _ => { },
                Completed = () => completedCount++
            });
            Assert.NotNull(first);
            Assert.IsTrue(first.gameObject.activeSelf);

            InvokeComplete(first);

            Assert.AreEqual(1, completedCount);
            Assert.IsFalse(first.gameObject.activeSelf);

            var second = layer.Show(new UIFeedbackPopupRequest
            {
                Text = "+2",
                TextConfigurator = _ => { },
                Completed = () => completedCount++
            });

            Assert.AreSame(first, second);
            Assert.IsTrue(second.gameObject.activeSelf);
        }

        private static void InvokeComplete(UIFeedbackPopupView popup)
        {
            var method = typeof(UIFeedbackPopupView).GetMethod(
                "Complete",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(popup, null);
        }
    }
}
