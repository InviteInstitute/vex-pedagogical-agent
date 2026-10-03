using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SerializableClasses;

namespace PA_Toolkit
{
    public class Agent : MonoBehaviour
    {
        public const string NONE = "None";
        public const string DEFAULT_GESTURE = "idle";
        public const string DEFAULT_EXPRESSION = "neutral";
        public const string SKIN_COLOR_NAME = "_Color"; //Reverted back to default shader. _MainColor is the toon shader name
        public const string DECAL = "_DecalTex0";
        public int gestureLayer => actionComponents.animator.GetLayerIndex("gestures");
        public int blinkLayer => actionComponents.animator.GetLayerIndex("blink");
        public int expressionsLayer => actionComponents.animator.GetLayerIndex("expressions");
        public int mouthLayer => actionComponents.animator.GetLayerIndex("mouth");
        public int bodyLayer => actionComponents.animator.GetLayerIndex("body");

        BodyType currentBodyType;
        Hair currentHair;


        IEnumerator playActionRoutine;

        [SerializeField, ReadOnly] string _agentId = "DefaultAgent";
        [HideInInspector] public string agentName;
        [HideInInspector] public string agentNotes;

        bool isInWheelchair;

        [SerializeField, HideInInspector] int _activeBodyIndex;
        int activeBodyIndex
        {
            get
            {
                return _activeBodyIndex;
            }
            set
            {
                for (int i = 0; i < components.bodyTypes.Count; ++i)
                {
                    if (i == value) components.bodyTypes[i].SetValue(100);
                    else components.bodyTypes[i].SetValue(0);
                }

                _activeBodyIndex = value;
                if (onBodyTypeChanged != null)
                    onBodyTypeChanged?.Invoke(_activeBodyIndex);
                UpdateInstanceColliders();
            }
        }

        [SerializeField, ReadOnly] Texture2D _activeDecal;
        public Texture2D activeDecal => _activeDecal;

        Color _skinColor;
        Color skinColor
        {
            get { return _skinColor; }
            set
            {
                _skinColor = value;

                if (Application.isPlaying)
                {
                    components.face.material.SetColor(SKIN_COLOR_NAME, skinColor);
                    components.hands.material.SetColor(SKIN_COLOR_NAME, skinColor);
                    //components.outfit.material.SetColor(SKIN_COLOR_NAME, skinColor);
                    //components.shoes.material.SetColor(SKIN_COLOR_NAME, skinColor);
                }
                else
                {
                    components.face.sharedMaterial.SetColor(SKIN_COLOR_NAME, skinColor);
                    components.hands.sharedMaterial.SetColor(SKIN_COLOR_NAME, skinColor);
                    //components.outfit.sharedMaterial.SetColor(SKIN_COLOR_NAME, skinColor);
                    //components.shoes.sharedMaterial.SetColor(SKIN_COLOR_NAME, skinColor);
                    components.outfit.GetComponentInChildren<OutfitHelper>().skin.sharedMaterial.SetColor(SKIN_COLOR_NAME, skinColor);
                    components.shoes.GetComponentInChildren<ShoeHelper>().skin.sharedMaterial.SetColor(SKIN_COLOR_NAME, skinColor);
                }
                if (onSkinColorChanged != null)
                    onSkinColorChanged?.Invoke(skinColor);
            }
        }

        Color _hairColor;
        Color hairColor
        {
            get { return _hairColor; }
            set
            {
                _hairColor = value;
                //components.hair.skin.sharedMaterial.SetColor("_Color", _hairColor);
                //hairMats[0].SetColor("_Color", _hairColor);
                if (onHairColorChanged != null)
                    onHairColorChanged?.Invoke(_hairColor);
            }
        }

        public delegate void AgentDelegate();
        public event AgentDelegate onHairChanged;

        public delegate void BodyTypeDelegate(int bodyType);
        public event BodyTypeDelegate onBodyTypeChanged;

        public delegate void CollisionDelegate(List<CapsuleCollider> colliders);
        public event CollisionDelegate onClothCollidersChanged;

