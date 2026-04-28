using BaoZuPo.Core;
using BaoZuPo.UI.Settlement;

namespace BaoZuPo.GameFlow
{
    public sealed class SettlementPresentationMapper : ISettlementPresentationMapper
    {
        private const string DefaultSettlementLaneKey = UISettlementPlaybackBatch.DefaultLaneKey;

        public UISettlementPlaybackBatch Map(SettlementResult result)
        {
            var batch = new UISettlementPlaybackBatch();
            if (result == null)
            {
                return batch;
            }

            batch.CompletionBatchId = result.SettlementId;
            batch.DeferredMoneyStartValue = result.MoneyBefore;
            batch.DeferredMoneyEndValue = result.MoneyAfter;

            int sourceCount = CountSources(result);
            for (int i = 0; i < result.Stages.Count; i++)
            {
                var stage = result.Stages[i];
                if (stage == null)
                {
                    continue;
                }

                var entries = new UISettlementPlaybackEntry[stage.Sources.Count];
                int entryCount = 0;
                for (int j = 0; j < stage.Sources.Count; j++)
                {
                    var source = stage.Sources[j];
                    if (source == null)
                    {
                        continue;
                    }

                    string laneKey = BuildLaneKey(result.SettlementId, source.SourceIndex);
                    var payload = new GameEvents.SettlementSequenceQueued
                    {
                        BatchId = result.SettlementId,
                        SourceIndex = source.SourceIndex,
                        SourceCount = sourceCount,
                        LaneKey = laneKey,
                        SourceKind = source.SourceKind,
                        Room = source.Room,
                        Card = source.Card,
                        Title = source.Title,
                        Steps = source.Steps,
                        FinalAmount = source.FinalAmount,
                        TrackIndex = 0,
                        TrackCount = 1
                    };

                    entries[entryCount++] = UISettlementPlaybackEntry.Create(payload, laneKey);
                }

                if (entryCount == 0)
                {
                    continue;
                }

                if (entryCount != entries.Length)
                {
                    var compacted = new UISettlementPlaybackEntry[entryCount];
                    for (int j = 0; j < entryCount; j++)
                    {
                        compacted[j] = entries[j];
                    }

                    entries = compacted;
                }

                batch.Stages.Add(CreateStage(stage, entries));
            }

            return batch;
        }

        private static UISettlementPlaybackStage CreateStage(SettlementStageResult stage, UISettlementPlaybackEntry[] entries)
        {
            return stage.Kind switch
            {
                SettlementPlaybackStageKind.Parallel => UISettlementPlaybackStage.CreateParallel(stage.DebugLabel, entries),
                SettlementPlaybackStageKind.Barrier => UISettlementPlaybackStage.CreateBarrier(stage.DebugLabel),
                SettlementPlaybackStageKind.Aggregate => UISettlementPlaybackStage.CreateAggregate(stage.DebugLabel, entries),
                _ => UISettlementPlaybackStage.CreateSerial(stage.DebugLabel, entries)
            };
        }

        private static int CountSources(SettlementResult result)
        {
            int count = 0;
            for (int i = 0; i < result.Stages.Count; i++)
            {
                var stage = result.Stages[i];
                if (stage != null)
                {
                    count += stage.Sources.Count;
                }
            }

            return count;
        }

        private static string BuildLaneKey(string batchId, int sourceIndex)
        {
            return $"{DefaultSettlementLaneKey}:{batchId}:{sourceIndex}";
        }
    }
}
