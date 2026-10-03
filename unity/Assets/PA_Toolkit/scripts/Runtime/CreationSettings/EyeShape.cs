using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "eyeshape_name", menuName = "Data/EyeShape")]
    public class EyeShape : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] string _blinkAnimationName = "Blink";
        public string blinkAnimationName => _blinkAnimationName;
        [SerializeField] List<string> _blendShapes = new List<string>() { "d_00", "d_01", "d_02"};
        public List<string> blendShapes => _blendShapes;
        [SerializeField] public List<float> _blendShapeValues = new List<float>() { 0, 0, 0 };
        public List<float> blendShapeValues => _blendShapeValues;
    }
}