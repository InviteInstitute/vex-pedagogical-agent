using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace PA_Toolkit
{
    [CustomEditor(typeof(Agent)), CanEditMultipleObjects]
    public class AgentEditor : Editor
    {
        List<Agent> agents = new List<Agent>();


        protected void OnEnable()
        {
            agents.Clear();
            foreach (GameObject go in Selection.gameObjects)
            {
                agents.Add(go.GetComponent<Agent>());
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledGroupScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Load Agent from file"))
                {
                    AgentFileBuilder.LoadAgentsFromFile(agents.ToArray());
                }
            }
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        public override VisualElement CreateInspectorGUI()
        {
            PackageDependency.CheckForPackage("com.unity.animation.rigging", "");
            PackageDependency.CheckForPackage("com.hecomi.ulipsync", "https://github.com/hecomi/uLipSync.git#upm");
            return base.CreateInspectorGUI();
        }
    }
}
#endif