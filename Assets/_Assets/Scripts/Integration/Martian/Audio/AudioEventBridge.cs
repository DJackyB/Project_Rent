using BaoZuPo.Core;
using Martian.Audio;
using Martian.EventBus;
using UnityEngine;

namespace BaoZuPo.Integration
{
    /// <summary>
    /// 将 EventBus 游戏事件映射到 AudioServices 调用。
    /// 挂在与 AudioBootstrap 相同的 GameObject 上，OnEnable/OnDisable 自动管理订阅。
    /// </summary>
    public sealed class AudioEventBridge : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Subscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Subscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Subscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            EventBus.Subscribe<GameEvents.CardRewardSelected>(OnRewardSelected);
            EventBus.Subscribe<GameEvents.GameOver>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.CardPlayed>(OnCardPlayed);
            EventBus.Unsubscribe<GameEvents.MoneyChanged>(OnMoneyChanged);
            EventBus.Unsubscribe<GameEvents.TurnStarted>(OnTurnStarted);
            EventBus.Unsubscribe<GameEvents.CardRewardOffered>(OnRewardOffered);
            EventBus.Unsubscribe<GameEvents.CardRewardSelected>(OnRewardSelected);
            EventBus.Unsubscribe<GameEvents.GameOver>(OnGameOver);
        }

        private void Start()
        {
            // 等 AudioBootstrap.Awake 安装完后端后再播首段 BGM，避免首帧丢音乐。
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
