using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using NUnit.Framework;

namespace BaoZuPo.Tests.EditMode.Card
{
    public sealed class SettlementPresentationMapperTests
    {
        [Test]
        public void Map_CopiesDomainResultToUiBatchAndAssignsPlaybackFields()
        {
            var result = new SettlementResult
            {
                SettlementId = "settlement-test",
                MoneyBefore = 10,
                MoneyAfter = 25
            };

            var stage = new SettlementStageResult
            {
                DebugLabel = "Room 1",
                Kind = SettlementPlaybackStageKind.Serial
            };
            stage.Sources.Add(new SettlementSourceResult
            {
                SourceIndex = 0,
                SourceKind = GameEvents.SettlementSourceKind.Room,
                Title = "Tenant",
                FinalAmount = 15,
                Steps = new[]
                {
                    new GameEvents.SettlementStep
                    {
                        Kind = GameEvents.SettlementStepKind.Base,
                        Label = "Base",
                        Amount = 15
                    }
                }
            });
            result.Stages.Add(stage);

            var batch = new SettlementPresentationMapper().Map(result);

            Assert.That(batch.CompletionBatchId, Is.EqualTo("settlement-test"));
            Assert.That(batch.DeferredMoneyStartValue, Is.EqualTo(10));
            Assert.That(batch.DeferredMoneyEndValue, Is.EqualTo(25));
            Assert.That(batch.Stages, Has.Count.EqualTo(1));
            Assert.That(batch.Stages[0].Entries, Has.Count.EqualTo(1));

            var payload = batch.Stages[0].Entries[0].Payload;
            Assert.That(payload.BatchId, Is.EqualTo("settlement-test"));
            Assert.That(payload.SourceCount, Is.EqualTo(1));
            Assert.That(payload.LaneKey, Is.EqualTo("settlement-global:settlement-test:0"));
            Assert.That(payload.FinalAmount, Is.EqualTo(15));
            Assert.That(payload.Steps, Has.Length.EqualTo(1));
        }
    }
}
