using Martian.Audio;
using UnityEngine;

namespace BaoZuPo.Integration
{
    /// <summary>
    /// 运行时音量控制器。
    /// 将四个 AudioBus 的音量暴露到 Inspector，支持 Play Mode 下实时调整。
    ///
    /// 安装：
    /// - 挂在与 AudioBootstrap 相同的 GameObject 上
    /// - AudioBootstrap.Awake 初始化后端，本组件 Start 写入初始音量
    ///
    /// 用法（设置面板 Slider 绑定）：
    /// - Slider.onValueChanged → SetMasterVolume / SetMusicVolume / SetSfxVolume / SetUiVolume
    /// </summary>
    public sealed class AudioVolumeController : MonoBehaviour
    {
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume  = 1f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume    = 1f;
        [SerializeField] [Range(0f, 1f)] private float _uiVolume     = 1f;

        private void Start()
        {
            ApplyAll();
        }

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            AudioServices.Current.SetBusVolume(AudioBus.Master, _masterVolume);
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            AudioServices.Current.SetBusVolume(AudioBus.Music, _musicVolume);
        }

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            AudioServices.Current.SetBusVolume(AudioBus.Sfx, _sfxVolume);
        }

        public void SetUiVolume(float value)
        {
            _uiVolume = Mathf.Clamp01(value);
            AudioServices.Current.SetBusVolume(AudioBus.Ui, _uiVolume);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            ApplyAll();
        }
#endif

        private void ApplyAll()
        {
            AudioServices.Current.SetBusVolume(AudioBus.Master, _masterVolume);
            AudioServices.Current.SetBusVolume(AudioBus.Music,  _musicVolume);
            AudioServices.Current.SetBusVolume(AudioBus.Sfx,    _sfxVolume);
            AudioServices.Current.SetBusVolume(AudioBus.Ui,     _uiVolume);
        }
    }
}
