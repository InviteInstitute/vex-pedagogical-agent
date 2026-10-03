using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "gesture_name", menuName = "Data/Gesture")]
    public class Gesture : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] string _stateName;
        public string stateName => _stateName;
    }
}