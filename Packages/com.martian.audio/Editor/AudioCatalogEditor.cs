using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Martian.Audio.Editor
{
    [CustomEditor(typeof(AudioCatalog))]
    public sealed class AudioCatalogEditor : UnityEditor.Editor
    {
        private SerializedProperty _cues;
        private readonly Dictionary<string, int> _idCounts = new();

        private void OnEnable()
        {
            _cues = serializedObject.FindProperty("_cues");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RebuildIdCounts();

            DrawToolbar();
            EditorGUILayout.Space(8f);

            if (_cues.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No audio cues registered yet. Use Add Cue or Add Selected AudioClips.", MessageType.Info);
            }

            for (int i = 0; i < _cues.arraySize; i++)
            {
                DrawCue(i);
                EditorGUILayout.Space(4f);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Cue"))
                {
                    AddCue("new.cue", AudioBus.Sfx);
                }

                if (GUILayout.Button("Add Selected AudioClips"))
                {
                    AddSelectedAudioClips();
                }

                if (GUILayout.Button("Sort By ID"))
                {
                    SortById();
                }
            }

            EditorGUILayout.HelpBox(
                "This AudioCatalog is the runtime source of truth. Register cue IDs here, then assign one or more AudioClip assets to each cue.",
                MessageType.None);
        }

        private void DrawCue(int index)
        {
            SerializedProperty cue = _cues.GetArrayElementAtIndex(index);
            SerializedProperty id = cue.FindPropertyRelative("id");
            SerializedProperty bus = cue.FindPropertyRelative("bus");
            SerializedProperty clips = cue.FindPropertyRelative("clips");
            SerializedProperty baseVolume = cue.FindPropertyRelative("baseVolume");
            SerializedProperty pitchMin = cue.FindPropertyRelative("pitchMin");
            SerializedProperty pitchMax = cue.FindPropertyRelative("pitchMax");
            SerializedProperty cooldownSeconds = cue.FindPropertyRelative("cooldownSeconds");
            SerializedProperty loop = cue.FindPropertyRelative("loop");

            bool hasEmptyId = string.IsNullOrWhiteSpace(id.stringValue);
            bool hasDuplicateId = !hasEmptyId && _idCounts.TryGetValue(id.stringValue, out int count) && count > 1;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Cue {index + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Duplicate", GUILayout.Width(76f)))
                    {
                        DuplicateCue(index);
                        return;
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(68f)))
                    {
                        RemoveCue(index);
                        return;
                    }
                }

                EditorGUILayout.PropertyField(id, new GUIContent("Cue ID"));
                if (hasEmptyId)
                {
                    EditorGUILayout.HelpBox("Cue ID is empty. Runtime calls with an empty ID will be ignored.", MessageType.Warning);
                }
                else if (hasDuplicateId)
                {
                    EditorGUILayout.HelpBox("Duplicate cue ID. Runtime lookup keeps the last cue with this ID, so earlier entries will be shadowed.", MessageType.Warning);
                }

                EditorGUILayout.PropertyField(bus);
                EditorGUILayout.PropertyField(clips, includeChildren: true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(baseVolume);
                    EditorGUILayout.PropertyField(cooldownSeconds, new GUIContent("Cooldown"));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(pitchMin);
                    EditorGUILayout.PropertyField(pitchMax);
                }

                EditorGUILayout.PropertyField(loop);
            }
        }

        private void AddCue(string cueId, AudioBus bus)
        {
            int index = _cues.arraySize;
            _cues.InsertArrayElementAtIndex(index);

            SerializedProperty cue = _cues.GetArrayElementAtIndex(index);
            cue.FindPropertyRelative("id").stringValue = cueId;
            cue.FindPropertyRelative("bus").enumValueIndex = (int)bus;
            cue.FindPropertyRelative("clips").ClearArray();
            cue.FindPropertyRelative("baseVolume").floatValue = 1f;
            cue.FindPropertyRelative("pitchMin").floatValue = 1f;
            cue.FindPropertyRelative("pitchMax").floatValue = 1f;
            cue.FindPropertyRelative("cooldownSeconds").floatValue = 0f;
            cue.FindPropertyRelative("loop").boolValue = bus == AudioBus.Music;
        }

        private void AddSelectedAudioClips()
        {
            AudioClip[] selectedClips = Selection.objects.OfType<AudioClip>().ToArray();
            if (selectedClips.Length == 0)
            {
                EditorUtility.DisplayDialog("Audio Catalog", "Select one or more AudioClip assets in the Project window first.", "OK");
                return;
            }

            foreach (AudioClip clip in selectedClips)
            {
                string cueId = clip.name;
                AudioBus bus = cueId.StartsWith("bgm.") ? AudioBus.Music : cueId.StartsWith("ui.") ? AudioBus.Ui : AudioBus.Sfx;
                AddCue(cueId, bus);

                SerializedProperty cue = _cues.GetArrayElementAtIndex(_cues.arraySize - 1);
                SerializedProperty clips = cue.FindPropertyRelative("clips");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            }
        }

        private void DuplicateCue(int index)
        {
            _cues.InsertArrayElementAtIndex(index);

            SerializedProperty duplicatedCue = _cues.GetArrayElementAtIndex(index + 1);
            SerializedProperty duplicatedId = duplicatedCue.FindPropertyRelative("id");
            duplicatedId.stringValue = $"{duplicatedId.stringValue}.copy";
        }

        private void RemoveCue(int index)
        {
            _cues.DeleteArrayElementAtIndex(index);
        }

        private void SortById()
        {
            var catalog = (AudioCatalog)target;
            Undo.RecordObject(catalog, "Sort Audio Catalog");
            catalog.Cues.Sort((left, right) => string.CompareOrdinal(left?.id, right?.id));
            EditorUtility.SetDirty(catalog);
            serializedObject.Update();
        }

        private void RebuildIdCounts()
        {
            _idCounts.Clear();
            for (int i = 0; i < _cues.arraySize; i++)
            {
                string id = _cues.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                _idCounts.TryGetValue(id, out int count);
                _idCounts[id] = count + 1;
            }
        }
    }
}
