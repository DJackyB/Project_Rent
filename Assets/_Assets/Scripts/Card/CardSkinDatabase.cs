using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌美术资源数据库（ScriptableObject）。
    ///
    /// 集中管理卡牌的美术表现，包括：
    /// - 卡牌头像（cardArt）：由卡牌类型决定
    /// - 卡牌边框：由稀有度决定
    ///
    /// 在 CardUI 渲染时使用，支持按卡牌属性查询对应美术资源，
    /// 如果查询失败则返回默认资源。
    ///
    /// 配置方式：
    /// 1. 创建 CardSkinDatabase.asset
    /// 2. 设置默认头像和边框
    /// 3. 在"Face Mapping"中，为每个卡牌类型指定头像 Sprite
    /// 4. 在"Frame Mapping"中，为每个稀有度指定边框 Sprite
    /// </summary>
    [CreateAssetMenu(fileName = "CardSkinDatabase", menuName = "BaoZuPo/Card Skin Database")]
    public class CardSkinDatabase : ScriptableObject
    {
        /// <summary>卡牌类型到头像的映射条目。</summary>
        [System.Serializable]
        private struct CardTypeSkin
        {
            /// <summary>卡牌类型。</summary>
            public CardType cardType;

            /// <summary>该类型的头像 Sprite。</summary>
            public Sprite faceSprite;
        }

        /// <summary>稀有度到边框的映射条目。</summary>
        [System.Serializable]
        private struct CardRaritySkin
        {
            /// <summary>卡牌稀有度。</summary>
            public CardRarity rarity;

            /// <summary>该稀有度的边框 Sprite。</summary>
            public Sprite frameSprite;
        }

        [Header("Default Assets")]
        /// <summary>默认头像。当没有为卡牌类型配置头像时使用。</summary>
        [SerializeField] private Sprite defaultFaceSprite;

        /// <summary>默认边框。当没有为稀有度配置边框时使用。</summary>
        [SerializeField] private Sprite defaultFrameSprite;

        [Header("Face Mapping By Card Type")]
        /// <summary>卡牌类型到头像的映射列表。</summary>
        [SerializeField] private List<CardTypeSkin> typeSkins = new();

        [Header("Frame Mapping By Rarity")]
        /// <summary>稀有度到边框的映射列表。</summary>
        [SerializeField] private List<CardRaritySkin> raritySkins = new();

        /// <summary>默认头像 Sprite。</summary>
        public Sprite DefaultFaceSprite => defaultFaceSprite;

        /// <summary>默认边框 Sprite。</summary>
        public Sprite DefaultFrameSprite => defaultFrameSprite;

        /// <summary>
        /// 按卡牌类型查询头像。
        ///
        /// 流程：
        /// 1. 在 typeSkins 中查找匹配类型的条目
        /// 2. 如果找到且 Sprite 非 null，返回该 Sprite
        /// 3. 否则返回默认头像
        ///
        /// 参数：
        /// - cardType：卡牌类型（Tenant、Equipment 等）
        ///
        /// 返回：匹配的 Sprite，或默认 Sprite
        /// </summary>
        public Sprite GetFaceSprite(CardType cardType)
        {
            for (int i = 0; i < typeSkins.Count; i++)
            {
                if (typeSkins[i].cardType == cardType && typeSkins[i].faceSprite != null)
                {
                    return typeSkins[i].faceSprite;
                }
            }

            return defaultFaceSprite;
        }

        /// <summary>
        /// 按稀有度查询边框。
        ///
        /// 流程：
        /// 1. 在 raritySkins 中查找匹配稀有度的条目
        /// 2. 如果找到且 Sprite 非 null，返回该 Sprite
        /// 3. 否则返回默认边框
        ///
        /// 参数：
        /// - rarity：卡牌稀有度（Common、Rare、Epic、Legendary）
        ///
        /// 返回：匹配的 Sprite，或默认 Sprite
        /// </summary>
        public Sprite GetFrameSprite(CardRarity rarity)
        {
            for (int i = 0; i < raritySkins.Count; i++)
            {
                if (raritySkins[i].rarity == rarity && raritySkins[i].frameSprite != null)
                {
                    return raritySkins[i].frameSprite;
                }
            }

            return defaultFrameSprite;
        }
    }
}
