using System;
using System.Collections.Generic;
using BaoZuPo.Core;

namespace BaoZuPo.UI.Settlement
{
    [Serializable]
    public enum UISettlementPlaybackStageKind
    {
        Serial,
        Parallel,
        Barrier,
        Aggregate
    }

    [Serializable]
    public sealed class UISettlementPlaybackEntry
    {
        public GameEvents.SettlementSequenceQueued Payload;
        public string LaneKey;

        public int FinalAmount => Payload != null ? Payload.FinalAmount : 0;
        public bool IsValid => Payload != null;

        public static UISettlementPlaybackEntry Create(GameEvents.SettlementSequenceQueued payload, string laneKey)
        {
            return new UISettlementPlaybackEntry
            {
                Payload = payload,
                LaneKey = laneKey
            };
        }
    }

    [Serializable]
    public sealed class UISettlementPlaybackStage
    {
        public string DebugLabel;
        public UISettlementPlaybackStageKind Kind = UISettlementPlaybackStageKind.Serial;
        public List<UISettlementPlaybackEntry> Entries = new();

        public static UISettlementPlaybackStage CreateSerial(string debugLabel, params UISettlementPlaybackEntry[] entries)
        {
            return Create(debugLabel, UISettlementPlaybackStageKind.Serial, entries);
        }

        public static UISettlementPlaybackStage CreateParallel(string debugLabel, params UISettlementPlaybackEntry[] entries)
        {
            return Create(debugLabel, UISettlementPlaybackStageKind.Parallel, entries);
        }

        public static UISettlementPlaybackStage CreateBarrier(string debugLabel = null)
        {
            return Create(debugLabel, UISettlementPlaybackStageKind.Barrier);
        }

        public static UISettlementPlaybackStage CreateAggregate(string debugLabel, params UISettlementPlaybackEntry[] entries)
        {
            return Create(debugLabel, UISettlementPlaybackStageKind.Aggregate, entries);
        }

        private static UISettlementPlaybackStage Create(string debugLabel, UISettlementPlaybackStageKind kind, params UISettlementPlaybackEntry[] entries)
        {
            var stage = new UISettlementPlaybackStage
            {
                DebugLabel = debugLabel,
                Kind = kind
            };

            if (entries == null)
            {
                return stage;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null)
                {
                    stage.Entries.Add(entries[i]);
                }
            }

            return stage;
        }
    }

    [Serializable]
    public sealed class UISettlementPlaybackBatch
    {
        public const string DefaultLaneKey = "settlement-global";

        public List<UISettlementPlaybackStage> Stages = new();

        public bool IsEmpty => Stages == null || Stages.Count == 0;

        public static UISettlementPlaybackBatch CreateSerial(IReadOnlyList<GameEvents.SettlementSequenceQueued> payloads, string laneKey = null)
        {
            var batch = new UISettlementPlaybackBatch();
            if (payloads == null || payloads.Count == 0)
            {
                return batch;
            }

            string resolvedLaneKey = string.IsNullOrWhiteSpace(laneKey) ? DefaultLaneKey : laneKey;
            for (int i = 0; i < payloads.Count; i++)
            {
                var payload = payloads[i];
                if (payload == null)
                {
                    continue;
                }

                batch.Stages.Add(UISettlementPlaybackStage.CreateSerial(
                    payload.Title,
                    UISettlementPlaybackEntry.Create(payload, resolvedLaneKey)));
            }

            return batch;
        }

        public static UISettlementPlaybackBatch FromStages(params UISettlementPlaybackStage[] stages)
        {
            var batch = new UISettlementPlaybackBatch();
            if (stages == null)
            {
                return batch;
            }

            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null)
                {
                    batch.Stages.Add(stages[i]);
                }
            }

            return batch;
        }
    }
}
