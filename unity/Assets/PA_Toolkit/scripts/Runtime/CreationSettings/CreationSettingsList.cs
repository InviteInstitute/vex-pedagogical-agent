using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PA_Toolkit
{
    [CreateAssetMenu(fileName = "settingsList_name", menuName = "Data/Settings List")]
    public class CreationSettingsList : ScriptableObject
    {
        [System.Serializable]
        public class Item
        {
            [SerializeField] bool _isLocked;
            public bool isLocked => _isLocked;

            [SerializeField] CreationSetting _setting;
            public CreationSetting setting => _setting;
        }

        [SerializeField] List<Item> _items = new List<Item>();
        public List<Item> items => _items;

        public CreationSetting GetCreationSettingFromName(string settingName)
        {
            foreach (var item in _items)
            {
                if (item.setting.name.Equals(settingName)) return item.setting;
            }
            return null;
        }

    }
}