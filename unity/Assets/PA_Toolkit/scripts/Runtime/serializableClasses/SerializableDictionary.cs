using System.Collections.Generic;
using UnityEngine;



namespace SerializableClasses
{
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        public delegate bool TryRenameDelegate(SerializableDictionary<TKey,TValue> dictionary, TKey key, out TKey result);
        protected TryRenameDelegate onTryRename;

        public delegate TKey ValidateDelegate(TKey key);
        protected ValidateDelegate onValidate;


        [SerializeField] List<SerializableKeyValuePair<TKey, TValue>> _keyValuePairs = new List<SerializableKeyValuePair<TKey, TValue>>();
        protected virtual List<SerializableKeyValuePair<TKey, TValue>> keyValuePairs
        { 
            set => _keyValuePairs = value; 
            get => _keyValuePairs; 
        }


        public SerializableDictionary() : this(null, null) { }
        public SerializableDictionary(TryRenameDelegate onTryRename) : this(onTryRename, null) { }
        public SerializableDictionary(TryRenameDelegate onTryRename, ValidateDelegate onValidate) : base()
        {
            this.onTryRename = onTryRename;
            this.onValidate = onValidate;
        }


        public virtual void OnBeforeSerialize()
        {
            keyValuePairs.Clear();
            foreach(KeyValuePair<TKey, TValue> kvp in this)
                keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
        }
        public virtual void OnAfterDeserialize()
        {
            Clear();
            foreach (SerializableKeyValuePair<TKey, TValue> pair in keyValuePairs)
            {
                if (onValidate != null)
                    pair.key = onValidate(pair.key);

                if(ContainsKey(pair.key) && onTryRename != null && onTryRename.Invoke(this, pair.key, out TKey newKey))
                    pair.key = newKey;

                if (!ContainsKey(pair.key))
                    Add(pair.key, pair.value);
            }
        }
    }

    [System.Serializable]
    public class SerializableKeyValuePair<TKey, TValue>
    {
        public TKey key;
        public TValue value;

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }
}