        public delegate void ColorDelegate(Color color);
        public event ColorDelegate onHairColorChanged;
        public event ColorDelegate onSkinColorChanged;

        public delegate void TextureDelegate(Texture2D texture);
        public event TextureDelegate onDecalChanged;

        public delegate void BodyTypeColliderDelegate(BodyTypeColliderData data);
        public event BodyTypeColliderDelegate onBodyTypeCollidersChanged;

        //stores animation parameters for restoration after an animator rebind
        public class AnimationParameter
        {
            public AnimatorControllerParameterType type;
            public string name;
            public int intValue;
            public float floatValue;
            public bool boolValue;

            public AnimationParameter(string name, int intValue)
            {
                this.type = AnimatorControllerParameterType.Int;
                this.name = name;
                this.intValue = intValue;
            }
            public AnimationParameter(string name, float floatValue)
            {
                this.type = AnimatorControllerParameterType.Float;
                this.name = name;
                this.floatValue = floatValue;
            }
            public AnimationParameter(string name, bool boolValue)
            {
                this.type = AnimatorControllerParameterType.Bool;
                this.name = name;
                this.boolValue = boolValue;
            }
        }

        [System.Serializable]
        public class CreationGroup
        {
            [SerializeField] GameObject _group;
            public GameObject group => _group;

            [SerializeField] SkinnedMeshRenderer _skin;
            public SkinnedMeshRenderer skin => _skin;

            [SerializeField] Transform _parent;
            public Transform parent => _parent;
        }

        [System.Serializable]
        public class Accessories
        {
            [SerializeField] CreationGroup _headTop;
            public CreationGroup headTop => _headTop;

            [SerializeField] CreationGroup _eyes;
            public CreationGroup eyes => _eyes;

            [SerializeField] CreationGroup _earLeft;
            public CreationGroup earLeft => _earLeft;

            [SerializeField] CreationGroup _earRight;
            public CreationGroup earRight => _earRight;

            [SerializeField] CreationGroup _mouth;
            public CreationGroup mouth => _mouth;

            [SerializeField] CreationGroup _neck;
            public CreationGroup neck => _neck;

            [SerializeField] CreationGroup _forearmLeft;
            public CreationGroup forearmLeft => _forearmLeft;

            [SerializeField] CreationGroup _forearmRight;
            public CreationGroup forearmRight => _forearmRight;

            public List<CreationGroup> allSlots
            {
                get
                {
                    CreationGroup[] temp = new CreationGroup[] { headTop, eyes, earLeft, earRight, mouth, neck, forearmLeft, forearmRight };
                    return new List<CreationGroup>(temp);
                }
            }

            public AccessoryHelper[] instances => headTop.skin.GetComponentsInChildren<AccessoryHelper>();

            List<Accessory> _accessories = new List<Accessory>();
            public List<Accessory> accessories
            {
                get
                {
                    _accessories.Clear();
                    foreach (var helper in instances)
                        _accessories.Add(helper.Accessory);
                    return _accessories;
                }
            }

            public List<CreationGroup> overflowAccessories = new List<CreationGroup>();
        }

        [System.Serializable]
        public class Components
        {
            [SerializeField] Transform _headJoint;
            public Transform headJoint => _headJoint;

            [SerializeField] SkinnedMeshRenderer _face;
            public SkinnedMeshRenderer face => _face;

            [SerializeField] SkinnedMeshRenderer _hands;
            public SkinnedMeshRenderer hands => _hands;

            [SerializeField] CreationGroup _hair;
            public CreationGroup hair => _hair;

            [SerializeField] Renderer[] _eyeRenderers;
            public Renderer[] eyeRenderers => _eyeRenderers;

            [SerializeField] SkinnedMeshRenderer _outfit;
            public SkinnedMeshRenderer outfit => _outfit;

            [SerializeField] SkinnedMeshRenderer _shoes;
            public SkinnedMeshRenderer shoes => _shoes;

            [SerializeField] Accessories _accessories;
            public Accessories accessories => _accessories;

            [SerializeField] Transform[] _eyeAnchors;
            public Transform[] eyeAnchors => _eyeAnchors;
            [SerializeField] BodyTypeEyeData[] _bodyTypeEyeData;

