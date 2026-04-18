using System;
using System.Collections.Generic;
using Martian.Feedback;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Martian.Feedback.Runtime
{
    /// <summary>
    /// 浮动文本反馈后端实现。使用 UI 文本和动画显示玩家反馈。
    ///
    /// 架构：
    /// - Host：宿主 Transform（通常是 Canvas 或 UI 根）
    /// - Canvas：根据 options.SortingOrder 创建或利用现有 Canvas
    /// - MartianFeedbackLayer：所有浮动文本的父节点 (RectTransform)
    /// - FeedbackPlaybackTrack（多个）：每个车道一个，按顺序播放反馈
    ///
    /// 队列管理：
    /// - 同一 LaneKey 的反馈在 FeedbackPlaybackTrack 中排队
    /// - 不同 LaneKey 的反馈在不同 Track 中并发播放
    /// - 使用对象池管理 Track 以减少分配开销
    ///
    /// 生命周期：
    /// 1. Attach(host)：在宿主 Transform 下创建 Canvas、Layer、Track 等
    /// 2. Publish(request)：反馈入队到对应 Track
    /// 3. Track 播放反馈文本动画
    /// 4. Track 完成后归还到对象池
    /// 5. Clear()：销毁所有对象，重置状态
    /// </summary>
    public sealed class FloatingTextFeedbackBackend : IFeedbackPlaybackBackend, IFeedbackFontResolver
    {
        /// <summary>反馈系统挂载的宿主 Transform。</summary>
        private Transform _host;

        /// <summary>根 Canvas，所有浮动文本都在其下。</summary>
        private Canvas _canvas;

        /// <summary>根 Canvas 的 RectTransform。用于位置计算。</summary>
        private RectTransform _canvasRect;

        /// <summary>反馈层根节点。所有 Track 和文本都在其下。</summary>
        private RectTransform _layerRoot;

        /// <summary>当前运行时配置（排序顺序等）。</summary>
        private FeedbackRuntimeOptions _options = new();
        private Func<TMP_FontAsset> _fontResolver;

        /// <summary>活跃中的播放轨道。Key = LaneKey，Value = 对应的 Track。</summary>
        private readonly Dictionary<string, FeedbackPlaybackTrack> _activeTracks = new();

        /// <summary>Track 所有者映射。用于追踪 Track 属于哪个 Lane。</summary>
        private readonly Dictionary<FeedbackPlaybackTrack, string> _trackOwners = new();

        /// <summary>Track 对象池。已完成的 Track 放回此池以重复利用。</summary>
        private readonly Stack<FeedbackPlaybackTrack> _trackPool = new();

        /// <summary>所有播放任务完成时触发。</summary>
        public event Action AllPlaybackCompleted;

        /// <summary>后端是否可用（Host 和 Layer 都已初始化）。</summary>
        public bool IsAvailable => _host != null && _layerRoot != null;

        public void SetFontResolver(Func<TMP_FontAsset> fontResolver)
        {
            _fontResolver = fontResolver;

            foreach (var track in _activeTracks.Values)
            {
                track?.SetFontResolver(_fontResolver);
            }

            foreach (var track in _trackPool)
            {
                track?.SetFontResolver(_fontResolver);
            }
        }

        public void Attach(Transform host)
        {
            if (_host == host)
            {
                EnsureLayerRoot();
                return;
            }

            Clear();
            _host = host;
            EnsureLayerRoot();
        }

        public void Configure(FeedbackRuntimeOptions options)
        {
            _options = options != null ? options.Clone() : new FeedbackRuntimeOptions();
            EnsureLayerRoot();
            foreach (var track in _activeTracks.Values)
            {
                if (track != null)
                {
                    track.Configure(_canvas, _canvasRect, _options);
                }
            }
        }

        public FeedbackPlaybackHandle Publish(FeedbackRequest request)
        {
            if (!IsEnabled() || request == null)
            {
                return CreateCancelledHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            var playback = FeedbackPlaybackFormatting.Create(_options, request);
            return Enqueue(request.LaneKey, request.TargetKey, playback);
        }

        public FeedbackPlaybackHandle PublishSequence(FeedbackSequenceRequest request)
        {
            if (!IsEnabled() || request == null)
            {
                return CreateCancelledHandle(request != null ? request.LaneKey : null, request != null ? request.TargetKey : null);
            }

            var playback = FeedbackPlaybackFormatting.Create(_options, request);
            return Enqueue(request.LaneKey, request.TargetKey, playback);
        }

        public void Clear()
        {
            foreach (var track in _activeTracks.Values)
            {
                if (track != null)
                {
                    track.Clear();
                    DestroyObject(track.gameObject);
                }
            }

            foreach (var track in _trackPool)
            {
                if (track != null)
                {
                    track.Clear();
                    DestroyObject(track.gameObject);
                }
            }

            _activeTracks.Clear();
            _trackOwners.Clear();
            _trackPool.Clear();

            if (_layerRoot != null)
            {
                DestroyObject(_layerRoot.gameObject);
                _layerRoot = null;
            }

            _canvas = null;
            _canvasRect = null;
        }

        internal int ActiveTrackCount => _activeTracks.Count;

        internal int GetPendingCount(string laneKey)
        {
            string resolvedKey = ResolveLaneKey(laneKey, laneKey);
            if (!_activeTracks.TryGetValue(resolvedKey, out var track) || track == null)
            {
                return 0;
            }

            return track.PendingCount;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        internal void CompleteTrackForTesting(string laneKey)
        {
            string resolvedKey = ResolveLaneKey(laneKey, laneKey);
            if (!_activeTracks.TryGetValue(resolvedKey, out var track) || track == null)
            {
                return;
            }

            track.CompleteCurrentForTesting();
        }

        private bool IsEnabled()
        {
            return _options == null || _options.EnableFeedback;
        }

        private FeedbackPlaybackHandle Enqueue(string laneKey, string targetKey, FeedbackPlaybackRequest playback)
        {
            if (playback == null || playback.Steps == null || playback.Steps.Count == 0)
            {
                return CreateCancelledHandle(laneKey, targetKey);
            }

            EnsureLayerRoot();
            if (_layerRoot == null)
            {
                return CreateCancelledHandle(laneKey, targetKey);
            }

            string resolvedKey = ResolveLaneKey(laneKey, targetKey);
            var track = GetOrCreateTrack(resolvedKey);
            if (track == null)
            {
                return CreateCancelledHandle(laneKey, targetKey);
            }

            playback.LaneKey = resolvedKey;
            playback.Handle = new FeedbackPlaybackHandle(resolvedKey, targetKey);
            track.Enqueue(playback);
            return playback.Handle;
        }

        private FeedbackPlaybackTrack GetOrCreateTrack(string targetKey)
        {
            if (_activeTracks.TryGetValue(targetKey, out var existingTrack) && existingTrack != null)
            {
                return existingTrack;
            }

            FeedbackPlaybackTrack track = _trackPool.Count > 0 ? _trackPool.Pop() : CreateTrack();
            if (track == null)
            {
                return null;
            }

            _activeTracks[targetKey] = track;
            _trackOwners[track] = targetKey;
            track.transform.SetParent(_layerRoot, false);
            track.gameObject.SetActive(true);
            track.SetFontResolver(_fontResolver);
            track.Configure(_canvas, _canvasRect, _options);
            return track;
        }

        private FeedbackPlaybackTrack CreateTrack()
        {
            var trackObject = new GameObject("MartianFeedbackTrack", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(FeedbackPlaybackTrack));
            trackObject.transform.SetParent(_layerRoot, false);

            var track = trackObject.GetComponent<FeedbackPlaybackTrack>();
            track.PlaybackCompleted += () => OnTrackPlaybackCompleted(track);
            track.SetFontResolver(_fontResolver);
            track.Configure(_canvas, _canvasRect, _options);
            return track;
        }

        private void OnTrackPlaybackCompleted(FeedbackPlaybackTrack completedTrack)
        {
            if (completedTrack == null || !_trackOwners.TryGetValue(completedTrack, out var key))
            {
                if (_activeTracks.Count == 0)
                {
                    AllPlaybackCompleted?.Invoke();
                }

                return;
            }

            _activeTracks.Remove(key);
            _trackOwners.Remove(completedTrack);
            completedTrack.Clear();
            completedTrack.gameObject.SetActive(false);
            completedTrack.transform.SetParent(_layerRoot, false);
            _trackPool.Push(completedTrack);

            if (_activeTracks.Count == 0)
            {
                AllPlaybackCompleted?.Invoke();
            }
        }

        private void EnsureLayerRoot()
        {
            if (_host == null)
            {
                return;
            }

            if (_canvas == null)
            {
                _canvas = _host.GetComponentInParent<Canvas>();
            }

            if (_canvas == null)
            {
                var canvasObject = new GameObject("MartianFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(_host, false);

                _canvas = canvasObject.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = _options.SortingOrder;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            _canvasRect = _canvas.transform as RectTransform;
            if (_canvasRect == null || _layerRoot != null)
            {
                return;
            }

            var layerObject = new GameObject("MartianFeedbackLayer", typeof(RectTransform), typeof(CanvasGroup));
            layerObject.transform.SetParent(_canvas.transform, false);

            _layerRoot = layerObject.GetComponent<RectTransform>();
            _layerRoot.anchorMin = Vector2.zero;
            _layerRoot.anchorMax = Vector2.one;
            _layerRoot.offsetMin = Vector2.zero;
            _layerRoot.offsetMax = Vector2.zero;
            _layerRoot.SetAsLastSibling();

            var canvasGroup = layerObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private static string ResolveLaneKey(string laneKey, string targetKey)
        {
            if (!string.IsNullOrWhiteSpace(laneKey))
            {
                return laneKey;
            }

            if (!string.IsNullOrWhiteSpace(targetKey))
            {
                return targetKey;
            }

            return "__global__";
        }

        private static FeedbackPlaybackHandle CreateCancelledHandle(string laneKey, string targetKey)
        {
            var handle = new FeedbackPlaybackHandle(ResolveLaneKey(laneKey, targetKey), targetKey);
            handle.Cancel();
            return handle;
        }
    }
}
