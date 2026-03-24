using UnityEngine;

namespace BaoZuPo.UI
{
    public static class UIRuntimeSpriteUtility
    {
        private static Sprite _whiteSprite;

        public static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            var texture = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            _whiteSprite.name = "UIRuntimeWhiteSprite";
            return _whiteSprite;
        }
    }
}
