using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_Toolkit
{

    [CreateAssetMenu(fileName = "preset_name", menuName = "Data/Preset")]
    public class Preset : CreationSetting
    {
        [SerializeField] AgentSettings _settings;
        public AgentSettings settings => _settings;
    }
}