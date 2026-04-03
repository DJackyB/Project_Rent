using System;
using System.Collections.Generic;
using BaoZuPo.Card;

namespace BaoZuPo.GameFlow
{
    /// <summary>
    /// 卡牌目标选择与验证工具类
    ///
    /// 职责：
    /// 1. 判断卡牌所需的目标类型（PlayArea 或 Room）
    /// 2. 判断卡牌出牌后是否持久存在
    /// 3. 验证卡牌配置的一致性（目标类型与效果）
    /// 4. 列举需要房间目标的效果ID
    /// </summary>
    public static class CardTargeting
    {
        /// <summary>
        /// 显式需要房间目标的效果ID列表
        /// 这些效果在执行时依赖 EffectContext.SelectedRoom
        /// 如果卡牌使用这些效果但 targetKind 不是 Room，则配置有误
        /// </summary>
        private static readonly HashSet<string> ExplicitRoomTargetEffects = new(StringComparer.Ordinal)
        {
            "AddMoneyBySelectedRoomTenantCount",
            "AddTenantDurabilityInSelectedRoom",
            "EvictTenantInSelectedRoom",
            "ExpandSlot",
            "MoveTenantToEmptyRoom",
            "SpawnRandomTenantInSelectedRoom",
            "TriggerSelectedRoomSettle",
        };

        /// <summary>
        /// 获取卡牌所需的目标类型
        ///
        /// 返回值：
        /// - CardPlayTargetKind.Room：卡牌需要指定目标房间（租户、装备、需要房间效果的卡）
        /// - CardPlayTargetKind.PlayArea：卡牌在全局区域出牌（合同、即发卡）
        ///
        /// 数据来源：
        /// - card.targetKind（从 CardData 配置中读取）
        /// </summary>
        public static CardPlayTargetKind GetRequiredTargetKind(CardData card)
        {
            if (card == null)
            {
                return CardPlayTargetKind.PlayArea;
            }

            return card.targetKind;
        }

        /// <summary>
        /// 判断卡牌出牌后是否持久存在于房间
        ///
        /// 返回 true 的条件：
        /// - targetKind == Room
        /// - cardType == Tenant 或 Equipment
        ///
        /// 这些卡牌在出牌后放入目标房间，参与结算与耐久减少
        /// </summary>
        public static bool PersistsInRoom(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            return GetRequiredTargetKind(card) == CardPlayTargetKind.Room
                && (card.cardType == CardType.Tenant || card.cardType == CardType.Equipment);
        }

        /// <summary>
        /// 判断卡牌出牌后是否持久存在为合同
        ///
        /// 返回 true 的条件：
        /// - targetKind == PlayArea
        /// - cardType == Contract
        ///
        /// 合同卡牌在出牌后添加到 BoardManager.Contracts，参与结算但不关联房间
        /// </summary>
        public static bool PersistsAsContract(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            return GetRequiredTargetKind(card) == CardPlayTargetKind.PlayArea
                && card.cardType == CardType.Contract;
        }

        /// <summary>
        /// 验证卡牌的目标类型配置是否一致
        ///
        /// 检查项：
        /// 1. targetKind 枚举值是否有效
        /// 2. 如果卡牌使用需要房间目标的效果，则 targetKind 必须是 Room
        ///
        /// 返回值：
        /// - true：配置正确
        /// - false：配置有误，warning 包含错误描述
        ///
        /// 示例：
        /// 错误：cardType=Equipment, targetKind=PlayArea, instantEffect="EvictTenantInSelectedRoom"
        ///   -> 因为 EvictTenantInSelectedRoom 需要房间目标，但 targetKind 不是 Room
        ///
        /// 这个方法用于 Editor 验证和导表工具检查，防止配置错误在运行时暴露
        /// </summary>
        public static bool TryValidateConfiguredTargetKind(CardData card, out string warning)
        {
            warning = null;
            if (card == null)
            {
                return true;
            }

            if (!Enum.IsDefined(typeof(CardPlayTargetKind), card.targetKind))
            {
                warning = $"Configured target kind '{(int)card.targetKind}' is invalid.";
                return false;
            }

            if (HasSelectedRoomEffect(card) && card.targetKind != CardPlayTargetKind.Room)
            {
                warning = "Cards that use room-dependent effects must use targetKind Room.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查卡牌的四个效果阶段（preEffect、instantEffect、settleEffect、destroyEffect）
        /// 是否包含需要房间目标的效果
        /// </summary>
        private static bool HasSelectedRoomEffect(CardData card)
        {
            return ContainsRoomDependentEffect(card.preEffect)
                || ContainsRoomDependentEffect(card.instantEffect)
                || ContainsRoomDependentEffect(card.settleEffect)
                || ContainsRoomDependentEffect(card.destroyEffect);
        }

        /// <summary>
        /// 解析效果字符串并检查是否包含需要房间目标的效果
        ///
        /// 效果字符串格式：
        /// "EffectId1;param1;param2|EffectId2;param1|EffectId3"
        /// 多个效果用 | 分隔，效果参数用 ; 分隔
        ///
        /// 算法：
        /// 1. 按 | 分隔效果列表
        /// 2. 对每个效果，提取 ; 之前的 EffectId
        /// 3. 检查 EffectId 是否在 ExplicitRoomTargetEffects 中
        /// 4. 只要找到一个，返回 true
        /// </summary>
        private static bool ContainsRoomDependentEffect(string effectString)
        {
            if (string.IsNullOrWhiteSpace(effectString))
            {
                return false;
            }

            var segments = effectString.Split('|');
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                int separatorIndex = segment.IndexOf(';');
                string effectId = separatorIndex >= 0 ? segment[..separatorIndex] : segment;
                if (ExplicitRoomTargetEffects.Contains(effectId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
