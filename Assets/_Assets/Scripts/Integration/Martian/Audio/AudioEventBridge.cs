using BaoZuPo.Core;
using Martian.Audio;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.Integration
{
    /// <summary>
    /// 音频事件桥接器。
    /// 将游戏逻辑事件（EventBus）映射到音频系统（Martian.Audio）的调用。
    ///
    /// 事件映射：
    /// - CardPlayed → sfx.card.play（卡牌打出音效）
    /// - MoneyChanged → sfx.coin.gain/lose（金币获得/消费音效）
    /// - TurnStarted → sfx.turn.start（回合开始音效）
    /// - CardRewardOffered → sfx.reward.show（奖励出现音效）
    /// - CardRewardSelected → sfx.reward.pick（奖励选择音效）
    /// - GameOver → bgm.result（游戏结束音乐切换）
    ///
    /// 安装：
    /// - 挂在与 AudioBootstrap 相同的 GameObject 上
    /// - OnEnable/OnDisable 自动管理 EventBus 订阅，无需手工接线
    /// </summary>
    public sealed class AudioEventBridge : MonoBehaviour
    {
        /// <summary>
        /// 启用时订阅所有游戏事件。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            EventBus.Subscribe<GameEvents.CardRewardSelected>(OnRewardSelected);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
        }

        /// <summary>
        /// 禁用时取消所有订阅。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            EventBus.Unsubscribe<GameEvents.CardRewardSelected>(OnRewardSelected);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
        }

        /// <summary>
        /// Start 时播放主 BGM。
        /// 等待 AudioBootstrap.Awake 完成后端初始化后再播，避免首帧丢音乐。
        /// </summary>
        private void Start()
        {
            AudioServices.Current.PlayMusic("bgm.main");
        }

        private void OnCardPlayed(GameEvents.CardPlayed _)            => Play("sfx.card.play");
        private void OnTurnStarted(GameEvents.TurnStarted _)          => Play("sfx.turn.start");
        private void OnRewardOffered(GameEvents.CardRewardOffered _)   => Play("sfx.reward.show");
        private void OnRewardSelected(GameEvents.CardRewardSelected _) => Play("sfx.reward.pick");

        private void OnMoneyChanged(GameEvents.MoneyChanged e)
            => Play(e.Delta > 0 ? "sfx.coin.gain" : "sfx.coin.lose");

        private void OnGameOver(GameEvents.GameOver _)
            => AudioServices.Current.PlayMusic("bgm.result");

        private static void Play(string cueId)
            => AudioServices.Current.Play(AudioPlayRequest.Create(cueId));
    }
}
