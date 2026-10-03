using UnityEngine;
using static PA_Toolkit.Agent;

namespace PA_Toolkit
{
    public static class Extensions
    {
        public static void CopyBonesOf(this SkinnedMeshRenderer skin, SkinnedMeshRenderer other)
        {
            skin.bones = other.bones;
            skin.rootBone = other.rootBone;
            skin.bounds = other.bounds;
        }
        public static void CopyBlendshapesOf(this SkinnedMeshRenderer skin, SkinnedMeshRenderer other)
        {
            for (int i = 0; i < other.sharedMesh.blendShapeCount; i++)
                skin.SetBlendShapeWeight(i, other.GetBlendShapeWeight(i));
        }
        public static ColliderData GetData(this CapsuleCollider collider, Transform agent)
        {
            ColliderData data = new ColliderData();
            //if (collider.TryGetComponent(out ObjectId id)) data.id = id.id;
            if (agent != null)
                data.id = collider.transform.GetPath(agent);
            data.localPosition = collider.transform.localPosition;
            data.localRotation = collider.transform.localRotation;
            data.localScale = collider.transform.localScale;
            data.center = collider.center;
            data.radius = collider.radius;
            data.height = collider.height;
            data.direction = collider.direction;

            return data;
        }

        public static ColliderData[] GetData(this CapsuleCollider[] colliders, Transform colliderParent)
        {
            ColliderData[] data = new ColliderData[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                data[i] = colliders[i].GetData(colliderParent);
            }
            return data;
        }

        public static void SetData(this CapsuleCollider collider, ColliderData data)
        {
            collider.transform.localPosition = data.localPosition;
            collider.transform.localRotation = data.localRotation;
            collider.transform.localScale = data.localScale;
            collider.center = data.center;
            collider.radius = data.radius;
            collider.height = data.height;
            collider.direction = data.direction;
        }
        public static string GetPath(this Transform current, Transform stopAt = null, bool includeParentName = false)
        {
            if (current.parent == stopAt)
            {
                if (!includeParentName) return current.name;
                else if (current.parent == null) return "/" + current.name;
                else return current.parent.name + "/" + current.name;
            }
            return current.parent.GetPath(stopAt) + "/" + current.name;
        }

    }
}
