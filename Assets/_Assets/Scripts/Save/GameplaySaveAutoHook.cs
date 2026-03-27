using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.Save
{
    public static class GameplaySaveAutoHook
    {
        private static bool _registered;
        private static bool _suppressNextActionAutoSave;
        private static GamePhase _lastPhase = GamePhase.Prepare;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _registered = false;
            _suppressNextActionAutoSave = false;
            _lastPhase = GamePhase.Prepare;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (_registered)
            {
                return;
            }

            EventBus.Subscribe<GameEvents.PhaseChanged>(OnPhaseChanged);
            EventBus.Subscribe<GameEvents.GameStateLoaded>(OnGameStateLoaded);
            _registered = true;
        }

        private static void OnPhaseChanged(GameEvents.PhaseChanged payload)
        {
            if (payload.Phase == GamePhase.Action && _lastPhase != GamePhase.Action)
            {
                if (_suppressNextActionAutoSave)
                {
                    _suppressNextActionAutoSave = false;
                }
                else if (!GameplaySaveFacade.Shared.SaveAutoSlot(out var error))
                {
                    Debug.LogWarning($"[GameplaySaveAutoHook] Failed to auto-save: {error}");
                }
            }

            _lastPhase = payload.Phase;
        }

        private static void OnGameStateLoaded(GameEvents.GameStateLoaded payload)
        {
            _lastPhase = payload.Phase;
            _suppressNextActionAutoSave = true;
        }
    }
}
