using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PA_Toolkit
{
    [System.Serializable]
    public class ActionSettings
    {
        [System.Serializable]
        public class Dialogue
        {
            public string text;
            public AudioClip audio;
        }

        [SerializeField] string _name;
        public string name => _name;

        [SerializeField] Dialogue _dialogue;
        public Dialogue dialogue => _dialogue;

        [SerializeField] Expression _expression;
        public Expression expression => _expression;

        [SerializeField] Gesture _gesture;
        public Gesture gesture => _gesture;
    }
}