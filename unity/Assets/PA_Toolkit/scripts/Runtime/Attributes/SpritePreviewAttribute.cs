using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    public class SpritePreviewAttribute : PropertyAttribute
    {

        public int maxSize;

        public SpritePreviewAttribute(int maxSize = 50)
        {

            this.maxSize = maxSize;
        }
    }
}