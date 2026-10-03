using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
namespace PA_Toolkit
{


    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    public class SpritePreviewAttributeDrawer : PropertyDrawer
    {

        float lineHeight = EditorGUIUtility.singleLineHeight;
        int indentWidth = 15;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            SpritePreviewAttribute t = (SpritePreviewAttribute)attribute;

            float aspectRatio = GetAspectRatio(property);

            return lineHeight + GetHeight(aspectRatio, t.maxSize);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            SpritePreviewAttribute t = (SpritePreviewAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);


            //determine positioning
            float aspectRatio = GetAspectRatio(property);
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
            labelRect = EditorGUI.IndentedRect(labelRect);

            Rect nameRect = new Rect(position.x + labelRect.width, position.y, position.width - labelRect.width, lineHeight);
            Rect previewRect = new Rect(position.x + labelRect.width, position.y + lineHeight, GetWidth(aspectRatio, t.maxSize) + EditorGUI.indentLevel * indentWidth, GetHeight(aspectRatio, t.maxSize));


            //label
            GUI.Label(labelRect, property.displayName);


            //sprite name
            EditorGUI.BeginDisabledGroup(true);
            string spriteName = "—";
            if (!property.hasMultipleDifferentValues)
                spriteName = property.objectReferenceValue != null ? property.objectReferenceValue.name : "None (Sprite)";
            EditorGUI.LabelField(nameRect, spriteName);
            EditorGUI.EndDisabledGroup();


            //sprite
            EditorGUI.BeginChangeCheck();

            //temp store value of multi selection
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            UnityEngine.Object newValue = EditorGUI.ObjectField(previewRect, property.objectReferenceValue, typeof(Sprite), false);
            EditorGUI.showMixedValue = false;

            //only apply new value to selection if it was changed
            if (EditorGUI.EndChangeCheck())
                property.objectReferenceValue = newValue;


            EditorGUI.EndProperty();
        }



        float GetAspectRatio(SerializedProperty property)
        {

            if (property.objectReferenceValue != null)
            {
                Sprite s = (Sprite)property.objectReferenceValue;
                return s.bounds.size.x / (float)s.bounds.size.y;
            }
            return 1;
        }
        float GetHeight(float aspectRatio, float maxSize)
        {

            if (aspectRatio <= 1)
                return maxSize;
            else
                return maxSize / aspectRatio;
        }
        float GetWidth(float aspectRatio, float maxSize)
        {

            if (aspectRatio >= 1)
                return maxSize;
            else
                return maxSize * aspectRatio;
        }
    }
}
#endif