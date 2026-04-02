using Martian.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 挂在 Button 上，点击时播放指定音效。
    /// 通用按钮音效走此组件，不经过 EventBus。
    /// </summary>
    public sealed class AudioOnClick : MonoBehaviour
    {
        [SerializeField] private string _cueId = "ui.button";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
            => AudioServices.Current.Play(AudioPlayRequest.Create(_cueId));
    }
}
