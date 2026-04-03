using Martian.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 点击音效助手，挂在 Button 上自动播放点击音效。
    /// 通用按钮音效在此处理，不经过事件总线，减少耦合。
    /// 可配置音效 ID，默认为 "ui.button"。
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
