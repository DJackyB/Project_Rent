using Martian.Tooltip;
using Martian.Tooltip.Presets;
using Martian.Tooltip.Runtime;
using NUnit.Framework;
using System;
using UnityEngine;

namespace Martian.Tests.Tooltip
{
    public class TooltipRuntimeInstallerTests
    {
        [TearDown]
        public void TearDown()
        {
            TooltipPresenterRegistry.Clear();
            TooltipServices.SetCurrent(null);

            var services = Object.FindObjectsByType<TooltipRuntimeService>(FindObjectsSortMode.None);
            for (int i = 0; i < services.Length; i++)
            {
                Object.DestroyImmediate(services[i].gameObject);
            }
        }

        [Test]
        public void Install_CreatesRuntimeService()
        {
            TooltipRuntimeInstaller.Install();

            Assert.IsTrue(TooltipServices.Current.IsAvailable);
            Assert.NotNull(Object.FindFirstObjectByType<TooltipRuntimeService>());
        }

        [Test]
        public void DocumentPresetInstaller_RegistersDefaultPresenter()
        {
            TooltipDocumentPresetInstaller.Install();

            var parent = new GameObject("Parent", typeof(RectTransform));
            try
            {
                var content = new TooltipContent(
                    TooltipDocumentContentIds.Document,
                    new TooltipDocument(
                        "Title",
                        "Subtitle",
                        "Summary",
                        new[] { "tag1", "tag2" },
                        new[]
                        {
                            new TooltipDocumentSection(
                                "Stats",
                                new[]
                                {
                                    new TooltipDocumentRow("HP", "120"),
                                    new TooltipDocumentRow("ATK", "16", true)
                                })
                        }));

                Assert.IsTrue(TooltipPresenterRegistry.TryCreatePresenter(content, parent.transform, out var presenter));
                Assert.IsNotNull(presenter);

                presenter.Show(new TooltipRequest(parent, parent.GetComponent<RectTransform>(), content));
                var texts = presenter.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

                Assert.That(texts, Is.Not.Empty);
                Assert.IsTrue(Array.Exists(texts, t => t != null && t.text.Contains("Title")));
                Assert.IsTrue(Array.Exists(texts, t => t != null && t.text.Contains("Stats")));
                Assert.IsTrue(Array.Exists(texts, t => t != null && t.text.Contains("ATK")));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void RuntimeService_ShowWithoutPresenter_IsSafe()
        {
            TooltipRuntimeInstaller.Install();
            TooltipPresenterRegistry.Clear();

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var anchorObject = new GameObject("Anchor", typeof(RectTransform));

            try
            {
                anchorObject.transform.SetParent(canvasObject.transform, false);

                var service = TooltipServices.Current;
                var request = new TooltipRequest(
                    anchorObject,
                    anchorObject.GetComponent<RectTransform>(),
                    new TooltipContent("martian.tooltip.unknown", new object()));

                Assert.DoesNotThrow(() => service.Show(request));
            }
            finally
            {
                Object.DestroyImmediate(anchorObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
