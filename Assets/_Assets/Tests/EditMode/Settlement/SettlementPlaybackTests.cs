using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.Integration.Martian.Feedback;
using BaoZuPo.UI;
using BaoZuPo.UI.Settlement;
using Martian.EventBus;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace BaoZuPo.Tests.Settlement
{
    public class SettlementPlaybackTests
    {
        private readonly System.Collections.Generic.List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            FeedbackServiceLocator.Reset();

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
        public void SettlementCaptureContext_CanOmitFinalStep()
        {
            var capture = new SettlementCaptureContext();
            capture.Begin();
            capture.RecordBase(20, "Base");
            capture.RecordDelta(5, "Bonus");

            var steps = capture.Complete(25, includeFinalStep: false);

            Assert.AreEqual(2, steps.Length);
            Assert.AreEqual(GameEvents.SettlementStepKind.Base, steps[0].Kind);
            Assert.AreEqual(GameEvents.SettlementStepKind.Delta, steps[1].Kind);
            Assert.IsFalse(capture.IsCapturing);
        }

        [Test]
        public void SettlementBatchPlayback_UsesParallelRoomTracks_AndSingleMoneyJumpAtBatchEnd()
        {
            var feedback = new RecordingFeedbackService();
            FeedbackServiceLocator.SetService(feedback);

            var moneyManager = CreateObject<MoneyManager>("MoneyManager");
            moneyManager.Initialize(100);

            var canvasObject = CreateCanvasRoot();
            var uiManager = canvasObject.AddComponent<UIManager>();
            var topBar = canvasObject.AddComponent<UITopBar>();
            uiManager.topBar = topBar;

            var controllerObject = CreateChild(canvasObject, "SettlementController");
            var controller = controllerObject.AddComponent<UISettlementSequenceController>();

            var room = CreateObject<RoomSlot>("Room");
            room.Initialize(0, 2, 3);

            var tenantA = CreateCardInstance("Worker A", CardType.Tenant);
            var tenantB = CreateCardInstance("Worker B", CardType.Tenant);

            int completionCount = 0;
            void OnCompleted(GameEvents.SettlementPlaybackCompleted _) => completionCount++;
            EventBus.Subscribe<GameEvents.SettlementPlaybackCompleted>(OnCompleted);

            try
            {
                EventBus.Publish(new GameEvents.PhaseChanged
                {
                    Phase = GamePhase.Settle,
                    PhaseName = nameof(GamePhase.Settle)
                });

                moneyManager.AddMoney(30);

                var batch = new UISettlementPlaybackBatch
                {
                    CompletionBatchId = "batch-room-parallel",
                    DeferredMoneyStartValue = 100,
                    DeferredMoneyEndValue = 130
                };

                batch.Stages.Add(UISettlementPlaybackStage.CreateParallel(
                    "Room 1",
                    UISettlementPlaybackEntry.Create(CreateRoomPayload("batch-room-parallel", 0, room, tenantA, 0, 2, 15, 10, 5), "lane:a"),
                    UISettlementPlaybackEntry.Create(CreateRoomPayload("batch-room-parallel", 1, room, tenantB, 1, 2, 15, 10, 5), "lane:b")));

                controller.Queue(batch);

                Assert.AreEqual(2, feedback.SequenceRequests.Count);
                Assert.AreEqual(1, feedback.Requests.Count);
                Assert.AreEqual(1, completionCount);

                Assert.AreEqual(UIStrings.SettlementBase, feedback.SequenceRequests[0].Steps[0].Label);
                Assert.AreEqual("Worker A", feedback.SequenceRequests[0].Steps[1].Label);
                Assert.AreEqual("Worker B", feedback.SequenceRequests[1].Steps[1].Label);
                Assert.AreNotEqual(feedback.SequenceRequests[0].ScreenOffset.x, feedback.SequenceRequests[1].ScreenOffset.x);

                Assert.AreEqual("hud:money", feedback.Requests[0].TargetKey);
                Assert.AreEqual("+30", feedback.Requests[0].Text);
                Assert.NotNull(topBar.moneyText);
                StringAssert.Contains("130", topBar.moneyText.text);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.SettlementPlaybackCompleted>(OnCompleted);
            }
        }

        [Test]
        public void SettlementBatchPlayback_WithNoSourceStages_StillPublishesSingleMoneyJump()
        {
            var feedback = new RecordingFeedbackService();
            FeedbackServiceLocator.SetService(feedback);

            var moneyManager = CreateObject<MoneyManager>("MoneyManager");
            moneyManager.Initialize(120);

            var canvasObject = CreateCanvasRoot();
            var uiManager = canvasObject.AddComponent<UIManager>();
            var topBar = canvasObject.AddComponent<UITopBar>();
            uiManager.topBar = topBar;

            var controllerObject = CreateChild(canvasObject, "SettlementController");
            var controller = controllerObject.AddComponent<UISettlementSequenceController>();

            int completionCount = 0;
            void OnCompleted(GameEvents.SettlementPlaybackCompleted _) => completionCount++;
            EventBus.Subscribe<GameEvents.SettlementPlaybackCompleted>(OnCompleted);

            try
            {
                EventBus.Publish(new GameEvents.PhaseChanged
                {
                    Phase = GamePhase.Settle,
                    PhaseName = nameof(GamePhase.Settle)
                });

                bool paid = moneyManager.ReduceMoney(20);
                Assert.IsTrue(paid);

                controller.Queue(new UISettlementPlaybackBatch
                {
                    CompletionBatchId = "batch-loan-only",
                    DeferredMoneyStartValue = 120,
                    DeferredMoneyEndValue = 100
                });

                Assert.AreEqual(0, feedback.SequenceRequests.Count);
                Assert.AreEqual(1, feedback.Requests.Count);
                Assert.AreEqual("-20", feedback.Requests[0].Text);
                Assert.AreEqual(1, completionCount);
                Assert.NotNull(topBar.moneyText);
                StringAssert.Contains("100", topBar.moneyText.text);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.SettlementPlaybackCompleted>(OnCompleted);
            }
        }

        private T CreateObject<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            _createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private GameObject CreateCanvasRoot()
        {
            var canvasObject = new GameObject("CanvasRoot", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _createdObjects.Add(canvasObject);
            return canvasObject;
        }

        private GameObject CreateChild(GameObject parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent.transform, false);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static CardInstance CreateCardInstance(string cardName, CardType cardType)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.cardName = cardName;
            data.cardType = cardType;
            data.targetKind = CardPlayTargetKind.Room;
            data.rarity = CardRarity.Common;
            data.cost = 0;
            data.baseRent = 0;
            return new CardInstance(data);
        }

        private static GameEvents.SettlementSequenceQueued CreateRoomPayload(
            string batchId,
            int sourceIndex,
            RoomSlot room,
            CardInstance card,
            int trackIndex,
            int trackCount,
            int finalAmount,
            int baseAmount,
            int deltaAmount)
        {
            return new GameEvents.SettlementSequenceQueued
            {
                BatchId = batchId,
                SourceIndex = sourceIndex,
                SourceCount = 2,
                TrackIndex = trackIndex,
                TrackCount = trackCount,
                LaneKey = $"lane:{trackIndex}",
                SourceKind = GameEvents.SettlementSourceKind.Room,
                Room = room,
                Card = card,
                Title = card.Data.cardName,
                Steps = new[]
                {
                    new GameEvents.SettlementStep
                    {
                        Kind = GameEvents.SettlementStepKind.Base,
                        Amount = baseAmount
                    },
                    new GameEvents.SettlementStep
                    {
                        Kind = GameEvents.SettlementStepKind.Delta,
                        Amount = deltaAmount
                    }
                },
                FinalAmount = finalAmount
            };
        }

        private sealed class RecordingFeedbackService : IFeedbackService
        {
            public readonly System.Collections.Generic.List<FeedbackRequest> Requests = new();
            public readonly System.Collections.Generic.List<FeedbackSequenceRequest> SequenceRequests = new();

            public bool IsAvailable => true;

            public FeedbackPlaybackHandle Publish(FeedbackRequest request)
            {
                Requests.Add(request);
                var handle = new FeedbackPlaybackHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
                handle.Complete();
                return handle;
            }

            public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
            {
                SequenceRequests.Add(request);
                var handle = new FeedbackPlaybackHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
                handle.Complete();
                return handle;
            }
        }
    }
}
