using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.Audio
{
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Martian/Audio/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [SerializeField] private List<AudioCueDefinition> _cues = new();

        private readonly Dictionary<string, AudioCueDefinition> _lookup = new(StringComparer.Ordinal);
        private bool _lookupDirty = true;

        public List<AudioCueDefinition> Cues => _cues;

        public bool TryGetCue(string cueId, out AudioCueDefinition cue)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                cue = null;
                return false;
            }

            EnsureLookup();
            return _lookup.TryGetValue(cueId, out cue);
        }

        internal void MarkLookupDirty()
        {
            _lookupDirty = true;
        }

        private void OnEnable()
        {
            _lookupDirty = true;
        }

        private void OnValidate()
        {
            _lookupDirty = true;
        }

        private void EnsureLookup()
        {
            if (!_lookupDirty)
            {
                return;
            }

            _lookupDirty = false;
            _lookup.Clear();
            if (_cues == null)
            {
                return;
            }

            for (int i = 0; i < _cues.Count; i++)
            {
                var cue = _cues[i];
                if (cue == null || string.IsNullOrWhiteSpace(cue.id))
                {
                    continue;
                }

                _lookup[cue.id] = cue;
            }
        }
    }
}
