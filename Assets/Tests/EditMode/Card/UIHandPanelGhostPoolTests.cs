using System.Collections.Generic;
using System.Reflection;
using BaoZuPo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.Tests.Card
{
    public sealed class UIHandPanelGhostPoolTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void GhostPool_ReusesReleasedCardGhost()
        {
            var host = CreateGameObject("HandPanel");
            var panel = host.AddComponent<UIHandPanel>();
            panel.cardPrefab = CreateCardPrefab();

            var layer = CreateGameObject("AnimationLayer", typeof(RectTransform)).transform;
            var first = InvokeGetGhost(panel, layer);
            Assert.NotNull(first);

            InvokeReleaseGhost(panel, first);
            Assert.IsFalse(first.activeSelf);

            var second = InvokeGetGhost(panel, layer);
            Assert.AreSame(first, second);
        }

        private GameObject CreateCardPrefab()
        {
            var prefab = CreateGameObject("CardPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UICardView));
            prefab.SetActive(false);
            return prefab;
        }

        private GameObject CreateGameObject(string name, params System.Type[] components)
        {
            var gameObject = components == null || components.Length == 0
                ? new GameObject(name)
                : new GameObject(name, components);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static GameObject InvokeGetGhost(UIHandPanel panel, Transform parent)
        {
            var method = typeof(UIHandPanel).GetMethod(
                "GetGhostCardObject",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (GameObject)method.Invoke(panel, new object[] { parent });
        }

        private static void InvokeReleaseGhost(UIHandPanel panel, GameObject ghost)
        {
            var method = typeof(UIHandPanel).GetMethod(
                "ReleaseGhostCardObject",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(panel, new object[] { ghost });
        }
    }
}
