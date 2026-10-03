#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace SerializableClasses
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty m_keyValuePairs = property.FindPropertyRelative("keyValuePairs");
            return EditorGUI.GetPropertyHeight(m_keyValuePairs, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty m_keyValuePairs = property.FindPropertyRelative("keyValuePairs");
            EditorGUI.PropertyField(position, m_keyValuePairs, label, true);
            EditorGUI.EndProperty();
        }
    }
}
#endif