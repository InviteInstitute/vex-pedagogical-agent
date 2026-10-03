using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    public class CreationSetting : ScriptableObject
    {
        [Header("Name")]
        [SerializeField] string _id;
        public string id => _id;

        [SerializeField] string _longName;
        public string longName => _longName;

        [SerializeField] Thumbnail _thumbnail;
        public Thumbnail thumbnail => _thumbnail;
    }
}