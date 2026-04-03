using BaoZuPo.Board;

namespace BaoZuPo.GameFlow
{
    /// <summary>
    /// 游戏阶段枚举
    ///
    /// 值：
    /// - Prepare：准备阶段（抽卡、前置效果）
    /// - Action：行动阶段（玩家出卡）
    /// - Settle：结算阶段（租金、耐久减少、贷款、奖励）
    ///
    /// 循环流程：Prepare -> Action -> Settle -> (下一回合) Prepare -> ...
    /// </summary>
    public enum GamePhase
    {
        Prepare,
        Action,
        Settle
    }

    /// <summary>
    /// 卡牌出牌所需的目标类型
    ///
    /// 值：
    /// - PlayArea：在全局区域出牌（无需选择房间）
    ///   * 合同卡（Contract）
    ///   * 全局即发卡
    /// - Room：需要选择目标房间
    ///   * 租户卡（Tenant）
    ///   * 装备卡（Equipment）
    ///   * 需要房间目标的即发卡
    ///
    /// 用于 UI 和出牌验证，决定是否显示房间选择器
    /// </summary>
    public enum CardPlayTargetKind
    {
        PlayArea,
        Room
    }

    /// <summary>
    /// 卡牌出牌阻止原因
    ///
    /// 值说明：
    /// - None：无阻止，可以出牌
    /// - GameOver：游戏已结束
    /// - NotActionPhase：当前不是行动阶段或行动阶段已结束
    /// - InsufficientMoney：金钱不足，无法支付卡牌费用
    /// - MissingTarget：需要房间目标但未提供
    /// - InvalidTarget：目标房间不存在或已销毁
    /// - TargetFull：目标房间不能放置该类型卡牌（租户满、装备满）
    ///
    /// 用于日志、调试和 UI 反馈
    /// </summary>
    public enum CardPlayBlockReason
    {
        None,
        GameOver,
        NotActionPhase,
        InsufficientMoney,
        MissingTarget,
        InvalidTarget,
        TargetFull
    }

    /// <summary>
    /// 卡牌出牌验证结果
    ///
    /// 成员：
    /// - IsValid：是否通过验证
    /// - RequiredTargetKind：卡牌所需的目标类型（用于 UI 提示）
    /// - BlockReason：阻止原因（如果验证失败）
    /// - TargetRoom：经过验证的目标房间（如果需要）
    ///
    /// 用法：
    /// var result = TurnManager.Instance.ValidatePlay(card, targetRoom);
    /// if (result.IsValid)
    /// {
    ///     TurnManager.Instance.PlayCard(card, result.TargetRoom);
    /// }
    /// else
    /// {
    ///     Debug.Log($"出牌失败：{result.BlockReason}");
    /// }
    ///
    /// 工厂方法：
    /// - Success(targetKind, room)：构建成功结果
    /// - Failure(reason, targetKind, room)：构建失败结果
    /// </summary>
    public readonly struct CardPlayValidationResult
    {
        public bool IsValid { get; }
        public CardPlayTargetKind RequiredTargetKind { get; }
        public CardPlayBlockReason BlockReason { get; }
        public RoomSlot TargetRoom { get; }

        public CardPlayValidationResult(
            bool isValid,
            CardPlayTargetKind requiredTargetKind,
            CardPlayBlockReason blockReason,
            RoomSlot targetRoom)
        {
            IsValid = isValid;
            RequiredTargetKind = requiredTargetKind;
            BlockReason = blockReason;
            TargetRoom = targetRoom;
        }

        public static CardPlayValidationResult Success(CardPlayTargetKind requiredTargetKind, RoomSlot targetRoom)
        {
            return new CardPlayValidationResult(true, requiredTargetKind, CardPlayBlockReason.None, targetRoom);
        }

        public static CardPlayValidationResult Failure(
            CardPlayBlockReason blockReason,
            CardPlayTargetKind requiredTargetKind,
            RoomSlot targetRoom = null)
        {
            return new CardPlayValidationResult(false, requiredTargetKind, blockReason, targetRoom);
        }
    }
}
