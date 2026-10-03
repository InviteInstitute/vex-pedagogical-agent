#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
namespace PA_Toolkit
{
    public static class PA_ToolkitData
    {
#if UNITY_EDITOR
        static string _path = null;
        public static string path
        {
            get
            {
                if (_path == null || _path.Equals(""))
                {
                    string temp = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("PA_Toolkit t:AssemblyDefinitionAsset")[0]);
                    temp = Path.GetDirectoryName(temp);
                    _path = temp;
                }
                return _path;
            }
        }

        public static string instancedMaterialsPath { get { return $"{path}/instancedMaterials"; } }
        public static string scriptableObjectsPath { get { return $"{path}/scriptableObjects";} }
#endif
    }
}
