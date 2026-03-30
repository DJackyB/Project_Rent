using System;
using System.Collections.Generic;
using System.Globalization;
using BaoZuPo.Card;
using BaoZuPo.Localization;
using Martian.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BaoZuPo.Tests.EditMode.Localization
{
    public sealed class LocalizationFallbackTests
    {
        private FakeLanguageService _languageService;

        [SetUp]
        public void SetUp()
        {
            LocalizationServices.Reset();
            _languageService = new FakeLanguageService("zh-Hans");
            LocalizationServices.SetLanguageService(_languageService);
            LocalizationServices.SetTextService(new LanguageAwareTextService());
        }

        [TearDown]
        public void TearDown()
        {
            LocalizationServices.Reset();
        }

        [Test]
        public void NullTextService_UsesFallbackAndFormatsArguments()
        {
            LocalizationServices.SetTextService(NullLocalizedTextService.Instance);

            string resolved = LocalizationServices.Resolve(
                new LocalizationTextRef("UI", "ui.test.value", "Value {0}"),
                42);

            Assert.AreEqual("Value 42", resolved);
        }

        [Test]
        public void GameText_UsesDefaultChineseFallbackWhenNoPackageTableIsAvailable()
        {
            Assert.AreEqual("\u56DE\u5408 3", GameText.Turn(3));
            Assert.AreEqual("\u51FA\u724C\u533A", GameText.PlayArea);
            Assert.AreEqual("\u6E38\u620F\u7ED3\u675F", GameText.GameOverTitle);
            Assert.AreEqual(
                "\u4F60\u575A\u6301\u4E86 2 \u56DE\u5408\n\u6700\u7EC8\u8D44\u91D1\uFF1A100",
                GameText.GameOverInfo(2, 100));
        }

        [Test]
        public void GameText_ChangesWithLanguageService()
        {
            Assert.AreEqual("\u56DE\u5408 5", GameText.Turn(5));

            _languageService.SetLanguage("en");

            Assert.AreEqual("Turn 5", GameText.Turn(5));
            Assert.AreEqual("End Turn", GameText.EndTurnButton);
            Assert.AreEqual("Game Over", GameText.GameOverTitle);
        }

        [Test]
        public void CardTextResolver_UsesDefaultFieldsAndLocalizedKeys()
        {
            CardData cardData = ScriptableObjectUtility.CreateCardData(
                1001,
                "Worker",
                "Simple worker, pays on time.",
                "card.1001.name",
                "card.1001.description");

            Assert.AreEqual("Worker", cardData.cardName);
            Assert.AreEqual("Simple worker, pays on time.", cardData.description);
            Assert.AreEqual("Worker", cardData.DefaultName);
            Assert.AreEqual("Simple worker, pays on time.", cardData.DefaultDescription);
            Assert.AreEqual("card.1001.name", cardData.ResolveNameTextKey());
            Assert.AreEqual("card.1001.description", cardData.ResolveDescriptionTextKey());

            Assert.AreEqual("Worker", CardTextResolver.ResolveName(cardData));
            Assert.AreEqual("Simple worker, pays on time.", CardTextResolver.ResolveDescription(cardData));

            _languageService.SetLanguage("en");

            Assert.AreEqual("Worker (EN)", CardTextResolver.ResolveName(cardData));
            Assert.AreEqual("Simple worker, pays on time. (EN)", CardTextResolver.ResolveDescription(cardData));

            Object.DestroyImmediate(cardData);
        }

        private sealed class FakeLanguageService : ILanguageService
        {
            private readonly List<string> _supportedLanguageCodes = new() { "zh-Hans", "en" };

            public FakeLanguageService(string initialLanguageCode)
            {
                CurrentLanguageCode = initialLanguageCode;
                LastSelection = new LanguageSelectionResult(initialLanguageCode, LanguageSelectionReason.Default);
            }

            public event Action<string> LanguageChanged;

            public bool IsAvailable => true;
            public string CurrentLanguageCode { get; private set; }
            public IReadOnlyList<string> SupportedLanguageCodes => _supportedLanguageCodes;
            public LanguageSelectionResult LastSelection { get; private set; }

            public bool SetLanguage(string languageCode)
            {
                CurrentLanguageCode = languageCode;
                if (!_supportedLanguageCodes.Contains(languageCode))
                {
                    _supportedLanguageCodes.Add(languageCode);
                }

                LastSelection = new LanguageSelectionResult(languageCode, LanguageSelectionReason.ExplicitChange);
                LanguageChanged?.Invoke(languageCode);
                return true;
            }
        }

        private sealed class LanguageAwareTextService : ILocalizedTextService
        {
            public bool IsAvailable => true;

            public string Resolve(LocalizationTextRef textRef, params object[] arguments)
            {
                string template = LocalizationServices.Language.CurrentLanguageCode == "en"
                    ? ResolveEnglish(textRef)
                    : textRef.Fallback;

                if (string.IsNullOrEmpty(template))
                {
                    return string.Empty;
                }

                try
                {
                    return string.Format(CultureInfo.InvariantCulture, template, arguments ?? Array.Empty<object>());
                }
                catch (FormatException)
                {
                    return template;
                }
            }

            private static string ResolveEnglish(LocalizationTextRef textRef)
            {
                return textRef.Entry switch
                {
                    "ui.topbar.turn" => "Turn {0}",
                    "ui.topbar.money" => "Money {0}",
                    "ui.topbar.spent" => "Spent {0}",
                    "ui.board.play_area" => "Play Area",
                    "ui.board.contracts" => "Contracts",
                    "ui.board.room_summary" => "Room {0}  Tenant {1}/{2}  Equip {3}/{4}",
                    "ui.card.cost" => "Cost {0}",
                    "ui.card.base_rent" => "Base Rent {0}",
                    "ui.card.wait" => "Wait {0}",
                    "ui.card.lease" => "Lease",
                    "ui.card.durability" => "Durability",
                    "ui.room.empty_equipment" => "Empty Equipment Slot",
                    "ui.room.empty_tenant" => "Empty Tenant Slot",
                    "ui.game_over.title" => "Game Over",
                    "ui.game_over.info" => "You survived {0} turns\nFinal Money: {1}",
                    "ui.phase.end_turn" => "End Turn",
                    "ui.phase.waiting" => "Waiting",
                    "ui.phase.prepare" => "Prepare",
                    "ui.phase.action" => "Action",
                    "ui.phase.settle" => "Settle",
                    "ui.card.type.tenant" => "Tenant",
                    "ui.card.type.equipment" => "Equipment",
                    "ui.card.type.event" => "Event",
                    "ui.card.type.contract" => "Contract",
                    "ui.settlement.room_title" => "Room {0}",
                    "ui.settlement.fallback" => "Settle",
                    "ui.settlement.base" => "Base",
                    "ui.settlement.bonus" => "Bonus",
                    "ui.settlement.multiplier" => "Multiplier",
                    "ui.settlement.final" => "Final",
                    "card.1001.name" => "Worker (EN)",
                    "card.1001.description" => "Simple worker, pays on time. (EN)",
                    _ => textRef.Fallback
                };
            }
        }

        private static class ScriptableObjectUtility
        {
            public static CardData CreateCardData(int cardId, string defaultName, string defaultDescription, string nameTextKey, string descriptionTextKey)
            {
                CardData cardData = ScriptableObject.CreateInstance<CardData>();
                cardData.cardId = cardId;
                cardData.defaultName = defaultName;
                cardData.defaultDescription = defaultDescription;
                cardData.nameTextKey = nameTextKey;
                cardData.descriptionTextKey = descriptionTextKey;
                return cardData;
            }
        }
    }
}
