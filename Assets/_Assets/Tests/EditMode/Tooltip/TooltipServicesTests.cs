using BaoZuPo.UI.Common.Tooltip;
using NUnit.Framework;

namespace BaoZuPo.Tests.UI.Tooltip
{
    public class TooltipServicesTests
    {
        [TearDown]
        public void TearDown()
        {
            TooltipServices.SetCurrent(null);
        }

        [Test]
        public void Current_UsesNullServiceByDefault()
        {
            TooltipServices.SetCurrent(null);

            Assert.IsFalse(TooltipServices.Current.IsAvailable);
            TooltipServices.Current.Show(null);
            TooltipServices.Current.Hide(null);
            TooltipServices.Current.HideAll();
        }

        [Test]
        public void ResetCurrent_RestoresNullService()
        {
            var fakeService = new FakeTooltipService();
            TooltipServices.SetCurrent(fakeService);

            TooltipServices.ResetCurrent(fakeService);

            Assert.IsFalse(TooltipServices.Current.IsAvailable);
        }

        private sealed class FakeTooltipService : ITooltipService
        {
            public bool IsAvailable => true;

            public void Show(TooltipRequest request, UnityEngine.Vector2? pointerPosition = null)
            {
            }

            public void Hide(object owner)
            {
            }

            public void HideAll()
            {
            }
        }
    }
}
