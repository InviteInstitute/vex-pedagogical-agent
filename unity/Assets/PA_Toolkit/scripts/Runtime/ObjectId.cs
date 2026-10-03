using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{
    public class ObjectId : MonoBehaviour
    {
        [SerializeField] string _id;
        public string id => _id;
    }
}
