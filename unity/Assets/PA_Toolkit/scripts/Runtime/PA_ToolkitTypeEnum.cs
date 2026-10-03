using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    public class PA_ToolkitTypeEnum : MonoBehaviour
    {
        public enum SettingType
        {
            Accessory,
            BodyType,
            CreationSetting,
            CreationSettingList,
            Expression,
            EyeShape,
            Eyes,
            Face,
            Gesture,
            Hair,
            HairColor,
            Outfit,
            Preset,
            Shoes,
            SkinColor,
            Thumbnail
        }

        public static Type SettingTypeToType(SettingType type)
        {
            switch (type)
            {
                case SettingType.Accessory: return typeof(Accessory);
                case SettingType.BodyType: return typeof(BodyType);
                case SettingType.CreationSetting: return typeof(CreationSetting);
                case SettingType.CreationSettingList: return typeof(CreationSettingsList);
                case SettingType.Expression: return typeof(Expression);
                case SettingType.EyeShape: return typeof(EyeShape);
                case SettingType.Eyes: return typeof(Eyes);
                case SettingType.Face: return typeof(Face);
                case SettingType.Gesture: return typeof(Gesture);
                case SettingType.Hair: return typeof(Hair);
                case SettingType.HairColor: return typeof(HairColor);
                case SettingType.Outfit: return typeof(Outfit);
                case SettingType.Preset: return typeof(Preset);
                case SettingType.Shoes: return typeof(Shoes);
                case SettingType.SkinColor: return typeof(SkinColor);
                case SettingType.Thumbnail: return typeof(Thumbnail);
                default: return null;
            }
        }
    }

    public enum PAJoint
    {
        Head,
        Neck,
        LeftShoulder,
        RightShoulder,
        Spine3
    }
}
