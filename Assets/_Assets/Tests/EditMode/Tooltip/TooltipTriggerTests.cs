using Martian.Tooltip;
using Martian.Tooltip.Runtime;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Martian.Tests.Tooltip
{
    public class TooltipTriggerTests
    {
        [TearDown]
        public void TearDown()
        {
            TooltipServices.SetCurrent(null);

            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < eventSystems.Length; i++)
            {
                Object.DestroyImmediate(eventSystems[i].gameObject);
            }

            var services = Object.FindObjectsByType<TooltipRuntimeService>(FindObjectsSortMode.None);
            for (int i = 0; i < services.Length; i++)
            {
                Object.DestroyImmediate(services[i].gameObject);
            }
        }

        [Test]
        public void OnPointerEnter_WhenProviderReturnsFalse_DoesNotCallService()
        {
            var service = new FakeTooltipService();
            TooltipServices.SetCurrent(service);

            var go = new GameObject("TooltipHost");
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            try
            {
                var provider = go.AddComponent<FakeTooltipProvider>();
                provider.ShouldSucceed = false;
                var trigger = go.AddComponent<TooltipTrigger>();
                trigger.Bind(provider);

                trigger.OnPointerEnter(new PointerEventData(eventSystem) { position = new Vector2(10f, 10f) });

                Assert.AreEqual(0, service.ShowCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(eventSystem.gameObject);
            }
        }

        [Test]
        public void OnPointerEnter_WhenRequestHasNoAnchor_DoesNotCallService()
        {
            var service = new FakeTooltipService();
            TooltipServices.SetCurrent(service);

            var go = new GameObject("TooltipHost");
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            try
            {
                var provider = go.AddComponent<FakeTooltipProvider>();
                provider.ShouldSucceed = true;
                provider.Request = new TooltipRequest(provider, null, new TooltipContent("martian.tooltip.test", new object()));

                var trigger = go.AddComponent<TooltipTrigger>();
                trigger.Bind(provider);
                trigger.OnPointerEnter(new PointerEventData(eventSystem) { position = Vector2.zero });

                Assert.AreEqual(0, service.ShowCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(eventSystem.gameObject);
            }
        }

        [Test]
        public void OnDisable_HidesLastActiveOwner()
        {
            var service = new FakeTooltipService();
            TooltipServices.SetCurrent(service);

            var go = new GameObject("TooltipHost", typeof(RectTransform));
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

            try
            {
                var provider = go.AddComponent<FakeTooltipProvider>();
                var trigger = go.AddComponent<TooltipTrigger>();
                trigger.Bind(provider);

                typeof(TooltipTrigger)
                    .GetField("_activeOwner", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(trigger, provider);

                typeof(TooltipTrigger)
                    .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(trigger, null);

                Assert.AreEqual(1, service.HideCount);
                Assert.AreSame(provider, service.LastHiddenOwner);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(eventSystem.gameObject);
            }
        }

        private sealed class FakeTooltipProvider : MonoBehaviour, ITooltipContentProvider
        {
            public bool ShouldSucceed;
            public TooltipRequest Request;

            public bool TryBuildTooltipRequest(out TooltipRequest request)
            {
                request = Request;
                return ShouldSucceed;
            }
        }

        private sealed class FakeTooltipService : ITooltipService
        {
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }
            public object LastHiddenOwner { get; private set; }
            public bool IsAvailable => true;

            public void Show(TooltipRequest request, Vector2? pointerPosition = null)
            {
                ShowCount++;
            }

            public void Hide(object owner)
            {
                HideCount++;
                LastHiddenOwner = owner;
            }

            public void HideAll()
            {
            }
        }
    }
}
