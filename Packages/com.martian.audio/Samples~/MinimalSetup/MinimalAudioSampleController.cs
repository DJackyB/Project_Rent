using UnityEngine;

namespace Martian.Audio.Samples
{
    public sealed class MinimalAudioSampleController : MonoBehaviour
    {
        [SerializeField] private string _musicCueId = "bgm.main";
        [SerializeField] private string _confirmCueId = "sfx.confirm";
        [SerializeField] private string _clickCueId = "ui.click";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                AudioServices.Current.PlayMusic(_musicCueId);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                AudioServices.Current.Play(AudioPlayRequest.Create(_confirmCueId));
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                AudioServices.Current.Play(new AudioPlayRequest
                {
                    CueId = _clickCueId,
                    OverrideBus = AudioBus.Ui
                });
            }
        }
    }
}
