using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [System.Serializable]
    public class SerializableTexture
    {
        [SerializeField] Texture2D _tex;
        public Texture2D tex
        {
            get
            {
                if (_tex == null) Deserialize();
                return _tex;
            }
            set
            {
                _tex = value;
                Serialize();
            }
        }

        [SerializeField, HideInInspector] Color32[] _colors;
        [SerializeField, HideInInspector] TextureFormat _format;
        [SerializeField, HideInInspector] int _width;
        [SerializeField, HideInInspector] int _height;
        void Deserialize()
        {
            if (_colors == null || _colors.Length == 0) return;
            _tex = new Texture2D(_width, _height, _format, 1, true, false);
            _tex.LoadRawTextureData(new Unity.Collections.NativeArray<Color32>(_colors, 
                            Unity.Collections.Allocator.Temp));
            _tex.wrapMode = TextureWrapMode.Clamp;
            _tex.Apply();
        }

        void Serialize()
        {
            if(_tex == null) return;
            _colors = _tex.GetRawTextureData<Color32>().ToArray();
            _format = _tex.format;
            _width = tex.width;
            _height = tex.height;
        }
    }
}
