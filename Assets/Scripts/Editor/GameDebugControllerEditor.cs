using BaoZuPo.Core;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    [CustomEditor(typeof(GameDebugController))]
    public class GameDebugControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            GUILayout.Label("── 随机事件调试 ──", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("触发随机事件", GUILayout.Height(30)))
            {
                ((GameDebugController)target).TriggerDebugRandomEvent();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("需要在 Play Mode 下触发", MessageType.Info);

            GUI.enabled = true;
        }
    }
}
