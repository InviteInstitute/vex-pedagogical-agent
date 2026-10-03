using SerializableClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "bodyType_name", menuName = "Data/Body Type")]
    public class BodyType : CreationSetting
    {
        [Header("Settings")]
        [SerializeField] int _index;
        public int index => _index;
        public Vector3 leftEyePosition;
        public AnimatorOverrideController standingAnimator;
        public AnimatorOverrideController wheelchairAnimator;

        //Deprecated, hiding for now as to not loose work if the new system doesn't work
        [SerializeField, HideInInspector] List<BodyTypeColliderData> _colliderDataByBodytype;
        public List<BodyTypeColliderData> colliderDataByBodytype => _colliderDataByBodytype;

        [Tooltip("Locations are relative to Streaming Assets")]
        [SerializeField] SerializeableDictionary_StringKeys<string> _colliderDataLocations = new SerializeableDictionary_StringKeys<string>();
        public SerializeableDictionary_StringKeys<string> colliderDataLocations => _colliderDataLocations;

        [HideInInspector, SerializeField]
        public SerializeableDictionary_StringKeys<BodyTypeColliderData> colliderDataCache = new SerializeableDictionary_StringKeys<BodyTypeColliderData>();
    }
}