using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

#if UNITY_EDITOR
namespace PA_Toolkit
{
    [CustomPropertyDrawer(typeof(Thumbnail))]
    public class ThumbnailDrawer : PropertyDrawer
    {
        protected float lineHeight = EditorGUIUtility.singleLineHeight;
        protected float vSpace = EditorGUIUtility.standardVerticalSpacing;
        protected int indentWidth = 15;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty m_type = property.FindPropertyRelative("_type");
            SerializedProperty m_sprite = property.FindPropertyRelative("_sprite");

            if (property.isExpanded)
            {
                if (m_type.hasMultipleDifferentValues)
                    return lineHeight * 2 + EditorGUI.GetPropertyHeight(m_sprite) + 2 * vSpace;

                if (m_type.enumValueIndex == (int)Thumbnail.Type.sprite)
                    return lineHeight * 2 + vSpace;
                else
                    return lineHeight * 2 + EditorGUI.GetPropertyHeight(m_sprite) + 2 * vSpace;
            }
            else
                return lineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
            foldoutStyle.fontStyle = FontStyle.Normal;
            GUIStyle enumStyle = new GUIStyle(EditorStyles.popup);

            SerializedProperty m_type = property.FindPropertyRelative("_type");
            SerializedProperty m_sprite = property.FindPropertyRelative("_sprite");
            SerializedProperty m_color01 = property.FindPropertyRelative("_color01");
            SerializedProperty m_color02 = property.FindPropertyRelative("_color02");


            bool hierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = true;

            Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);


            property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, property.isExpanded, property.displayName, foldoutStyle);
            EditorGUIUtility.hierarchyMode = hierarchyMode;

            EditorGUI.BeginProperty(foldoutRect, label, property);
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    Rect typeRect = new Rect(position.x, foldoutRect.yMax + vSpace, position.width, lineHeight);
                    EditorGUI.PropertyField(typeRect, m_type);
                    //EditorGUILayout.PropertyField(m_type);

                    if (!m_type.hasMultipleDifferentValues)// using (new EditorGUI.DisabledGroupScope(m_type.hasMultipleDifferentValues))
                    {
                        if (m_type.enumValueIndex == (int)Thumbnail.Type.sprite)
                        {
                            //Rect spriteRect = new Rect(position.x, typeRect.yMax + vSpace, EditorGUI.GetPropertyHeight(m_sprite) + 50, EditorGUI.GetPropertyHeight(m_sprite));
                            //EditorGUI.PropertyField(spriteRect, m_sprite, true);
                            EditorGUILayout.PropertyField(m_sprite);
                        }
                        else
                        {
                            Rect color01Rect = new Rect(position.x, typeRect.yMax + vSpace, position.width, lineHeight);
                            Rect color02Rect = new Rect(position.x, color01Rect.yMax + vSpace, position.width, lineHeight);
                            EditorGUI.PropertyField(color01Rect, m_color01);
                            EditorGUI.PropertyField(color02Rect, m_color02);
                            //EditorGUILayout.PropertyField(m_color01);
                            //EditorGUILayout.PropertyField(m_color02);
                        }
                    }
                }
            }

            EditorGUI.EndFoldoutHeaderGroup();
            EditorGUI.EndProperty();
        }
    }
}
#endif