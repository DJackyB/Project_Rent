using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;

namespace Martian.Tooltip
{
    [MovedFrom("BaoZuPo.UI.Common.Tooltip")]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ITooltipContentProvider _provider;
        private object _activeOwner;

        public void Bind(ITooltipContentProvider provider)
        {
            _provider = provider;
        }

        public void Unbind(ITooltipContentProvider provider)
        {
            if (_provider == provider)
            {
                _provider = null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled)
            {
                return;
            }

            ResolveProvider();
            if (_provider == null || !_provider.TryBuildTooltipRequest(out var request) || request == null)
            {
                return;
            }

            if (request.Anchor == null || request.Content == null || !request.Anchor.gameObject.activeInHierarchy)
            {
                return;
            }

            _activeOwner = request.Owner;
            TooltipServices.Current.Show(request, eventData != null ? eventData.position : (Vector2?)null);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideActive();
        }

        private void OnDisable()
        {
            HideActive();
        }

        private void OnEnable()
        {
            ResolveProvider();
        }

        private void HideActive()
        {
            if (_activeOwner != null)
            {
                TooltipServices.Current.Hide(_activeOwner);
                _activeOwner = null;
            }
        }

        private void ResolveProvider()
        {
            if (_provider != null)
            {
                return;
            }

            var behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ITooltipContentProvider provider)
                {
                    _provider = provider;
                    return;
                }
            }
        }
    }
}