            [SerializeField] GameObject _wheelchair;
            public GameObject wheelchair => _wheelchair;

            [SerializeField] SerializeableDictionary_EnumKeys<PAJoint, Transform> _jointDictionary;
            public SerializeableDictionary_EnumKeys<PAJoint, Transform> jointDictionary => _jointDictionary;

            [SerializeField] List<BodyType> _bodyTypeSOs;
            public List<BodyType> bodyTypeSOs => _bodyTypeSOs;

            //Redefine as constant values using the references above
            //Predefine c_0 -> c_04
            List<BodyTypeData> _bodyTypes;
            public List<BodyTypeData> bodyTypes
            {
                get
                {
                    if (_bodyTypes == null || _bodyTypes.Count == 0)
                    {
                        InitializeBodyTypes();
                    }
                    return _bodyTypes;
                }
                set { _bodyTypes = value; }
            }

            void InitializeBodyTypes()
            {
                _bodyTypes = new List<BodyTypeData>();
                List<SkinnedMeshRenderer> allMeshes = new List<SkinnedMeshRenderer>()
                { face, hands, hair.skin, outfit, shoes};
                foreach (var group in accessories.allSlots)
                {
                    allMeshes.Add(group.skin);
                }
                foreach (var group in accessories.overflowAccessories)
                {
                    allMeshes.Add(group.skin);
                }


                foreach (BodyTypeEyeData eyeData in _bodyTypeEyeData)
                {
                    _bodyTypes.Add(new BodyTypeData(eyeData, allMeshes.ToArray()));
                }
            }

            [System.Serializable]
            public class BodyTypeEyeData
            {
                public int bodyTypeIndex;
            }


            [System.Serializable]
            public class BodyTypeData
            {
                public BodyTypeData(BodyTypeEyeData eyedata, SkinnedMeshRenderer[] meshRenderers)
                {
                    _bodyTypeIndex = eyedata.bodyTypeIndex;
                    //_eyePos = eyedata.leftEyePosition;
                    _meshRenderers = meshRenderers;
                }

                SkinnedMeshRenderer[] _meshRenderers;
                int _bodyTypeIndex;
                public void SetValue(float value)
                {
                    foreach (SkinnedMeshRenderer renderer in _meshRenderers)
                    {

                        string blendShapeName = "c_0" + _bodyTypeIndex;
                        if (renderer.sharedMesh == null) continue; //Sometimes your accessory has no mesh
                        int index = renderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
                        if (index == -1)
                        {
                            continue;
                        }
                        renderer.SetBlendShapeWeight(index, value);

                    }
                    //_leftEye.localPosition = _eyePos;
                    //_rightEye.localPosition = new Vector3(_eyePos.x, _eyePos.y, -_eyePos.z);
                }
            }

        }

        [System.Serializable]
        public class ActionComponents
        {
            [SerializeField] AudioSource _audioSource;
            public AudioSource audioSource => _audioSource;

            [SerializeField] Animator _animator;
            public Animator animator => _animator;
        }

        [SerializeField] ActionComponents _actionComponents;
        public ActionComponents actionComponents => _actionComponents;

        [SerializeField] Components _components;
        public Components components => _components;

        BlinkManager _blinkManager;
        BlinkManager blinkManager
        {
            get
            {
                if (_blinkManager == null)
                {
                    _blinkManager = GetComponent<BlinkManager>();
                }
                return _blinkManager;
            }
        }


        //----------------------------------\/\/\/FUNCTIONS\/\/\/---------------------------/\/\/\CLASSES & VARIABLES/\/\/\---------------------------

        public void ResetAnimator()
        {
            Animator anim = actionComponents.animator;
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            //store current state and time of animation
            float normalizedTime = stateInfo.normalizedTime;
            int stateHash = stateInfo.fullPathHash;

            //store current parameter values
            AnimatorControllerParameter[] parameters = anim.parameters;
            List<AnimationParameter> parameterValues = new List<AnimationParameter>(0);
            foreach (AnimatorControllerParameter p in parameters)
            {
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Int:
                        parameterValues.Add(new AnimationParameter(p.name, anim.GetInteger(p.name)));
                        break;

                    case AnimatorControllerParameterType.Float:
                        parameterValues.Add(new AnimationParameter(p.name, anim.GetFloat(p.name)));
                        break;

                    case AnimatorControllerParameterType.Bool:
                        parameterValues.Add(new AnimationParameter(p.name, anim.GetBool(p.name)));
                        break;
                }
            }

