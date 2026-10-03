using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    [System.Serializable]
    public class Thumbnail
    {
        public enum Type
        {
            sprite,
            gradient
        }

        [SerializeField] Type _type = Type.sprite;
        public Type type => _type;

        [SpritePreview(75), SerializeField] Sprite _sprite;
        public Sprite sprite => _sprite;

        [SerializeField] Color _color01 = Color.white;
        public Color color01 => _color01;

        [SerializeField] Color _color02 = Color.grey;
        public Color color02 => _color02;
    }
}