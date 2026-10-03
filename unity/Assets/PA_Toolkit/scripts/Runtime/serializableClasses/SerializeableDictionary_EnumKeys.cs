

namespace SerializableClasses
{
    [System.Serializable]
    public class SerializeableDictionary_EnumKeys<TKey, TValue> : SerializableDictionary<TKey, TValue> where TKey : System.Enum
    {
        public SerializeableDictionary_EnumKeys() : this(TryRename) { }
        public SerializeableDictionary_EnumKeys(TryRenameDelegate tryRenameMethod) : base(tryRenameMethod) { }



        /// <summary>
        /// Attempts to renames a key by cycling through all enum values and applying one that isn't currently in use.
        /// </summary>
        /// <param name="key">The key to rename</param>
        /// <param name="result">The new key, or default if it failed</param>
        /// <returns>True, if successful</returns>
        static bool TryRename(SerializableDictionary<TKey, TValue> dictionary, TKey key, out TKey result)
        {
            TKey[] keys = (TKey[])System.Enum.GetValues(typeof(TKey));
            foreach (TKey k in keys)
            {
                if (!dictionary.ContainsKey(k))
                {
                    result = k;
                    return true;
                }
            }

            result = default(TKey);
            return false;
        }
    }
}