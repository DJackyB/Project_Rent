using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 杩愯鏃跺崱鐗屽疄渚嬨€?
    /// 鎸佹湁 CardData 鐨勯潤鎬佸紩鐢紝浠ュ強杩愯鏃跺彲鍙樼姸鎬侊紙鑰愪箙銆佺瓑寰呯瓑锛夈€?
    /// 杩欐槸绾?C# 绫伙紝涓嶇户鎵?MonoBehaviour銆?
    /// </summary>
    public class CardInstance
    {
        /// <summary>闈欐€佹暟鎹紩鐢?/summary>
        public CardData Data { get; private set; }

        /// <summary>褰撳墠鑰愪箙鍊硷紙0 琛ㄧず鏃犻檺鑰愪箙锛?/summary>
        public int CurrentDurability { get; set; }

        /// <summary>褰撳墠绛夊緟鍊掕鏃讹紙0 琛ㄧず鏃犵瓑寰咃級</summary>
        public int CurrentWait { get; set; }

        /// <summary>鎵€鍦ㄦ埧闂存Ы浣嶏紙null 琛ㄧず褰撳墠涓嶅湪鍦轰笂锛?/summary>
        public Board.RoomSlot PlacedRoom { get; set; }

        /// <summary>鏄惁宸茶閿€姣?/summary>
        public bool IsDestroyed { get; private set; }

        // 棰勫厛瑙ｆ瀽濂界殑鏁堟灉瀹炰緥锛岄伩鍏嶈繍琛屾椂閲嶅瑙ｆ瀽瀛楃涓?
        public ICardEffect PreEffect { get; private set; }
        public ICardEffect InstantEffect { get; private set; }
        public ICardEffect SettleEffect { get; private set; }
        public ICardEffect DestroyEffect { get; private set; }

        /// <summary>
        /// 鍩轰簬 CardData 鍒涘缓涓€寮犺繍琛屾椂鍗＄墝瀹炰緥銆?
        /// </summary>
        public CardInstance(CardData data)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            Data = data;
            CurrentDurability = data.durability;
            CurrentWait = data.waitTurns;
            IsDestroyed = false;

            // 灏嗘晥鏋滃瓧绗︿覆棰勮В鏋愭垚 ICardEffect 瀹炰緥
            PreEffect = CardEffectFactory.Create(data.preEffect);
            InstantEffect = CardEffectFactory.Create(data.instantEffect);
            SettleEffect = CardEffectFactory.Create(data.settleEffect);
            DestroyEffect = CardEffectFactory.Create(data.destroyEffect);
        }

        /// <summary>
        /// 鏍囪涓哄凡閿€姣併€?
        /// </summary>
        public void MarkDestroyed()
        {
            IsDestroyed = true;
        }

        public CardRuntimeState CaptureRuntimeState()
        {
            return new CardRuntimeState
            {
                cardId = Data != null ? Data.cardId : 0,
                currentDurability = CurrentDurability,
                currentWait = CurrentWait
            };
        }

        public static bool TryCreateFromRuntimeState(CardRuntimeState state, out CardInstance card, out string error)
        {
            card = null;
            error = null;

            if (state == null)
            {
                error = "Card runtime state is null.";
                return false;
            }

            CardData data;
            try
            {
                data = CardDatabase.GetById(state.cardId);
            }
            catch (System.Exception exception)
            {
                error = $"CardDatabase is not ready: {exception.Message}";
                return false;
            }

            if (data == null)
            {
                error = $"CardData not found for cardId '{state.cardId}'.";
                return false;
            }

            card = new CardInstance(data)
            {
                CurrentDurability = state.currentDurability,
                CurrentWait = state.currentWait
            };

            return true;
        }

        public override string ToString()
        {
            return $"[{Data.cardName}](ID:{Data.cardId}, 鑰愪箙:{CurrentDurability}, 绛夊緟:{CurrentWait})";
        }
    }
}
