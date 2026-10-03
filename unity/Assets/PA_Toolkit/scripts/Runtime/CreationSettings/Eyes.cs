using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{

    [CreateAssetMenu(fileName = "eyes_name", menuName = "Data/Eyes")]
    public class Eyes : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Texture _texture;
        public Texture texture => _texture;

        public Material material;
    }
}