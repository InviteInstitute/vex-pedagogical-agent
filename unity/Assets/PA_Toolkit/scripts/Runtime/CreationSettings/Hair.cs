using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "hair_name", menuName = "Data/Hair")]
    public class Hair : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField] List<Material> _materials = new List<Material>();
        public List<Material> materials => _materials;

        [Header("Required Hair Prefab")]
        [SerializeField] HairHelper _hairPrefab;
        public HairHelper hairPrefab => _hairPrefab;

        [Header("Optional Parented Object")]
        [FormerlySerializedAs("_prefab")]
        [SerializeField] GameObject _childPrefab;
        public GameObject childPrefab => _childPrefab;
    }
}