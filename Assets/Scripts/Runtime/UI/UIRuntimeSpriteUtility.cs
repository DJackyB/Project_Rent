using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 运行时精灵加载工具，缓存并提供全局白色精灵用于运行时 UI 创建。
    /// 避免每次都从纹理重新创建精灵，提高性能。
    /// </summary>
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
