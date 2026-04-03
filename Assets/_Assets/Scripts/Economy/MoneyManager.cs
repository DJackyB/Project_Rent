using UnityEngine;
using BaoZuPo.Core;
using Martian.EventBus;

namespace BaoZuPo.Economy
{
    /// <summary>
    /// 金钱管理器：负责玩家余额的增减、事件发布、破产检查等。
    ///
    /// 职责：
    /// 1. 余额管理：维护玩家当前金钱值和累计消费额
    /// 2. 事件发布：通过EventBus发布金钱变动事件，供其他系统监听（UI更新、音效、逻辑触发）
    /// 3. 支付检查：在支付前检查余额充足性，触发"金钱不足"事件
    /// 4. 破产检查：余额<0时认定破产（由GameFlow或LoanSystem在必要时调用）
    ///
    /// 事件时序：
    /// AddMoney/ReduceMoney → 更新_currentMoney → 发布MoneyChanged事件
    /// 特殊: ReduceMoney失败 → 发布MoneyInsufficient事件（不改变余额）
    ///
    /// 关键事件：
    /// - GameEvents.MoneyChanged: {OldValue, NewValue, Delta} → UI更新、触发条件判断
    /// - GameEvents.MoneyInsufficient: {CurrentMoney, RequiredAmount} → UI警告、贷款系统检查
    /// </summary>
    public class MoneyManager : Singleton<MoneyManager>
    {
        [Header("Debug Info (Read Only)")]
        [SerializeField] private int _currentMoney;     // 玩家当前金钱
        [SerializeField] private int _totalSpent;       // 累计消费额（用于数据统计）

        /// <summary>玩家当前持有的金钱</summary>
        public int CurrentMoney => _currentMoney;

        /// <summary>玩家累计消费的总金钱（用于日志和分析）</summary>
        public int TotalSpent => _totalSpent;

        /// <summary>
        /// 初始化玩家金钱。在游戏开始时调用，设置初始余额。
        /// </summary>
        /// <param name="startingMoney">初始金钱数</param>
        public void Initialize(int startingMoney)
        {
            _currentMoney = startingMoney;
            _totalSpent = 0;
            Debug.Log($"[MoneyManager] Initialized money: {_currentMoney}");
        }

        /// <summary>
        /// 增加金钱。同时发布MoneyChanged事件供UI、触发条件等系统监听。
        /// [伪代码]
        /// 1. 验证amount > 0
        /// 2. 保存旧值
        /// 3. 更新_currentMoney
        /// 4. 发布MoneyChanged事件 {OldValue, NewValue, Delta}
        /// 5. 打印日志
        /// 注：不影响_totalSpent（仅记录消费）
        /// </summary>
        /// <param name="amount">增加的金钱数（必须为正数）</param>
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

            // 发布事件，供GameFlow、UI等系统监听
            EventBus.Publish(new GameEvents.MoneyChanged
            {
                OldValue = oldValue,
                NewValue = _currentMoney,
                Delta = amount
            });
        }

        /// <summary>
        /// 减少金钱（支付）。若余额不足，拒绝支付并发布MoneyInsufficient事件。
        /// [伪代码]
        /// 1. 验证amount > 0
        /// 2. 检查 _currentMoney >= amount:
        ///    - 不足: 发布MoneyInsufficient事件 → 返回false（余额不变）
        ///    - 充足: 保存旧值 → 更新_currentMoney -= amount → _totalSpent += amount
        ///           发布MoneyChanged事件 {OldValue, NewValue, Delta=-amount} → 返回true
        /// 3. 打印日志
        ///
        /// 事件发布时序（关键）：
        /// - 支付成功：MoneyChanged → 其他系统更新（UI、卡牌效果等）
        /// - 支付失败：MoneyInsufficient → 贷款系统检查、UI警告
        /// </summary>
        /// <param name="amount">减少的金钱数（必须为正数）</param>
        /// <returns>支付是否成功。false表示余额不足，余额保持不变</returns>
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

                // 发布不足事件，供贷款系统、UI等响应
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

            // 发布成功支付事件
            EventBus.Publish(new GameEvents.MoneyChanged
            {
                OldValue = oldValue,
                NewValue = _currentMoney,
                Delta = -amount
            });

            return true;
        }

        /// <summary>
        /// 检查玩家是否能够承担指定金额的支付。返回true表示余额充足。
        /// 常用于卡牌可玩性检查、UI启用禁用判断等。
        /// </summary>
        /// <param name="amount">要检查的金额</param>
        /// <returns>是否能够支付</returns>
        public bool CanAfford(int amount)
        {
            return _currentMoney >= amount;
        }
    }
}
