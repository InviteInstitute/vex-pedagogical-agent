using SerializableClasses;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace PA_Toolkit
{
    public class OutfitHelper : BodyPartHelper
    {
        [SerializeField] SerializeableDictionary_EnumKeys<PAJoint, ParentConstraint> constraints;
        [SerializeField] float durationSlideColliders = .15f;
        CapsuleCollider[] _colliders;
        public CapsuleCollider[] colliders
        {
            get
            {
                if (_colliders == null) _colliders = GetComponentsInChildren<CapsuleCollider>();
                return _colliders;
            }
        }
        bool hasInit = false;
        protected override void Subscribe()
        {
            base.Subscribe();
            agent.onSkinColorChanged += OnSkinColorChanged;
            agent.onDecalChanged += OnDecalChanged;
            agent.onBodyTypeCollidersChanged += CollidersChanged;
        }


        protected override void Unsubscribe()
        {
            base.Unsubscribe();
            agent.onSkinColorChanged -= OnSkinColorChanged;
            agent.onDecalChanged -= OnDecalChanged;
            agent.onBodyTypeCollidersChanged -= CollidersChanged;
        }

        protected override void AssignAgent(Agent agent)
        {
            base.AssignAgent(agent);
            foreach (var kvp in constraints)
            {
                if (agent.components.jointDictionary.TryGetValue(kvp.Key, out Transform joint))
                {
                    var constraint = new ConstraintSource();
                    constraint.sourceTransform = joint;
                    constraint.weight = 1;
                    kvp.Value.AddSource(constraint);
                }
            }
            OnDecalChanged(agent.activeDecal);
        }

        void OnSkinColorChanged(Color color)
        {
            if (Application.isPlaying && skin.materials[0] != null)
                skin.materials[0].SetColor(Agent.SKIN_COLOR_NAME, color);
            else if (!Application.isPlaying && skin.sharedMaterials[0] != null)
                skin.sharedMaterial.SetColor(Agent.SKIN_COLOR_NAME, color);
        }

        void OnDecalChanged(Texture2D texture)
        {
            if (skin.sharedMaterials.Length > 1 && skin.sharedMaterials[1] != null &&
                skin.sharedMaterials[1].HasTexture(Agent.DECAL))
                skin.sharedMaterials[1].SetTexture(Agent.DECAL, texture);
        }

        void CollidersChanged(BodyTypeColliderData data)
        {
            List<CapsuleCollider> colliders = new List<CapsuleCollider>(this.colliders);
            List<string> paths = new List<string>();
            foreach(var col in colliders)
                paths.Add(col.transform.GetPath(transform, false));

            foreach(var collider in data.colliderData)
            {
                int index = paths.IndexOf(collider.id);
                if (index < 0) continue;
                paths.RemoveAt(index); 
                if (gameObject.activeInHierarchy && hasInit)
                    StartCoroutine(LerpCollider(colliders[index], collider));
                else 
                    colliders[index].SetData(collider);
                colliders.RemoveAt(index);
            }
            if (!hasInit) hasInit = true;
        }

        IEnumerator LerpCollider(CapsuleCollider collider, ColliderData data)
        {
            float normalizedDuration = 0;
            ColliderData start = collider.GetData(null);
            while (normalizedDuration < 1)
            {
                normalizedDuration += Time.deltaTime / durationSlideColliders;
                collider.SetData(ColliderData.Lerp(start, data,
                    Mathf.SmoothStep(0, 1, normalizedDuration)));
                yield return null;
            }
        }
    }
}
