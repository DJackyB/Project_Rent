using UnityEngine;
using UnityEngine.EventSystems;

namespace BaoZuPo.UI.Common.Hover
{
    public class HoverPreviewTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private IHoverPreviewSource _source;

        public void Bind(IHoverPreviewSource source)
        {
            _source = source;
        }

        public void Unbind(IHoverPreviewSource source)
        {
            if (_source == source)
            {
                _source = null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled || _source == null)
            {
                return;
            }

            HoverPreviewController.Instance.Show(_source);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enabled || _source == null)
            {
                return;
            }

            HoverPreviewController.Instance.HideIfCurrent(_source);
        }

        private void OnDisable()
        {
            if (_source != null)
            {
                HoverPreviewController.Instance.HideIfCurrent(_source);
            }
        }
    }
}
