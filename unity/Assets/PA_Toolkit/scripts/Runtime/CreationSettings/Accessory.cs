using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "accessory_name", menuName = "Data/Accessory")]
    public class Accessory : CreationSetting
    {
        [System.Flags]
        public enum AccessorySlots
        {
            headTop = 1,
            eyes = 2,
            earLeft = 4,
            earRight = 8,
            mouth = 16,
            neck = 32,
            forearmLeft = 64,
            forearmRight = 128
        }


        [Header("Settings")]
        [SerializeField] AccessorySlots _slots;
        public AccessorySlots slots => _slots;

        [SerializeField] bool _disableHair;
        public bool disableHair => _disableHair;

        [SerializeField] AccessoryHelper _prefab;
        public AccessoryHelper prefab => _prefab;

        [SerializeField] Mesh _mesh;
        public Mesh mesh => _mesh;

        [SerializeField] List<Material> _materials = new List<Material>();
        public List<Material> materials => _materials;

        public bool overridesColliderData = false;

        //Deprecated, hiding for now as to not loose work if the new system doesn't work
        [SerializeField, HideInInspector] List<BodyTypeColliderData> _colliderDataByBodytype;
        public List<BodyTypeColliderData> colliderDataByBodytype => _colliderDataByBodytype;

    }
}