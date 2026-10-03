using System.Text.RegularExpressions;

namespace SerializableClasses
{
    [System.Serializable]
    public class SerializeableDictionary_StringKeys<TValue> : SerializableDictionary<string, TValue>
    {
        public SerializeableDictionary_StringKeys() : this(TryRename, Validate) { }
        public SerializeableDictionary_StringKeys(TryRenameDelegate onTryRename, ValidateDelegate onValidate) : base(onTryRename, onValidate){}

        const string REGEX = "^.*?\\(([0-9]+)\\)$";  //looking for something like "myKey (1)"


        static string Validate(string key)
        {
            return key == string.Empty ? "key" : key;
        }


        /// <summary>
        /// Attempt to rename a key by adding an incrementing suffix in parentheses ("myKey" => "myKey (1)", "myKey (5)" => "myKey (6)")
        /// </summary>
        /// <param name="key">The key to rename</param>
        /// <param name="result">The new key</param>
        /// <returns>True, if successful</returns>
        static bool TryRename(SerializableDictionary<string, TValue> dictionary, string key, out string result)
        {
            int number = 0;
            Match match = Regex.Match(key, REGEX);
            if(match != Match.Empty)
            {
                Group group = match.Groups[1];
                number = int.Parse(group.Value);
                key = key.Substring(0, group.Index - 1).TrimEnd();
            }

            do
            {
                result = $"{key} ({++number})";

            } while (dictionary.ContainsKey(result));

            return true;
        }


        /*bool TryRename(string key, out string result)
        {
            int number = 0;
            while (ContainsKey(key))
            {
                //if the suffix already exists
                Match match = Regex.Match(key, PATTERN);
                if (match != Match.Empty)
                {
                    Group group = match.Groups[1]; //the given regular expression stores the number in group 1
                    number = int.Parse(group.Value);
                    key = key.Substring(0, group.Index - 1).TrimEnd();
                }
                key += $" ({number + 1})";
            }
            result = key;
            return true;
        }*/
    }
}