using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [ExecuteInEditMode]
    public class BodyPartHelper : MonoBehaviour
    {
        SkinnedMeshRenderer _skin;
        public SkinnedMeshRenderer skin
        {
            get
            {
                if (_skin == null) _skin = GetComponent<SkinnedMeshRenderer>();
                return _skin;
            }
        }

        SkinnedMeshRenderer _parentSkin;
        protected SkinnedMeshRenderer parentSkin
        {
            get
            {
                if (_parentSkin == null) _parentSkin = transform.parent.GetComponent<SkinnedMeshRenderer>();
                return _parentSkin;
            }
        }


        Cloth _cloth;
        public Cloth cloth
        {
            get
            {
                if (_cloth == null) _cloth = GetComponent<Cloth>();
                return _cloth;
            }
        }

        bool _isMarkedForDestruction = false;
        public bool isMarkedForDestruction => _isMarkedForDestruction;


        protected Agent agent;

        protected virtual void Awake()
        {
            AssignAgent(GetComponentInParent<Agent>());
            name = name.Replace("(Clone)", "");
        }

        protected virtual void OnEnable()
        {
            skin.CopyBlendshapesOf(parentSkin);
            skin.CopyBonesOf(parentSkin);
        }
        protected virtual void OnDestroy()
        {
            if (agent != null) Unsubscribe();
        }
        protected virtual void Unsubscribe()
        {
            agent.onBodyTypeChanged -= OnBodyTypeChanged;
            agent.onClothCollidersChanged -= OnClothCollidersUpdated;
        }
        protected virtual void AssignAgent(Agent agent)
        {
            this.agent = agent;
            Subscribe();
        }

        protected virtual void Subscribe()
        {
            Unsubscribe();
            agent.onBodyTypeChanged += OnBodyTypeChanged;
            if (cloth != null)
                agent.onClothCollidersChanged += OnClothCollidersUpdated;
        }

        protected virtual void OnBodyTypeChanged(int bodyType)
        {
            if(cloth != null && !isMarkedForDestruction)
            {
                StartCoroutine(ResetCloth());
            }
            else 
                skin.CopyBlendshapesOf(parentSkin);
        }

        IEnumerator ResetCloth()
        {
            yield return new WaitForEndOfFrame();
            Instantiate(gameObject, transform.parent);
            DestroySelf();
        }

        protected virtual void OnClothCollidersUpdated(List<CapsuleCollider> colliders)
        {
            cloth.capsuleColliders = colliders.ToArray();
            if (gameObject.activeInHierarchy)
                StartCoroutine(EnableClothEndOfFrame(colliders));
        }
        IEnumerator EnableClothEndOfFrame(List<CapsuleCollider> colliders)
        {
            yield return null;
            cloth.capsuleColliders = colliders.ToArray();
        }

        public void DestroySelf()
        {
            if(!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Destroy(gameObject);
            _isMarkedForDestruction = true;
            gameObject.SetActive(false);
        }

    }
}
