using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "face_name", menuName = "Data/Face")]
    public class Face : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField] List<Material> _materials = new List<Material>();
        public List<Material> materials => _materials;
    }
}