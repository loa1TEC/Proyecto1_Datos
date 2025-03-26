using System;
using System.Collections.Generic;

namespace MQClient
{
    public class CustomDictionary<TKey, TValue>
    {
        private List<KeyValuePair<TKey, TValue>> _items;

        public CustomDictionary()
        {
            _items = new List<KeyValuePair<TKey, TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
                throw new ArgumentException("La clave ya existe en el diccionario.");

            _items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool Remove(TKey key)
        {
            int index = _items.FindIndex(kvp => kvp.Key.Equals(key));
            if (index == -1) return false;

            _items.RemoveAt(index);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            foreach (var kvp in _items)
            {
                if (kvp.Key.Equals(key))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return _items.Exists(kvp => kvp.Key.Equals(key));
        }

        public IEnumerable<TKey> Keys => _items.ConvertAll(kvp => kvp.Key);
        public IEnumerable<TValue> Values => _items.ConvertAll(kvp => kvp.Value);
    }
}
