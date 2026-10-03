using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
namespace PA_Toolkit
{
    [System.Serializable]
    public class AgentSettings
    {
        public string name;
        [TextArea(5, 5)]
        public string notes = string.Empty;

        public Preset preset;
        public bool useWheelchair;
        public Face face;
        public SkinColor skinColor;
        public Hair hair;
        public HairColor hairColor;
        public EyeShape eyeShape;
        public Eyes eyes;
        public Outfit outfit;
        public BodyType bodyType;
        public Shoes shoes;
        public List<Accessory> accessories = new List<Accessory>();
        public List<ActionSettings> actions = new List<ActionSettings>();
        public Texture2D decal;

        public bool presetMatchesSettings
        {
            get
            {
                //if we implement multiple accessories, they could be listed in any order, so we need to check if each list contains the same elements regardless of order
                bool match = preset != null
                    && preset.settings.useWheelchair == useWheelchair
                    && preset.settings.face == face
                    && preset.settings.skinColor == skinColor
                    && preset.settings.hair == hair
                    && preset.settings.hairColor == hairColor
                    && preset.settings.eyeShape == eyeShape
                    && preset.settings.eyes == eyes
                    && preset.settings.outfit == outfit
                    && preset.settings.bodyType == bodyType
                    && preset.settings.shoes == shoes
                    && preset.settings.accessories.Count() == accessories.Count()
                    && preset.settings.accessories.Except(accessories).Count() == 0;

             return match;
            }
        }

        public AgentSettings(AgentSettings settings)
        {
            preset = settings.preset;
            useWheelchair = settings.useWheelchair;
            name = settings.name;
            notes = settings.notes;
            face = settings.face;
            skinColor = settings.skinColor;
            hair = settings.hair;
            hairColor = settings.hairColor;
            eyeShape = settings.eyeShape;
            eyes = settings.eyes;
            outfit = settings.outfit;
            bodyType = settings.bodyType;
            shoes = settings.shoes;
            accessories = new List<Accessory>(settings.accessories);
            actions = settings.actions;
            decal = settings.decal;
        }

        public bool HasSetting(CreationSetting setting)
        {
            if (setting != null
                && setting == preset
                || setting == face
                || setting == skinColor
                || setting == hair
                || setting == hairColor
                || setting == eyeShape
                || setting == eyes
                || setting == outfit
                || setting == bodyType
                || setting == shoes
                || accessories.Contains(setting as Accessory)
                )
                return true;

            return false;
        }

        public SerializeableData GetData(List<CreationSettingsList> settingAssets)
        {
            SerializeableData data = new SerializeableData(this, settingAssets);
            return data;
        }

        public string ToJson(List<CreationSettingsList> settingAssets)
        {
            return JsonUtility.ToJson(GetData(settingAssets), true);
        }

        [System.Serializable]
        public class SerializeableData
        {
            public string agentId;

            public string presetName;
            public bool useWheelchair;
            public string faceName;
            public string skinColorName;
            public string hairName;
            public string hairColorName;
            public string eyeShapeName;
            public string eyesName;
            public string outfitName;
            public string bodyTypeName;
            public string shoesName;
            public List<string> accessoriesNames = new List<string>();
            public List<string> actionNames = new List<string>();
            public SerializableTexture decal = new SerializableTexture();

            public string agentName;
            public string notes;

            public SerializeableData(AgentSettings settings, List<CreationSettingsList> settingLists)
            {
                useWheelchair = settings.useWheelchair;
                agentName = settings.name;
                notes = settings.notes;
                decal.tex = settings.decal;

                agentId = string.Concat(agentName.Split(Path.GetInvalidFileNameChars())).Replace(' ','-') + 
                    "_" + System.DateTime.Now.ToString().Replace('/', '-').Replace(':', '-').Replace(' ', '_');

                foreach (var list in settingLists)
                {
                    foreach (var item in list.items)
                    {
                        string assetName = item.setting.name;

                        if (settings.preset != null && settings.preset.name.Contains(assetName)) presetName = assetName;
                        else if (settings.face != null && settings.face.name.Contains(assetName)) faceName = assetName;
                        else if (settings.skinColor != null && settings.skinColor.name.Contains(assetName)) skinColorName = assetName;
                        else if (settings.hair != null && settings.hair.name.Contains(assetName)) hairName = assetName;
                        else if (settings.hairColor != null && settings.hairColor.name.Contains(assetName)) hairColorName = assetName;
                        else if (settings.eyeShape != null && settings.eyeShape.name.Contains(assetName)) eyeShapeName = assetName;
                        else if (settings.eyes != null && settings.eyes.name.Contains(assetName)) eyesName = assetName;
                        else if (settings.outfit != null && settings.outfit.name.Contains(assetName)) outfitName = assetName;
                        else if (settings.bodyType != null && settings.bodyType.name.Contains(assetName)) bodyTypeName = assetName;
                        else if (settings.shoes != null && settings.shoes.name.Contains(assetName)) shoesName = assetName;
                        else
                        {
                            foreach (var accesory in settings.accessories)
                            {
                                if (accesory.name == assetName) accessoriesNames.Add(assetName);
                            }
                            foreach (var act in settings.actions)
                            {
                                if (act.name == assetName) actionNames.Add(assetName);
                            }
                        }
                    }
                }
            }

            public SerializeableData(SerializeableData data)
            {
                agentId = data.agentId;
                presetName = data.presetName;
                useWheelchair = data.useWheelchair;
                faceName = data.faceName;
                skinColorName    = data.skinColorName;
                hairName = data.hairName;
                hairColorName = data.hairColorName;
                eyeShapeName = data.eyeShapeName;
                eyesName = data.eyesName;
                outfitName  = data.outfitName;    
                bodyTypeName = data.bodyTypeName; 
                shoesName = data.shoesName;
                accessoriesNames    = data.accessoriesNames;
                actionNames = data.actionNames;
                agentName = data.agentName;
                notes = data.notes;
                decal = data.decal;
            }
        }
    }
}