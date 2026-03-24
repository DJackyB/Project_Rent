using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI.Common.Hover
{
    public class HoverPreviewController : MonoBehaviour
    {
        private static HoverPreviewController _instance;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private IHoverPreviewPresenter _presenter;
        private IHoverPreviewSource _currentSource;
        private Vector2 _currentOffset = new Vector2(24f, -24f);

        public static HoverPreviewController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var controllerObject = new GameObject("HoverPreviewController");
                    DontDestroyOnLoad(controllerObject);
                    _instance = controllerObject.AddComponent<HoverPreviewController>();
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
            EnsurePresenter();
        }

        private void LateUpdate()
        {
            if (_presenter == null || _presenter.Root == null || _currentSource == null)
            {
                return;
            }

            if (_currentSource.HoverSourceObject == null || !_currentSource.HoverSourceObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            var mousePosition = Input.mousePosition;
            _presenter.Root.anchoredPosition = HoverPreviewPositioner.CalculateClampedPosition(
                _canvasRect,
                _presenter.Root,
                mousePosition,
                _currentOffset);
            _presenter.Root.SetAsLastSibling();
        }

        public void Show(IHoverPreviewSource source)
        {
            if (source == null || source.HoverSourceObject == null || !source.HoverSourceObject.activeInHierarchy)
            {
                return;
            }

            EnsureCanvas();
            EnsurePresenter();

            _currentSource = source;
            _presenter.Show(new HoverPreviewRequest(source, Input.mousePosition));

            if (_presenter.Root != null)
            {
                _presenter.Root.anchoredPosition = HoverPreviewPositioner.CalculateClampedPosition(
                    _canvasRect,
                    _presenter.Root,
                    Input.mousePosition,
                    _currentOffset);
            }
        }

        public void HideIfCurrent(IHoverPreviewSource source)
        {
            if (source != null && _currentSource == source)
            {
                Hide();
            }
        }

        public void Hide()
        {
            _currentSource = null;
            _presenter?.Hide();
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;
            _canvasRect = _canvas.transform as RectTransform;

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsurePresenter()
        {
            if (_presenter != null)
            {
                return;
            }

            var presenterTransform = transform.Find("CardHoverPreview");
            if (presenterTransform == null)
            {
                presenterTransform = new GameObject("CardHoverPreview").transform;
                presenterTransform.SetParent(transform, false);
            }

            var presenter = presenterTransform.GetComponent<CardHoverPreviewPresenter>();
            if (presenter == null)
            {
                presenter = presenterTransform.gameObject.AddComponent<CardHoverPreviewPresenter>();
            }

            _presenter = presenter;
        }
    }
}
