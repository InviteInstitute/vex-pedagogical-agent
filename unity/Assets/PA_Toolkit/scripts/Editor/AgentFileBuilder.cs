
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PA_Toolkit
{
    public class AgentFileBuilder : EditorWindow
    {
        [MenuItem("PA_Toolkit/Load agent(s) from file (.agent)")]
        public static void LoadAgentFromFile()
        {
            if(Selection.gameObjects.Length > 1)
            {
                List<Agent> agents = new List<Agent>();
                foreach(GameObject go in Selection.gameObjects)
                {
                    agents.Add(go.GetComponent<Agent>());
                }
                LoadAgentsFromFile(agents.ToArray());
                return;
            }
            
            string path = EditorUtility.OpenFilePanel("Select .agent file to load", Application.dataPath, "agent");
            LoadAgentFromFile(Selection.activeGameObject.GetComponent<Agent>(), path);
        }

        public static void LoadAgentFromFile(Agent agent, string path)
        {
            if (path == null || path.Length == 0)
            {
                return;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(agent.gameObject);
            Undo.RecordObject(agent.gameObject, agent.gameObject.name);
            AgentDataLoader loader = agent.gameObject.AddComponent<AgentDataLoader>();
            loader.StartCoroutine(Load(loader, path));
        }

        public static void LoadAgentsFromFile(Agent[] agents)
        {
            string path = EditorUtility.OpenFilePanel("Select .agent file to load", Application.dataPath, "agent");
            foreach(Agent agent in agents)
            {
                LoadAgentFromFile(agent, path);
            }
        }

        [ExecuteInEditMode]
        static IEnumerator Load(AgentDataLoader loader, string path)
        {
            if (path == null || path.Equals("")) //Reset if the user cancels
            {
                DestroyImmediate(loader);
                yield break;
            }

            AgentSettings.SerializeableData data;

            try
            {
                data = JsonUtility.FromJson<AgentSettings.SerializeableData>(File.ReadAllText(path));
            }
            catch //Reset if the file is bad
            {
                DestroyImmediate(loader);
                yield break;
            }
            if (data == null) //Reset if no data
            {
                DestroyImmediate(loader);
                yield break;
            }

            loader.OnLoadSettingsOverride(data, loader.GetComponent<Agent>());

            EditorUtility.SetDirty(loader.gameObject);
            DestroyImmediate(loader);

            EditorUtility.ClearProgressBar();

        }
        [MenuItem("PA_Toolkit/Load agent(s) from file (.agent)", true)]
        public static bool AgentFunctionValidator()
        {
            if (Selection.activeGameObject == null || Application.isPlaying) return false;
            if(!Selection.activeGameObject.GetComponent<Agent>()) return false;
            foreach(GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<Agent>() == null) return false;
            }
            return true;
        }

        [MenuItem("GameObject/3D Object/Agent")]
        public static void SpawnAgentPrefab()
        {
            Object agent = AssetDatabase.LoadAssetAtPath($"{PA_ToolkitData.path}/prefabs/_prefab_agent.prefab", typeof(GameObject));
            Object inst = PrefabUtility.InstantiatePrefab(agent);
            Undo.RegisterCreatedObjectUndo(inst, $"Create: {inst.name}");
            Selection.activeObject = inst;
        }

    }

}
#endif