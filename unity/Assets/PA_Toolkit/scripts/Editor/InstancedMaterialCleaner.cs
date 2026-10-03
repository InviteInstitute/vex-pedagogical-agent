#if UNITY_EDITOR
using AssetUsageDetectorNamespace;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace PA_Toolkit
{
    public class InstancedMaterialCleaner : MonoBehaviour
    {
        static int scenesLoadedAtStart;

        [MenuItem("PA_Toolkit/Clear Unused Instanced Materials")]
        public static void ClearUnusedMaterials()
        {
            scenesLoadedAtStart = SceneManager.loadedSceneCount;

            for (int i = 0; i < scenesLoadedAtStart; ++i)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogError("Cannot clean materials while there are unsaved scenes!");
                    return;
                }
            }
            var instancedMaterialsFolder = AssetDatabase.LoadAssetAtPath(PA_ToolkitData.instancedMaterialsPath, typeof(DefaultAsset));


            AssetUsageDetector.Parameters parameters = new AssetUsageDetector.Parameters();
            parameters.calculateUnusedObjects = true;
            parameters.objectsToSearch = new Object[] { instancedMaterialsFolder };
            parameters.searchInScenes = SceneSearchMode.AllScenes;
            parameters.lazySceneSearch = false;
            parameters.showDetailedProgressBar = false;

            SearchResult result = new AssetUsageDetector().Run(parameters);

            if (result == null || !result.SearchCompletedSuccessfully)
            {
                Debug.LogError("There was a problem while searching, please try again");
                return;
            }

            List<string> usedDirectories = new List<string>();
            foreach (Object o in result.UsedObjects)
            {
                string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(o)).Replace("\\", "/");
                if (!usedDirectories.Contains(dir))
                {
                    usedDirectories.Add(dir);
                }
            }

            List<string> instancedMaterialsSubdirectories = new List<string>(Directory.GetDirectories(AssetDatabase.GetAssetPath(instancedMaterialsFolder)));
            List<string> assetsToDelete = new List<string>();
            string debugMessage = "";
            for (int i = 0; i < instancedMaterialsSubdirectories.Count; ++i)
            {
                instancedMaterialsSubdirectories[i] = instancedMaterialsSubdirectories[i].Replace("\\", "/");
                if (!usedDirectories.Contains(instancedMaterialsSubdirectories[i]))
                {
                    debugMessage += Path.GetFileName(instancedMaterialsSubdirectories[i]) + "\n";
                    assetsToDelete.Add(instancedMaterialsSubdirectories[i]);
                }
            }



            bool shouldDelete = false;
            if (assetsToDelete.Count == 0)
            {
                Debug.Log("There were no assets to delete");
            }
            else
            {
                shouldDelete = EditorUtility.DisplayDialog(
                    "Material Cleaner",
                    "The following instancedMaterial directories were detected as unused, would you like to delete them?\n\n" + debugMessage,
                    "Delete",
                    "Cancel");
            }
            if (shouldDelete)
            {
                foreach (string assetPath in assetsToDelete)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.Refresh();
            }

            //Any scenes with used assets in them will have become loaded by the search function
            for (int i = scenesLoadedAtStart; i < SceneManager.loadedSceneCount; ++i)
            {
                EditorSceneManager.CloseScene(EditorSceneManager.GetSceneAt(i), true);
            }
        }
        [MenuItem("PA_Toolkit/Clear Unused Instanced Materials", true)]
        public static bool ClearUnusedMaterialsValidator()
        {
            return !Application.isPlaying;
        }

    }
}
#endif