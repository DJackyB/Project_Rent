using UnityEngine;
using BaoZuPo.Core;
using Martian.EventBus;

namespace BaoZuPo.Economy
{
    /// <summary>
    /// 金钱管理器
    /// 管理游戏中唯一的资源——金钱。
    /// </summary>
    public class MoneyManager : Singleton<MoneyManager>
    {
        [Header("调试信息（运行时只读）")]
        [SerializeField] private int _currentMoney;

        /// <summary>当前金钱</summary>
        public int CurrentMoney => _currentMoney;

        /// <summary>
        /// 初始化金钱（由 GameManager 调用）
        /// </summary>
        public void Initialize(int startingMoney)
        {
            _currentMoney = startingMoney;
            Debug.Log($"[MoneyManager] 初始化金钱: {_currentMoney}");
        }

        /// <summary>
        /// 增加金钱
        /// </summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[MoneyManager] AddMoney 收到非正数: {amount}");
                return;
            }

            int oldValue = _currentMoney;
            _currentMoney += amount;

            Debug.Log($"[MoneyManager] 金钱 +{amount} ({oldValue} → {_currentMoney})");

            EventBus.Publish(new GameEvents.MoneyChanged
            {
                OldValue = oldValue,
                NewValue = _currentMoney,
                Delta = amount
            });
        }

        /// <summary>
        /// 减少金钱
        /// </summary>
        /// <returns>是否成功扣款（余额充足则扣款成功）</returns>
        public bool ReduceMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[MoneyManager] ReduceMoney 收到非正数: {amount}");
                return true;
            }

            if (_currentMoney < amount)
            {
                Debug.LogWarning($"[MoneyManager] 余额不足！当前: {_currentMoney}, 需要: {amount}");

                EventBus.Publish(new GameEvents.MoneyInsufficient
                {
                    CurrentMoney = _currentMoney,
                    RequiredAmount = amount
                });

                return false;
            }

            int oldValue = _currentMoney;
            _currentMoney -= amount;

            Debug.Log($"[MoneyManager] 金钱 -{amount} ({oldValue} → {_currentMoney})");

            EventBus.Publish(new GameEvents.MoneyChanged
            {
                OldValue = oldValue,
                NewValue = _currentMoney,
                Delta = -amount
            });

            return true;
        }

        /// <summary>
        /// 判断是否能支付指定金额
        /// </summary>
        public bool CanAfford(int amount)
        {
            return _currentMoney >= amount;
        }
    }
}