            //TODO --- Add catch here for out of bounds errors
            //rebind animator in case the heirarchy of the animator has changed
            anim.Rebind();

            //restore animation state and time
            anim.Play(stateHash, 0, normalizedTime);

            //restore parameters
            foreach (AnimationParameter p in parameterValues)
            {
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Int:
                        anim.SetInteger(p.name, p.intValue);
                        break;

                    case AnimatorControllerParameterType.Float:
                        anim.SetFloat(p.name, p.floatValue);
                        break;

                    case AnimatorControllerParameterType.Bool:
                        anim.SetBool(p.name, p.boolValue);
                        break;
                }
            }
        }

        public void OnChangeSetting(CreationSetting setting)
        {
            if (setting is Preset) OnChangePreset(setting as Preset);
            else if (setting is Face) OnChangeFace(setting as Face);
            else if (setting is SkinColor) OnChangeSkinColor(setting as SkinColor);
            else if (setting is Hair) OnChangeHair(setting as Hair);
            else if (setting is HairColor) OnChangeHairColor(setting as HairColor);
            else if (setting is EyeShape) OnChangeEyeShape(setting as EyeShape);
            else if (setting is Eyes) OnChangeEyes(setting as Eyes);
            else if (setting is Outfit) OnChangeOutfit(setting as Outfit);
            else if (setting is BodyType) OnChangeBodyType(setting as BodyType);
            else if (setting is Shoes) OnChangeShoes(setting as Shoes);
        }

        public void OnChangeSettings(List<CreationSetting> settings)
        {
            if (settings != null && settings[0] is Accessory)
            {
                print("change acc");
                OnChangeAccessory(settings.Cast<Accessory>().ToList());
            }
            else
            {
                print("OCS FAIL - null?" + settings == null + " is accessory?" + (settings[0] is Accessory));
            }
        }

        public void OnChangePreset(Preset preset)
        {
            OnChangeFace(preset.settings.face);
            OnChangeSkinColor(preset.settings.skinColor);
            OnChangeHair(preset.settings.hair);
            OnChangeHairColor(preset.settings.hairColor);
            OnChangeEyeShape(preset.settings.eyeShape);
            OnChangeEyes(preset.settings.eyes);
            OnChangeOutfit(preset.settings.outfit);
            OnChangeBodyType(preset.settings.bodyType);
            OnChangeShoes(preset.settings.shoes);
            OnChangeAccessory(preset.settings.accessories.Count > 0 ? preset.settings.accessories : null);
            OnChangeWheelchair(preset.settings.useWheelchair);
            OnChangeDecal(preset.settings.decal);
        }

        public void OnChangeWheelchair(bool useWheelchair)
        {
            isInWheelchair = useWheelchair;

            components.wheelchair.SetActive(useWheelchair);
            if (isInWheelchair && currentBodyType.wheelchairAnimator != null) actionComponents.animator.runtimeAnimatorController = currentBodyType.wheelchairAnimator;
            else if (!isInWheelchair && currentBodyType.standingAnimator != null) actionComponents.animator.runtimeAnimatorController = currentBodyType.standingAnimator;

            activeBodyIndex = activeBodyIndex;
        }


        public void OnChangeFace(Face face)
        {
            if (face == null)
            {
                Debug.Log("Face given was null");
                return;
            }

            components.face.sharedMesh = face.mesh;
            if (Application.isPlaying)
            {
                components.face.SetMaterials(face.materials);
                //sync skin color
                if (components.hands.material.GetColor(SKIN_COLOR_NAME) != null)
                    skinColor = skinColor;
            }
            else
            {
                components.face.SetSharedMaterials(face.materials);
            }
        }

        public void OnChangeSkinColor(SkinColor skinColor)
        {
            if (skinColor == null)
            {
                Debug.Log("Skin color given was null");
                return;
            }
            this.skinColor = skinColor.tint;
        }

        public void OnChangeHair(Hair hair)
        {
            if (hair == null)
            {
                Debug.Log("Hair given was null");
                return;
            }

            currentHair = hair;

            //Destroy Physics based Hair
            for (int i = components.hair.skin.transform.childCount - 1; i >= 0; --i)
            {
                if (Application.isPlaying) Destroy(components.hair.skin.transform.GetChild(i).gameObject);
                else DestroyImmediate(components.hair.skin.transform.GetChild(i).gameObject);
            }

            //bound mesh

            if (hair.hairPrefab != null)
            {
                components.hair.skin.materials = new Material[0];
                components.hair.skin.enabled = false;
                Instantiate(hair.hairPrefab, components.hair.skin.transform);

                if (onHairChanged != null)
                    onHairChanged?.Invoke();

                UpdateInstanceColliders();
            }

            //unbound prefab
            foreach (Transform t in components.hair.parent)
            {
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
            if (hair.childPrefab != null)
            {
                GameObject hairChild = Instantiate(hair.childPrefab, components.hair.parent);
                hairChild.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = hair.materials[0];
            }

            hairColor = hairColor;
            activeBodyIndex = activeBodyIndex;
        }

        List<CapsuleCollider> GetAllClothColliders()
        {
            List<CapsuleCollider> colliders = new List<CapsuleCollider>();
            //colliders.AddRange(components.bodyColliders);
            if (components.outfit.transform.childCount > 0 &&
                components.outfit.transform.GetChild(components.outfit.transform.childCount - 1).
                TryGetComponent(out OutfitHelper outfitHelper))
                colliders.AddRange(outfitHelper.colliders);

            foreach (AccessoryHelper helper in components.accessories.instances)
                colliders.AddRange(helper.colliders);

            return colliders;
        }

        void UpdateInstanceColliders()
        {
            if (currentBodyType == null || currentHair == null) return;
            string id;

            //Look for settings going from Specific, to Hair to Body
            for (int i = 2; i >= 0; --i)
            {
                id = BodyTypeColliderData.GetColliderDataId(currentBodyType,
                currentHair, components.accessories.accessories,
                (BodyTypeColliderData.ColSaveType)i);
                if (currentBodyType.colliderDataLocations.TryGetValue(id, out string dataLoc))
                {
#if !UNITY_EDITOR

                    if (currentBodyType.colliderDataCache
                    .TryGetValue(dataLoc, out var data))
                    {
                        UpdateInstanceColliders(data);
                    }
                    else


#endif
                    BodyTypeColliderData.ActionWithCollisionData(dataLoc, this,
                    (colData) => UpdateInstanceColliders(colData));

                    return;
                }
            }
            Debug.LogWarning("No Collision Data Found!!");
        }
        public void UpdateInstanceColliders(BodyTypeColliderData data)
        {
            if (data == null)
            {
                Debug.LogWarning("Collision Data was null");
                return;
            }
            if (data != null && onBodyTypeCollidersChanged != null)
                onBodyTypeCollidersChanged?.Invoke(data);
            if (onClothCollidersChanged != null)
                onClothCollidersChanged?.Invoke(GetAllClothColliders());
        }

        public void OnChangeHairColor(HairColor hairColor)
        {
            if (hairColor == null)
            {
                Debug.Log("Hair Color given was null");
                return;
            }
            this.hairColor = hairColor.tint;
        }

        public void OnChangeEyeShape(EyeShape eyeShape)
        {
            if (eyeShape == null)
            {
                Debug.Log("Eye Shape given was null");
                return;
            }

            if (eyeShape.blendShapes.Count != eyeShape.blendShapeValues.Count)
            {
                Debug.Log($"Mismached blendshape values for {eyeShape.id}");
                return;
            }

            for (int i = 0; i < eyeShape.blendShapes.Count; ++i)
            {
                string blendName = eyeShape.blendShapes[i];
                int index = components.face.sharedMesh.GetBlendShapeIndex(blendName);
                if (index == -1)
                {
                    Debug.LogWarning($"Blendshape of name {blendName} couldn't be found");
                    continue;
                }
                components.face.SetBlendShapeWeight(index, eyeShape.blendShapeValues[i]);
            }

            blinkManager.blinkState = eyeShape.blinkAnimationName;
        }

        public void OnChangeEyes(Eyes eyes)
        {
            if (eyes == null)
            {
                Debug.Log("Eyes given were null");
                return;
            }

            foreach (Renderer r in components.eyeRenderers)
            {
                if (Application.isPlaying) r.material.SetTexture("_MainTex", eyes.texture);
                else
                {
                    r.sharedMaterial = eyes.material;
                    r.sharedMaterial.SetTexture("_MainTex", eyes.texture);

                }
            }
        }

        public void OnChangeOutfit(Outfit outfit)
        {
            if (outfit == null)
            {
                Debug.Log("Outfit given was null");
                return;
            }

            components.outfit.enabled = false;
            components.outfit.sharedMesh = outfit.mesh;
            components.outfit.materials = new Material[0];

            components.outfit.GetComponentInChildren<OutfitHelper>()?.DestroySelf();
            var outfitInst = Instantiate(outfit.helper, components.outfit.transform);
            //outfitInst.AssignAgent(this);

            if (Application.isPlaying)
            {
                //components.outfit.SetMaterials(outfit.materials);
                outfitInst.skin.SetMaterials(outfit.materials);
                components.hands.material = outfit.materials[0];
                //sync skin color
                skinColor = skinColor;
            }
            else
            {
                //components.outfit.SetSharedMaterials(outfit.materials);
                outfitInst.skin.SetSharedMaterials(outfit.materials);
                components.hands.sharedMaterial = outfit.materials[0];
            }
            activeBodyIndex = activeBodyIndex;
        }

        public void OnChangeBodyType(BodyType bodyType)
        {
            if (bodyType == null)
            {
                Debug.Log("BodyType given was null");
                return;
            };

            currentBodyType = bodyType;
            //standingAnimator = bodyType.standingAnimator;
            //wheelchairAnimator = bodyType.wheelchairAnimator;

            actionComponents.animator.runtimeAnimatorController = isInWheelchair
                ? currentBodyType.wheelchairAnimator
                : currentBodyType.standingAnimator;

            Vector3 eyePos = bodyType.leftEyePosition;
            components.eyeAnchors[0].localPosition = eyePos;
            eyePos.z *= -1;
            components.eyeAnchors[1].localPosition = eyePos;

            activeBodyIndex = bodyType.index;

            ResetAnimator();
        }

        public void OnChangeShoes(Shoes shoes)
        {
            if (shoes == null)
            {
                Debug.Log("Shoe given was null");
                return;
            }

            components.shoes.enabled = false;
            components.shoes.sharedMesh = shoes.mesh;
            components.shoes.materials = new Material[0];

            components.shoes.GetComponentInChildren<ShoeHelper>()?.DestroySelf();
            var shoesInst = Instantiate(shoes.shoeHelper, components.shoes.transform);
            //shoesInst.AssignAgent(this);


            //sync skin color
            if (Application.isPlaying)
            {
                //components.shoes.SetMaterials(shoes.materials);
                shoesInst.skin.SetMaterials(shoes.materials);
                if (components.face.material.GetColor(SKIN_COLOR_NAME) != null)
                    skinColor = skinColor;
            }
            else
            {
                //components.shoes.SetSharedMaterials(shoes.materials);
                shoesInst.skin.SetSharedMaterials(shoes.materials);
            }

            activeBodyIndex = activeBodyIndex;
        }

        public void OnChangeAccessory(List<Accessory> accessories)
        {
            //Empty list to remove all existing accessories
            if (accessories == null) accessories = new List<Accessory>();
            else accessories = new List<Accessory>(accessories);


            bool disableHair = false;
            bool hairStartState = components.hair.group.activeSelf;
            //clear all skinned accessory slots, and destroy all geo under parents
            List<CreationGroup> allSlots = components.accessories.allSlots;
            foreach (CreationGroup g in allSlots)
            {
                foreach (var accessory in g.skin.GetComponentsInChildren<AccessoryHelper>())
                {
                    if (accessories.Contains(accessory.Accessory))
                    {
                        if (accessory.Accessory.disableHair)
                            disableHair = true;
                        accessories.Remove(accessory.Accessory);
                        continue;
                    }
                    accessory.DestroySelf();
                    if (!Application.isPlaying) DestroyImmediate(accessory.gameObject);
                }
            }


            for (int i = 0; i < accessories.Count; i++)
            {
                if (accessories[i] == null)
                {
                    Debug.Log("Accessory given was null");
                    return;
                }

                var accessory = Instantiate(accessories[i].prefab,
                    components.accessories.headTop.skin.transform);
                //accessory.AssignAgent(this);
                accessory.AssignHeadJoint(components.headJoint);

                if (accessories[i].disableHair)
                {
                    disableHair = true;
                }
            }

            //disable hair, if needed
            components.hair.group.SetActive(!disableHair);
            activeBodyIndex = activeBodyIndex;
            UpdateInstanceColliders();
            if (((hairStartState == disableHair) || disableHair) && onHairChanged != null)
            {
                onHairChanged?.Invoke();
                hairColor = hairColor;
            }
        }

        public void OnChangeDecal(Texture2D tex)
        {
            _activeDecal = tex;
            if (Application.isPlaying)
            {
                if (onDecalChanged != null)
                    onDecalChanged?.Invoke(tex);
                return;
            }

            SkinnedMeshRenderer outfitSkin = components.outfit;
            if (components.outfit.transform.childCount > 0 &&
                components.outfit.transform.GetChild(0).TryGetComponent(out OutfitHelper outfit))
                outfitSkin = outfit.skin;

            if (Application.isPlaying
                ? outfitSkin.materials.Length < 2 ||
                    outfitSkin.materials[1].HasTexture(DECAL)
                : outfitSkin.sharedMaterials.Length < 2 ||
                    outfitSkin.sharedMaterials[1].HasTexture(DECAL))
                return;

            //if (Application.isPlaying)
            //    outfitSkin.materials[1].SetTexture(decalName, tex);
            //else
            outfitSkin.sharedMaterials[1].SetTexture(DECAL, tex);
        }
        public void PlayAction(ActionSettings actionSettings)
        {
            if (actionSettings == null) return;
            if (playActionRoutine != null) StopCoroutine(playActionRoutine);
            playActionRoutine = PlayActionRoutine(actionSettings);
            StartCoroutine(playActionRoutine);
        }
        IEnumerator PlayActionRoutine(ActionSettings actionSettings)
        {
            string gestureName = actionSettings.gesture != null ? actionSettings.gesture.stateName : DEFAULT_GESTURE;
            string expressionName = actionSettings.expression != null ? actionSettings.expression.stateName : DEFAULT_EXPRESSION;

            actionComponents.animator.CrossFadeInFixedTime(gestureName, .25f, gestureLayer);
            actionComponents.animator.CrossFadeInFixedTime(expressionName, .25f, expressionsLayer);

            actionComponents.audioSource.clip = actionSettings.dialogue.audio;
            actionComponents.audioSource.Play();

            //wait until audio clip (or animation if no audio clip attached) has played, then go back to default expression        
            yield return null;
            float delay = actionSettings.dialogue.audio != null ? actionSettings.dialogue.audio.length : actionComponents.animator.GetCurrentAnimatorStateInfo(gestureLayer).length;
            yield return new WaitForSeconds(delay);
            actionComponents.animator.CrossFadeInFixedTime(DEFAULT_EXPRESSION, .25f, expressionsLayer);
        }


        public void StopAction()
        {
            actionComponents.animator.CrossFadeInFixedTime(DEFAULT_GESTURE, .25f, gestureLayer);
            actionComponents.animator.CrossFadeInFixedTime(DEFAULT_EXPRESSION, .25f, expressionsLayer);

            actionComponents.audioSource.Stop();
        }

        public void SetAgentId(string agentId)
        {
            _agentId = agentId;
        }
    }
}