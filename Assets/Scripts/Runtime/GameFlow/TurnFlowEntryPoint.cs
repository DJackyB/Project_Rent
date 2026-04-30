using System;
using System.Threading;
using BaoZuPo.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace BaoZuPo.GameFlow
{
    public sealed class TurnFlowEntryPoint : IAsyncStartable
    {
        private readonly ITurnFlowService _turnFlowService;
        private readonly GameManager _gameManager;

        public TurnFlowEntryPoint(ITurnFlowService turnFlowService, GameManager gameManager)
        {
            _turnFlowService = turnFlowService ?? throw new ArgumentNullException(nameof(turnFlowService));
            _gameManager = gameManager ?? throw new ArgumentNullException(nameof(gameManager));
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                _gameManager.EnsureInitialized();
                _gameManager.DisableNodeCanvasTurnFlow();
                await _turnFlowService.RunAsync(cancellation);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
