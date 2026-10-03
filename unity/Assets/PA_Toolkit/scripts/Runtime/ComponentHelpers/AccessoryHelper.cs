using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace PA_Toolkit
{
    public class AccessoryHelper : BodyPartHelper
    {
        [SerializeField] float durationScaleInColliders = .25f;
        [SerializeField] Accessory accessory;
        public Accessory Accessory => accessory;
        [SerializeField] ParentConstraint parentConstraint;
        [SerializeField] ScaleConstraint scaleConstraint;

        Coroutine routineScale;
        CapsuleCollider[] _colliders;
        public CapsuleCollider[] colliders
        {
            get
            {
                if (_colliders == null) _colliders =
                        gameObject.GetComponentsInChildren<CapsuleCollider>();
                return _colliders;
            }
        }
        protected override void Subscribe()
        {
            base.Subscribe();
            agent.onBodyTypeCollidersChanged += CollidersChanged;
            agent.onHairChanged += ScaleIn;
        }

        protected override void Unsubscribe()
        {
            base.Unsubscribe();
            agent.onBodyTypeCollidersChanged -= CollidersChanged;
            agent.onHairChanged -= ScaleIn;
        }


        protected override void OnEnable()
        {
            skin.CopyBonesOf(parentSkin);
            CopyParentBlendShapes();

            if (transform.childCount == 0) return;
            scaleConstraint.enabled = false;
            ScaleIn();
        }

        public void AssignHeadJoint(Transform headJoint)
        {
            var source = new ConstraintSource();
            source.sourceTransform = headJoint;
            source.weight = 1;
            if (parentConstraint != null) parentConstraint.SetSource(0, source);
            if (scaleConstraint != null) scaleConstraint.SetSource(0, source);
        }

        void ScaleIn()
        {
            if (!gameObject.activeInHierarchy) return;
            if (routineScale != null) StopCoroutine(routineScale);
            StartCoroutine(ScaleInColliders());
        }
        IEnumerator ScaleInColliders()
        {
            if (transform.childCount == 0) yield break;
            Transform cols = transform.GetChild(0);
            float normalizedDuration = 0;
            float step;
            while (normalizedDuration < 1)
            {
                if (normalizedDuration < .5) step = Mathf.Lerp(0, 1, normalizedDuration);
                else step = Mathf.SmoothStep(0, 1, normalizedDuration);
                cols.localScale = Vector3.one * step;
                normalizedDuration += Time.deltaTime / durationScaleInColliders;
                yield return null;
            }
            cols.localScale = Vector3.one;
        }

        protected override void OnBodyTypeChanged(int bodyType)
        {
            if (cloth != null && !isMarkedForDestruction)
            {
                Instantiate(gameObject, transform.parent);
                DestroySelf();
            }
            else CopyParentBlendShapes();
        }

        public void CopyParentBlendShapes()
        {
            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                skin.SetBlendShapeWeight(i, parentSkin.GetBlendShapeWeight(i));
        }

        protected override void OnClothCollidersUpdated(List<CapsuleCollider> colliders)
        {
            colliders = new List<CapsuleCollider>(colliders);
            //Remove self-intersection
            foreach (var collider in this.colliders)
            {
                colliders.Remove(collider);
            }

            base.OnClothCollidersUpdated(colliders);
        }
        void CollidersChanged(BodyTypeColliderData data)
        {
            List<CapsuleCollider> colliders = new List<CapsuleCollider>(this.colliders);
            List<string> paths = new List<string>();
            foreach (var col in colliders)
                paths.Add(col.transform.GetPath(transform, false));

            foreach (var collider in data.colliderData)
            {
                int index = paths.IndexOf(collider.id);
                if (index < 0) continue;
                paths.RemoveAt(index);
                if (gameObject.activeInHierarchy)
                    StartCoroutine(LerpCollider(colliders[index], collider));
                else
                    colliders[index].SetData(collider);
                colliders.RemoveAt(index);
            }
            //ScaleIn();
        }

        IEnumerator LerpCollider(CapsuleCollider collider, ColliderData data)
        {
            if (routineScale != null) StopCoroutine(routineScale);
            transform.GetChild(0).localScale = Vector3.one;
            float normalizedDuration = 0;
            ColliderData start = collider.GetData(null);
            if (durationScaleInColliders > 0)
            {
                while (normalizedDuration < 1)
                {
                    normalizedDuration += Time.deltaTime / durationScaleInColliders;
                    collider.SetData(ColliderData.Lerp(start, data,
                        Mathf.SmoothStep(0, 1, normalizedDuration)));
                    yield return null;
                }
            }
            collider.SetData(data);
        }
    }
}
