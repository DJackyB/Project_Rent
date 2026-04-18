using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using System.Collections;

namespace Martian.Tooltip
{
    [MovedFrom("BaoZuPo.UI.Common.Tooltip")]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float showDelaySeconds;

        private ITooltipContentProvider _provider;
        private object _activeOwner;
        private Coroutine _pendingShowRoutine;
        private Vector2? _pendingPointerPosition;

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

            _pendingPointerPosition = eventData != null ? eventData.position : (Vector2?)null;
            CancelPendingShow();
            if (showDelaySeconds <= 0f)
            {
                ShowTooltipNow();
            }
            else
            {
                _pendingShowRoutine = StartCoroutine(ShowAfterDelay());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelPendingShow();
            HideActive();
        }

        private void OnDisable()
        {
            CancelPendingShow();
            HideActive();
        }

        private void OnEnable()
        {
            ResolveProvider();
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(showDelaySeconds);
            _pendingShowRoutine = null;
            ShowTooltipNow();
        }

        private void ShowTooltipNow()
        {
            ResolveProvider();
            if (_provider == null || !_provider.TryBuildTooltipRequest(out var request) || request == null)
            {
                return;
            }

            if (request.Owner != null && ReferenceEquals(request.Owner, _activeOwner))
            {
                return;
            }

            HideActive();

            if (request.Anchor == null || request.Content == null || !request.Anchor.gameObject.activeInHierarchy)
            {
                return;
            }

            _activeOwner = request.Owner;
            TooltipServices.Current.Show(request, _pendingPointerPosition);
        }

        private void HideActive()
        {
            if (_activeOwner != null)
            {
                TooltipServices.Current.Hide(_activeOwner);
                _activeOwner = null;
            }
        }

        private void CancelPendingShow()
        {
            if (_pendingShowRoutine != null)
            {
                StopCoroutine(_pendingShowRoutine);
                _pendingShowRoutine = null;
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
