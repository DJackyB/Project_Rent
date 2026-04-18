using Martian.EventBus;
using Martian.RandomEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    public class RandomEventPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TextMeshProUGUI[] optionLabels;

        private RandomEventDisplayData _current;

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.AddListener(() => SelectOption(index));
            }
            Hide();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RandomEventTriggered>(OnEventTriggered);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RandomEventTriggered>(OnEventTriggered);
        }

        private void OnEventTriggered(RandomEventTriggered msg)
        {
            _current = msg.DisplayData;
            titleText.text = _current.FormattedTitle;
            descriptionText.text = _current.FormattedDescription;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = i < _current.Options.Count;
                optionButtons[i].gameObject.SetActive(hasOption);
                if (hasOption)
                {
                    optionLabels[i].text = _current.Options[i].FormattedDisplayText;
                    optionButtons[i].interactable = _current.Options[i].IsAvailable;
                }
            }

            Show();
        }

        private void SelectOption(int index)
        {
            Hide();
            EventBus.Publish(new RandomEventOptionSelected
            {
                EventInstanceId     = _current.EventInstanceId,
                SelectedOptionId    = _current.Options[index].OptionId,
                SelectedOptionIndex = index
            });
        }

        private void Show()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
