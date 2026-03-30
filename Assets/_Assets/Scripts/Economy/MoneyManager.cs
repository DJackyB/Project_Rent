using UnityEngine;
using BaoZuPo.Core;
using Martian.EventBus;
using BaoZuPo.Save;

namespace BaoZuPo.Economy
{
    /// <summary>
    /// Money manager.
    /// </summary>
    public class MoneyManager : Singleton<MoneyManager>
    {
        [Header("Debug Info (Read Only)")]
        [SerializeField] private int _currentMoney;
        [SerializeField] private int _totalSpent;

        /// <summary>Current money.</summary>
        public int CurrentMoney => _currentMoney;

        /// <summary>Total spent money.</summary>
        public int TotalSpent => _totalSpent;

        /// <summary>
        /// Initialize money.
        /// </summary>
        public void Initialize(int startingMoney)
        {
            _currentMoney = startingMoney;
            _totalSpent = 0;
            Debug.Log($"[MoneyManager] Initialized money: {_currentMoney}");
        }

        /// <summary>
        /// Add money.
        /// </summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[MoneyManager] AddMoney received non-positive amount: {amount}");
                return;
            }

            int oldValue = _currentMoney;
            _currentMoney += amount;

            Debug.Log($"[MoneyManager] Money +{amount} ({oldValue} -> {_currentMoney})");

            EventBus.Publish(new GameEvents.MoneyChanged
            {
                OldValue = oldValue,
                NewValue = _currentMoney,
                Delta = amount
            });
        }

        /// <summary>
        /// Reduce money.
        /// </summary>
        /// <returns>Whether the payment succeeded.</returns>
        public bool ReduceMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[MoneyManager] ReduceMoney received non-positive amount: {amount}");
                return true;
            }

            if (_currentMoney < amount)
            {
                Debug.LogWarning($"[MoneyManager] Insufficient money. Current: {_currentMoney}, Required: {amount}");

                EventBus.Publish(new GameEvents.MoneyInsufficient
                {
                    CurrentMoney = _currentMoney,
                    RequiredAmount = amount
                });

                return false;
            }

            int oldValue = _currentMoney;
            _currentMoney -= amount;
            _totalSpent += amount;

            Debug.Log($"[MoneyManager] Money -{amount} ({oldValue} -> {_currentMoney})");

            EventBus.Publish(new GameEvents.MoneyChanged
            {
                OldValue = oldValue,
                NewValue = _currentMoney,
                Delta = -amount
            });

            return true;
        }

        /// <summary>
        /// Check whether current money can afford amount.
        /// </summary>
        public bool CanAfford(int amount)
        {
            return _currentMoney >= amount;
        }

        public EconomySaveState CaptureState()
        {
            return new EconomySaveState
            {
                currentMoney = _currentMoney,
                totalSpent = _totalSpent
            };
        }

        public void RestoreState(EconomySaveState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            _currentMoney = state.currentMoney;
            _totalSpent = state.totalSpent;
        }
    }
}
