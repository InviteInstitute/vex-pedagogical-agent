using SerializableClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "outfit_name", menuName = "Data/Outfit")]
    public class Outfit : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField] List<Material> _materials = new List<Material>();
        public List<Material> materials => _materials;

        [FormerlySerializedAs("helper")]
        [SerializeField] OutfitHelper _helper;
        public OutfitHelper helper => _helper;

        //Deprecated, hiding for now as to not loose work if the new system doesn't work
        [SerializeField, HideInInspector] List<BodyTypeColliderData> _colliderDataByBodytype;
        public List<BodyTypeColliderData> colliderDataByBodytype => _colliderDataByBodytype;
    }
}