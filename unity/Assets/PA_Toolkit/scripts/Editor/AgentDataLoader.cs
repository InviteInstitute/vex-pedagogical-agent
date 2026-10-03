#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PA_Toolkit
{
    public class AgentDataLoader : MonoBehaviour
    {
        public delegate void AgentDataChange<T>(T setting);
        public event AgentDataChange<CreationSetting> onSettingSelected;
        public event AgentDataChange<List<CreationSetting>> onSettingsSelected;
        public event AgentDataChange<bool> onWheelchairSelected;
        public event AgentDataChange<Texture2D> onDecalSelected;
        public event AgentDataChange<string> onNameSelected;
        public event AgentDataChange<string> onNotesSelected;
        public event AgentDataChange<string> onIdSelected;

        public delegate void Event();
        public event Event onLoadFinish;
        public void OnLoadSettings(AgentSettings.SerializeableData data)
        {
            TryLoad(data);
        }

        public void OnLoadSettingsOverride(AgentSettings.SerializeableData data, Agent targetAgent)
        {

            onSettingSelected += targetAgent.OnChangeSetting;
            onSettingsSelected += targetAgent.OnChangeSettings;
            onWheelchairSelected += targetAgent.OnChangeWheelchair;
            onDecalSelected += targetAgent.OnChangeDecal;
            onNameSelected += (newName) => { targetAgent.agentName = newName; };
            onNotesSelected += (newNotes) => { targetAgent.agentNotes = newNotes; };
            onIdSelected += targetAgent.SetAgentId;

            TryLoad(data);

            EditorUtility.SetDirty(targetAgent);

            onSettingSelected -= targetAgent.OnChangeSetting;
            onSettingsSelected -= targetAgent.OnChangeSettings;
            onWheelchairSelected -= targetAgent.OnChangeWheelchair;
            onDecalSelected -= targetAgent.OnChangeDecal;
            onNameSelected -= (newName) => { targetAgent.agentName = newName; };
            onNotesSelected -= (newNotes) => { targetAgent.agentNotes = newNotes; };
            onIdSelected -= targetAgent.SetAgentId;
        }

        void TryLoad(AgentSettings.SerializeableData data)
        {
            if (Application.isPlaying) return;

            Face        faceAsset       = AssetDatabase.LoadAssetAtPath<Face>       (ScriptableObjectPath(data.faceName));
            Hair        hairAsset       = AssetDatabase.LoadAssetAtPath<Hair>       (ScriptableObjectPath(data.hairName));
            HairColor   hairColorAsset  = AssetDatabase.LoadAssetAtPath<HairColor>  (ScriptableObjectPath(data.hairColorName));
            EyeShape    eyeShapeAsset   = AssetDatabase.LoadAssetAtPath<EyeShape>   (ScriptableObjectPath(data.eyeShapeName));
            Eyes        eyesAsset       = AssetDatabase.LoadAssetAtPath<Eyes>       (ScriptableObjectPath(data.eyesName));
            Outfit      outfitAsset     = AssetDatabase.LoadAssetAtPath<Outfit>     (ScriptableObjectPath(data.outfitName));
            BodyType    bodyTypeAsset   = AssetDatabase.LoadAssetAtPath<BodyType>   (ScriptableObjectPath(data.bodyTypeName));
            Shoes       shoesAsset      = AssetDatabase.LoadAssetAtPath<Shoes>      (ScriptableObjectPath(data.shoesName));
            SkinColor   skinColorAsset  = AssetDatabase.LoadAssetAtPath<SkinColor>  (ScriptableObjectPath(data.skinColorName));
            
            List<Accessory> accessoryAssets = new List<Accessory>();
            foreach(string accessoryName in data.accessoriesNames)
            {
                accessoryAssets.Add(AssetDatabase.LoadAssetAtPath<Accessory>(ScriptableObjectPath(accessoryName)));
            }

            Face faceCopy = Instantiate(faceAsset);
            Outfit outfitCopy = Instantiate(outfitAsset);
            Shoes shoesCopy = Instantiate(shoesAsset);
            Hair hairCopy = Instantiate(hairAsset);
            Eyes eyesCopy = Instantiate(eyesAsset);

            string agentDataPath = PA_ToolkitData.instancedMaterialsPath + "/" + data.agentId;

            if(Directory.Exists(agentDataPath))
            {
                faceCopy.materials[0] = AssetDatabase.LoadAssetAtPath<Material>($"{agentDataPath}/{faceAsset.materials[0].name}_face.mat");
                outfitCopy.materials[0] = AssetDatabase.LoadAssetAtPath<Material>($"{agentDataPath}/{outfitAsset.materials[0].name}_outfit.mat");
                shoesCopy.materials[0] = AssetDatabase.LoadAssetAtPath<Material>($"{agentDataPath}/{shoesAsset.materials[0].name}_shoes.mat");
                hairCopy.materials[0] = AssetDatabase.LoadAssetAtPath<Material>($"{agentDataPath}/{hairAsset.materials[0].name}_hair.mat");
                eyesCopy.material = AssetDatabase.LoadAssetAtPath<Material>($"{agentDataPath}/{eyesAsset.material.name}_eyes.mat");
            }
            else
            {
                Directory.CreateDirectory(agentDataPath);

                faceCopy.materials[0] = CopyMaterial(faceAsset.materials[0], "_face", agentDataPath);
                outfitCopy.materials[0] = CopyMaterial(outfitAsset.materials[0], "_outfit", agentDataPath);
                shoesCopy.materials[0] = CopyMaterial(shoesAsset.materials[0], "_shoes", agentDataPath);
                hairCopy.materials[0] = CopyMaterial(hairAsset.materials[0], "_hair", agentDataPath);
                eyesCopy.material = CopyMaterial(eyesAsset.material, "_eyes", agentDataPath);

                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }

            onSettingSelected?.Invoke(faceCopy);
            onSettingSelected?.Invoke(hairCopy);
            onSettingSelected?.Invoke(hairColorAsset);
            onSettingSelected?.Invoke(eyeShapeAsset);
            onSettingSelected?.Invoke(eyesCopy);
            onSettingSelected?.Invoke(outfitCopy);
            onSettingSelected?.Invoke(bodyTypeAsset);
            onSettingSelected?.Invoke(shoesCopy);
            onSettingSelected?.Invoke(skinColorAsset);
            //foreach (Accessory accessoryAsset in accessoryAssets)
            //{
            //    onSettingSelected?.Invoke(accessoryAsset);
            //}
            onSettingsSelected?.Invoke(accessoryAssets.Cast<CreationSetting>().ToList());

            onWheelchairSelected?.Invoke(data.useWheelchair);
            onDecalSelected?.Invoke(SaveTexture(data.decal.tex, agentDataPath));
            onNameSelected?.Invoke(data.agentName);
            onNotesSelected?.Invoke(data.notes);
            onIdSelected?.Invoke(data.agentId);

            onLoadFinish?.Invoke();
        }

        Material CopyMaterial(Material baseMaterial, string suffix, string path)
        {
            Material newMaterial = Instantiate(baseMaterial);
            newMaterial.name = baseMaterial.name + suffix;
            AssetDatabase.CreateAsset(newMaterial, $"{path}/{newMaterial.name}.mat");
            return newMaterial;
        }

        Texture2D SaveTexture(Texture2D texture, string path)
        {
            if (texture == null) return null;
            path = $"{path}/decal.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.Refresh();
            TextureImporter importer = TextureImporter.GetAtPath(path) as TextureImporter;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return savedTex;
        }

        public string ScriptableObjectPath(string filename)
        {
            return $"{PA_ToolkitData.scriptableObjectsPath}/{filename}.asset";
        }
    }
}
#endif