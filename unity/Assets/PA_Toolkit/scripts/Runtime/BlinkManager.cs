using System.Collections;
using System.Collections.Generic;
using UnityEngine;



#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(BlinkManager))]
public class BlinkManagerEditor : Editor
{
    SerializedProperty m_interval;

    protected void OnEnable()
    {
        m_interval = serializedObject.FindProperty("interval");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        using (new EditorGUI.DisabledGroupScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        DrawPropertiesExcluding(serializedObject, "m_Script", "interval");

        Vector2 temp = m_interval.vector2Value;
        EditorGUI.BeginChangeCheck();
        {
            EditorGUILayout.MinMaxSlider("Blink Interval Range", ref temp.x, ref temp.y, .1f, 10f);

            using (new EditorGUI.IndentLevelScope())
            {
                temp.x = EditorGUILayout.FloatField("Min:", temp.x);
                temp.y = EditorGUILayout.FloatField("Max:", temp.y);
            }
        }
        if (EditorGUI.EndChangeCheck())
            m_interval.vector2Value = temp;

        


        serializedObject.ApplyModifiedProperties();
    }
}
#endif



public class BlinkManager : MonoBehaviour
{
    [SerializeField] Animator animator;
    public string blinkState;

    [SerializeField] Vector2 interval;

    public int blinkLayer => animator.GetLayerIndex("blink");


    void OnEnable()
    {
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while(true)
        {
            float rand = Random.Range(interval.x, interval.y);
            yield return new WaitForSeconds(rand);

            animator.Play(blinkState, blinkLayer, 0f);
        }
    }
}
