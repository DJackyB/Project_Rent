using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    [CreateAssetMenu(fileName = "CardSkinDatabase", menuName = "BaoZuPo/Card Skin Database")]
    public class CardSkinDatabase : ScriptableObject
    {
        [System.Serializable]
        private struct CardTypeSkin
        {
            public CardType cardType;
            public Sprite faceSprite;
        }

        [System.Serializable]
        private struct CardRaritySkin
        {
            public CardRarity rarity;
            public Sprite frameSprite;
        }

        [Header("Default Assets")]
        [SerializeField] private Sprite defaultFaceSprite;
        [SerializeField] private Sprite defaultFrameSprite;

        [Header("Face Mapping By Card Type")]
        [SerializeField] private List<CardTypeSkin> typeSkins = new();

        [Header("Frame Mapping By Rarity")]
        [SerializeField] private List<CardRaritySkin> raritySkins = new();

        public Sprite DefaultFaceSprite => defaultFaceSprite;
        public Sprite DefaultFrameSprite => defaultFrameSprite;

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
