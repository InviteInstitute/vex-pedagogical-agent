
using UnityEngine;

namespace PA_Toolkit
{
    public class HairHelper : BodyPartHelper
    {
        [SerializeField] Hair hair;
        protected override void OnEnable()
        {
            base.OnEnable();
            skin.sharedMaterial = hair.materials[0];
        }

        protected override void Subscribe()
        {
            base.Subscribe();
            agent.onHairColorChanged += UpdateHairColor;
        }

        protected override void Unsubscribe()
        {
            base.Unsubscribe();
            agent.onHairColorChanged -= UpdateHairColor;
        }

        void UpdateHairColor(Color color)
        {
            skin.sharedMaterial.color = color;
        }

    }
}
