using Martian.Tooltip;
using Martian.Tooltip.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Martian.Tests.Tooltip
{
    public class TooltipRegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            TooltipPresenterRegistry.Clear();
        }

        [Test]
        public void Register_Unregister_And_Clear_Work()
        {
            var factory = new FakePresenterFactory();
            var parent = new GameObject("Parent", typeof(RectTransform));

            try
            {
                TooltipPresenterRegistry.Register(factory);
                TooltipPresenterRegistry.Register(factory);

                var request = new TooltipContent("martian.tooltip.fake", new object());
                Assert.IsTrue(TooltipPresenterRegistry.TryCreatePresenter(request, parent.transform, out var presenter));
                Assert.NotNull(presenter);
                Assert.AreEqual(1, factory.CreateCount);

                TooltipPresenterRegistry.Unregister(factory);
                Assert.IsFalse(TooltipPresenterRegistry.TryCreatePresenter(request, parent.transform, out _));

                TooltipPresenterRegistry.Register(factory);
                TooltipPresenterRegistry.Clear();
                Assert.IsFalse(TooltipPresenterRegistry.TryCreatePresenter(request, parent.transform, out _));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private sealed class FakePresenterFactory : ITooltipPresenterFactory
        {
            public int CreateCount { get; private set; }

            public bool CanPresent(TooltipContent content)
            {
                return content != null && content.ContentId == "martian.tooltip.fake";
            }

            public ITooltipPresenter Create(Transform parent)
            {
                CreateCount++;
                return new FakePresenter(parent);
            }
        }

        private sealed class FakePresenter : ITooltipPresenter
        {
            public FakePresenter(Transform parent)
            {
                Root = parent as RectTransform;
            }

            public RectTransform Root { get; }

            public void Show(TooltipRequest request)
            {
            }

            public void Hide()
            {
            }
        }
    }
}
