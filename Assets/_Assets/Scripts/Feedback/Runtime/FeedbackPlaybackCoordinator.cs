using System;
using System.Collections.Generic;
using BaoZuPo.Feedback.Core;
using BaoZuPo.Feedback.UI;
using BaoZuPo.UI.Common.Sequence;
using UnityEngine;

namespace BaoZuPo.Feedback.Runtime
{
    public class FeedbackPlaybackCoordinator : MonoBehaviour, IFeedbackService
    {
        [Header("Feedback Playback")]
        [SerializeField] private bool enableLogs;

        private readonly Dictionary<string, UISequenceTextController> _activeTracks = new();
        private readonly Dictionary<UISequenceTextController, string> _playerToKey = new();
        private readonly Stack<UISequenceTextController> _playerPool = new();

        public event Action AllPlaybackCompleted;

        public bool IsAvailable => isActiveAndEnabled;

        public void Configure(bool logsEnabled)
        {
            enableLogs = logsEnabled;
        }

        public void Publish(FeedbackRequest request)
        {
            if (request == null)
            {
                return;
            }

            var playbackRequest = FeedbackStyleResolver.BuildPlaybackRequest(request);
            EnqueueOnTrack(request.TargetKey, playbackRequest);
        }

        public void PublishSequence(FeedbackSequenceRequest request)
        {
            if (request == null)
            {
                return;
            }

            var playbackRequest = FeedbackStyleResolver.BuildPlaybackRequest(request);
            EnqueueOnTrack(request.TargetKey, playbackRequest);
        }

        private void OnDisable()
        {
            ReleaseAllTracks();
        }

        private void EnqueueOnTrack(string targetKey, UISequencePlaybackRequest playbackRequest)
        {
            if (playbackRequest == null || playbackRequest.Steps == null || playbackRequest.Steps.Count == 0)
            {
                return;
            }

            string resolvedKey = string.IsNullOrWhiteSpace(targetKey) ? "__global__" : targetKey;
            UISequenceTextController track = GetOrCreateTrack(resolvedKey);
            if (track == null)
            {
                if (enableLogs)
                {
                    Debug.LogWarning($"[FeedbackPlaybackCoordinator] Failed to create track for '{resolvedKey}'.", this);
                }

                return;
            }

            track.Enqueue(playbackRequest);
        }

        private UISequenceTextController GetOrCreateTrack(string targetKey)
        {
            if (_activeTracks.TryGetValue(targetKey, out var existingTrack) && existingTrack != null)
            {
                return existingTrack;
            }

            UISequenceTextController track = _playerPool.Count > 0 ? _playerPool.Pop() : CreateTrack();
            if (track == null)
            {
                return null;
            }

            _activeTracks[targetKey] = track;
            _playerToKey[track] = targetKey;
            track.transform.SetParent(transform, false);
            track.gameObject.SetActive(true);
            return track;
        }

        private UISequenceTextController CreateTrack()
        {
            var trackObject = new GameObject("FeedbackTrack", typeof(RectTransform));
            trackObject.transform.SetParent(transform, false);

            var track = trackObject.AddComponent<UISequenceTextController>();
            track.PlaybackCompleted += () => OnTrackPlaybackCompleted(track);
            return track;
        }

        private void OnTrackPlaybackCompleted(UISequenceTextController completedTrack)
        {
            if (completedTrack == null || !_playerToKey.TryGetValue(completedTrack, out var key))
            {
                if (_activeTracks.Count == 0)
                {
                    AllPlaybackCompleted?.Invoke();
                }

                return;
            }

            _activeTracks.Remove(key);
            _playerToKey.Remove(completedTrack);

            completedTrack.HideImmediate();
            completedTrack.gameObject.SetActive(false);
            completedTrack.transform.SetParent(transform, false);
            _playerPool.Push(completedTrack);

            if (_activeTracks.Count == 0)
            {
                AllPlaybackCompleted?.Invoke();
            }
        }

        private void ReleaseAllTracks()
        {
            foreach (var pair in _activeTracks)
            {
                if (pair.Value != null)
                {
                    pair.Value.HideImmediate();
                    pair.Value.gameObject.SetActive(false);
                    pair.Value.transform.SetParent(transform, false);
                    _playerPool.Push(pair.Value);
                }
            }

            _activeTracks.Clear();
            _playerToKey.Clear();
        }
    }
}
