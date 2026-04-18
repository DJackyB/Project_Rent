using BaoZuPo.GameFlow;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// Opens the current turn's shop session.
    /// </summary>
    public sealed class OpenShopEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            if (TurnManager.Instance == null)
            {
                Debug.LogWarning("[OpenShopEffect] TurnManager is unavailable.");
                return;
            }

            TurnManager.Instance.OpenShop(source);
        }
    }
}
