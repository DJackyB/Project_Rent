using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    public sealed class TurnFlowService : ITurnFlowService
    {
        private readonly TurnManager _turnManager;

        public TurnFlowService(TurnManager turnManager)
        {
            _turnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
        }

        public async UniTask RunAsync(CancellationToken ct)
        {
            await UniTask.NextFrame(ct);

            while (!ct.IsCancellationRequested && !_turnManager.IsGameOver)
            {
                _turnManager.ExecutePreparePhase();
                await WaitPrepareCompletedAsync(ct);
                if (_turnManager.IsGameOver)
                    break;

                _turnManager.StartActionPhase();
                await WaitActionCompletedAsync(ct);
                if (_turnManager.IsGameOver)
                    break;

                _turnManager.ExecuteSettlePhase();
                await WaitSettlementCompletedAsync(ct);
            }
        }

        private UniTask WaitPrepareCompletedAsync(CancellationToken ct)
        {
            return UniTask.WaitUntil(
                () => _turnManager.IsGameOver || !_turnManager.IsPreparePresentationPending,
                PlayerLoopTiming.Update,
                ct);
        }

        private UniTask WaitActionCompletedAsync(CancellationToken ct)
        {
            return UniTask.WaitUntil(
                () => _turnManager.IsGameOver || _turnManager.ActionPhaseEnded,
                PlayerLoopTiming.Update,
                ct);
        }

        private UniTask WaitSettlementCompletedAsync(CancellationToken ct)
        {
            return UniTask.WaitUntil(
                () => _turnManager.IsGameOver || IsSettlementFlowIdle(),
                PlayerLoopTiming.Update,
                ct);
        }

        private bool IsSettlementFlowIdle()
        {
            return !_turnManager.IsSettlementPlaybackPending
                && !_turnManager.IsRewardSelectionPending
                && !_turnManager.IsPostSettlementRandomEventPending;
        }
    }
}
