using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    public class ShoeHelper : BodyPartHelper
    {
        protected override void Subscribe()
        {
            base.Subscribe();
            agent.onSkinColorChanged += OnSkinColorChanged;
        }

        protected override void Unsubscribe()
        {
            base.Unsubscribe();
            agent.onSkinColorChanged -= OnSkinColorChanged;
        }

        void OnSkinColorChanged(Color color)
        {
            if (Application.isPlaying)
                skin.materials[0].SetColor(Agent.SKIN_COLOR_NAME, color);
            else
                skin.sharedMaterials[0].SetColor(Agent.SKIN_COLOR_NAME, color);
        }
    }
}
