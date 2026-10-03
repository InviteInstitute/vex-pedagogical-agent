using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "expression_name", menuName = "Data/Expression")]
    public class Expression : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] string _stateName;
        public string stateName => _stateName;
    }
}