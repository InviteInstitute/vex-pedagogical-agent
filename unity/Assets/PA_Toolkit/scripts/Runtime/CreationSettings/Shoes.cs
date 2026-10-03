using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "shoes_name", menuName = "Data/Shoes")]
    public class Shoes : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField] List<Material> _materials = new List<Material>();
        public List<Material> materials => _materials;

        [SerializeField] ShoeHelper _shoeHelper;
        public ShoeHelper shoeHelper => _shoeHelper;
    }
}