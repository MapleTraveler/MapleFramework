using System;
using System.Collections.Generic;
using UnityEngine;

namespace Maple.Core
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new();
        [SerializeField] private List<TValue> values = new();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kv in this)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            int count = Math.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++)
                this[keys[i]] = values[i];

            if (keys.Count != values.Count)
                Debug.LogError("[SerializableDictionary] keys/values count mismatch after deserialization.");
        }

        public List<TKey> FindDuplicateKeys()
        {
            var seen = new HashSet<TKey>();
            var duplicates = new List<TKey>();
            for (int i = 0; i < keys.Count; i++)
            {
                if (!seen.Add(keys[i]))
                    duplicates.Add(keys[i]);
            }
            return duplicates;
        }

        public int MakeKeysUniqueIfPossible()
        {
            int fixedCount = 0;
            var used = new HashSet<TKey>();

            for (int i = 0; i < keys.Count; i++)
            {
                if (used.Add(keys[i])) continue;

                if (keys[i] is string s)
                {
                    string baseName = string.IsNullOrEmpty(s) ? "Key" : s;
                    string name = baseName;
                    int idx = 1;
                    while (!used.Add((TKey)(object)name))
                        name = $"{baseName} ({idx++})";
                    keys[i] = (TKey)(object)name;
                    fixedCount++;
                }
            }

            return fixedCount;
        }
    }
}
