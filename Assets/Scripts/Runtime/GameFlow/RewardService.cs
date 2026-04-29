using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.UI;
using Cysharp.Threading.Tasks;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    public sealed class RewardService : IRewardService
    {
        private const int DefaultRewardCount = 3;

        public async UniTask<RewardChoiceResult> OfferAndWaitChoiceAsync(bool boosted, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var config = GameManager.Instance?.gameConfig;
            if (config == null)
                throw new InvalidOperationException("[RewardService] GameConfig is missing.");

            var rewardLibrary = config.rewardLibrary;
            if (rewardLibrary == null)
            {
                Debug.LogWarning("[RewardService] No reward library configured.");
                return RewardChoiceResult.Skipped;
            }

            var allEntries = rewardLibrary.entries;
            if (allEntries == null || allEntries.Count == 0)
            {
                Debug.LogWarning("[RewardService] Reward library is empty.");
                return RewardChoiceResult.Skipped;
            }

            List<CardData> source;
            if (boosted)
            {
                source = allEntries.Select(e => e?.card).Where(c => c != null && c.rarity >= CardRarity.Rare).ToList();
                if (source.Count == 0)
                {
                    Debug.LogWarning("[RewardService] Boosted reward requested but no Rare+ cards. Falling back to full reward library.");
                    source = allEntries.Select(e => e?.card).Where(c => c != null).ToList();
                }
            }
            else
            {
                source = allEntries.Select(e => e?.card).Where(c => c != null).ToList();
            }

            if (source.Count == 0)
            {
                Debug.LogWarning("[RewardService] No reward cards available.");
                return RewardChoiceResult.Skipped;
            }

            if (UIManager.Instance == null)
                throw new InvalidOperationException("[RewardService] UIManager is required for reward selection.");

            var options = BuildUniqueOptions(source, DefaultRewardCount);
            if (options.Length == 0)
            {
                Debug.LogWarning("[RewardService] No unique reward cards available.");
                return RewardChoiceResult.Skipped;
            }

            if (options.Length < DefaultRewardCount)
                Debug.LogWarning($"[RewardService] Only {options.Length} unique reward card(s) available.");

            var tcs = new UniTaskCompletionSource<RewardChoiceResult>();

            void OnSelected(GameEvents.CardRewardSelected e)
            {
                tcs.TrySetResult(new RewardChoiceResult(e.ChosenCard, e.HasSourceWorldPosition, e.SourceWorldPosition));
            }

            EventBus.Subscribe<GameEvents.CardRewardSelected>(OnSelected);
            try
            {
                EventBus.Publish(new GameEvents.CardRewardOffered { Options = options, Boosted = boosted });
                return await tcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                EventBus.Unsubscribe<GameEvents.CardRewardSelected>(OnSelected);
            }
        }

        private static CardData[] BuildUniqueOptions(List<CardData> source, int desiredCount)
        {
            var remaining = source.ToList();
            var options = new List<CardData>(desiredCount);

            while (remaining.Count > 0 && options.Count < desiredCount)
            {
                var chosen = remaining[UnityEngine.Random.Range(0, remaining.Count)];
                options.Add(chosen);
                remaining.RemoveAll(c => c != null && (ReferenceEquals(c, chosen) || c.cardId == chosen.cardId));
            }

            return options.ToArray();
        }
    }
}
