using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "skinColor_name", menuName = "Data/Skin Color")]
    public class SkinColor : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Color _tint = Color.white; //new shader doesn't utilize hdr
                                                    //[ColorUsage(true, true), SerializeField] Color _tint = Color.white;

        public Color tint => _tint;
    }
}