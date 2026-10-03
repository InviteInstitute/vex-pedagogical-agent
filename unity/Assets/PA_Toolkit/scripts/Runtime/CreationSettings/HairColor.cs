using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "hairColor_name", menuName = "Data/Hair Color")]
    public class HairColor : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Color _tint = Color.white;
        public Color tint => _tint;
    }
}