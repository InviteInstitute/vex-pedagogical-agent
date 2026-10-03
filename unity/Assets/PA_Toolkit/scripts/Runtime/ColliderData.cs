using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PA_Toolkit
{
    [System.Serializable]
    public class ColliderData
    {
        public string id = "MISSING ID";
        [Header("Transform")]
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        [Header("Collider")]
        public Vector3 center;
        public float radius;
        public float height;
        [Tooltip("X-Axis: 0, Y-Axis: 1, Z-Axis: 2")]
        public int direction;

        public static ColliderData Lerp(ColliderData start, ColliderData end, float amount)
        {
            ColliderData value = new ColliderData();
            value.localPosition = Vector3.Lerp(start.localPosition, end.localPosition, amount);
            value.localRotation = Quaternion.Lerp(start.localRotation, end.localRotation, amount);
            value.localScale = Vector3.Lerp(start.localScale, end.localScale, amount);
            value.center = Vector3.Lerp(start.center, end.center, amount);
            value.radius = Mathf.Lerp(start.radius, end.radius, amount);
            value.height = Mathf.Lerp(start.height, end.height, amount);
            value.direction = amount < .5f ? start.direction : end.direction;
            return value;
        }

    }
    [System.Serializable]
    public class BodyTypeColliderData
    {
        public int bodyType;
        [SerializeField] List<ColliderData> _colliderData = new List<ColliderData>();
        public List<ColliderData> colliderData => _colliderData;
        public BodyTypeColliderData() { }
        public BodyTypeColliderData(int bodyType) { this.bodyType = bodyType; }

        public void OverrideColliderData(List<ColliderData> colliderData) =>
            _colliderData = colliderData;
        public void UpdateColliderData(List<ColliderData> colliderData)
        {
            foreach (var collider in colliderData)
            {
                int index = _colliderData.FindIndex((find) => find.id == collider.id);
                if (index < 0) _colliderData.Add(collider);
                else _colliderData[index] = collider;
            }
        }

        //static Dictionary<string, BodyTypeColliderData> colliderCache;
        //static int cacheSize;
        //static Action callbackOnFinishCache;
        //public static bool colliderCacheInit = false;
        //public static void InitColliderDataCache(Agent agent,
        //    Action callbackOnComplete = null)
        //{
        //    if (colliderCache != null) return;
        //    colliderCache = new Dictionary<string, BodyTypeColliderData>();
        //
        //    cacheSize = 0;
        //    callbackOnFinishCache = callbackOnComplete;
        //    for (int i = 0; i < agent.components.bodyTypeSOs.Count; ++i)
        //    {
        //        var locations = agent.components.bodyTypeSOs[i].
        //            colliderDataLocations.Values.ToList();
        //        for (int j = 0; j < locations.Count; j++)
        //        {
        //            cacheSize++;
        //            agent.StartCoroutine(InitColliderDataCacheRoutine(locations[j]));
        //        }
        //    }
        //}
        //
        //static IEnumerator InitColliderDataCacheRoutine(string localPath)
        //{
        //    if (string.IsNullOrEmpty(localPath) || localPath.Contains(".meta") ||
        //        colliderCache.ContainsKey(localPath))
        //
        //    {
        //        cacheSize--;
        //        yield break;
        //    }
        //    string json = "";
        //    using (var request = UnityWebRequest.Get(Path.Combine(
        //        Application.streamingAssetsPath, localPath)))
        //    {
        //        var asyncRequest = request.SendWebRequest();
        //        yield return new WaitForSeconds(1);
        //        while (!asyncRequest.isDone)
        //        {
        //            yield return null;
        //        }
        //        json = asyncRequest.webRequest.downloadHandler.text;
        //    }
        //    var colData = JsonUtility.FromJson<BodyTypeColliderData>(json);
        //    AddDataToCache(localPath, colData);
        //}

        //static void AddDataToCache(string key, BodyTypeColliderData data)
        //{
        //    key = key.Replace("\\", "/");
        //    if (colliderCache.ContainsKey(key))
        //    {
        //        cacheSize--;
        //    }
        //    else
        //    {
        //        colliderCache.Add(key, data);
        //    }
        //    if (colliderCache.Keys.Count < cacheSize) return;
        //    colliderCacheInit = true;
        //    if (callbackOnFinishCache != null)
        //        callbackOnFinishCache?.Invoke();
        //    Debug.Log("Cache Size " + cacheSize);
        //}
        /// <summary>
        /// Pulls collider data from the cached dictionary
        /// Path is local to Streaming Assets
        /// </summary>
        /// <param name="path">Location of the bodytype data in streaming assets</param>
        /// <returns></returns>
        //public static BodyTypeColliderData GetBodyTypeCollider(string localPath)
        //{
        //    Debug.Log("Getting" + localPath);
        //    if (colliderCache == null) return null;
        //    if (!colliderCache.TryGetValue(localPath, out var data)) return null;
        //    return data;
        //}


        /// <summary>
        /// Path is local to Streaming Assets
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static void ActionWithCollisionData
            (string path, MonoBehaviour coroutineRunner,
            Action<BodyTypeColliderData> action)
        {
            coroutineRunner.StartCoroutine(FromJsonPathRoutine(path, action));
        }

        static IEnumerator FromJsonPathRoutine(string path, Action<BodyTypeColliderData> action)
        {
            if (string.IsNullOrEmpty(path)) yield break;
            path = Path.Combine(Application.streamingAssetsPath, path);
            string json = "";
#if UNITY_EDITOR || !UNITY_WEBGL
            json = File.ReadAllText(path);
#else
            using (var request = UnityWebRequest.Get(path))
            {
                var asyncRequest = request.SendWebRequest();
                yield return new WaitForSeconds(1);
                while (!asyncRequest.isDone)
                {
                    yield return null;
                }
                json = asyncRequest.webRequest.downloadHandler.text;
            }
#endif


            var colData = JsonUtility.FromJson<BodyTypeColliderData>(json);
            action?.Invoke(colData);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }


        public static string GetColliderDataId(AgentSettings settings,
            ColSaveType type) =>
            GetColliderDataId(settings.bodyType, settings.hair,
                settings.accessories, type);

        public static string GetColliderDataId(BodyType body, Hair hair,
            List<Accessory> accessories, ColSaveType type)
        {
            StringBuilder sb = new StringBuilder();
            List<string> accessoryIds = new List<string>();

            if (type >= ColSaveType.Body)
            {
                sb.Append(body.id);
                sb.Append("_");
            }
            if (type >= ColSaveType.Hair)
            {
                CreationSetting setting = null;
                if (type < ColSaveType.Specific)
                {
                    foreach (var acc in accessories)
                    {
                        if (acc.overridesColliderData)
                        {
                            setting = acc;
                            break;
                        }
                    }
                }
                if (setting == null) setting = hair;

                sb.Append(setting.id);
                sb.Append("_");
            }
            if (type >= ColSaveType.Specific)
            {
                foreach (var accessory in accessories)
                {
                    accessoryIds.Add(accessory.id.Substring(accessory.id.Length - 2));
                }
                accessoryIds.Sort();
                sb.Append("acc-");
                for (int i = 0; i < accessoryIds.Count; ++i)
                {
                    sb.Append(accessoryIds[i]);
                    sb.Append("-");
                }
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        public enum ColSaveType
        {
            Body,
            Hair,
            Specific
        }


    }
}
