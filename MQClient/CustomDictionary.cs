using System;
using System.Collections.Generic;

namespace MQClient
{
    /// <summary>
    /// Implementación personalizada de un diccionario utilizando una lista de pares clave-valor.
    /// Permite operaciones básicas como agregar, eliminar, buscar y obtener claves/valores.
    /// </summary>
    /// <typeparam name="TKey">Tipo de la clave.</typeparam>
    /// <typeparam name="TValue">Tipo del valor.</typeparam>
    public class CustomDictionary<TKey, TValue>
    {
        /// <summary>
        /// Lista interna que almacena los pares clave-valor.
        /// </summary>
        private List<KeyValuePair<TKey, TValue>> _items;

        /// <summary>
        /// Inicializa una nueva instancia del diccionario personalizado.
        /// </summary>
        public CustomDictionary()
        {
            _items = new List<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Agrega un nuevo par clave-valor al diccionario.
        /// </summary>
        /// <param name="key">Clave única.</param>
        /// <param name="value">Valor asociado.</param>
        /// <exception cref="ArgumentException">Se lanza si la clave ya existe.</exception>
        public void Add(TKey key, TValue value)
        {
            if (ContainsKey(key))
                throw new ArgumentException("La clave ya existe en el diccionario.");

            _items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Elimina un par clave-valor del diccionario según la clave.
        /// </summary>
        /// <param name="key">Clave del elemento a eliminar.</param>
        /// <returns>True si se eliminó; False si no se encontró.</returns>
        public bool Remove(TKey key)
        {
            int index = _items.FindIndex(kvp => kvp.Key.Equals(key));
            if (index == -1) return false;

            _items.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Intenta obtener el valor asociado a una clave.
        /// </summary>
        /// <param name="key">Clave a buscar.</param>
        /// <param name="value">Valor resultante, si existe.</param>
        /// <returns>True si se encontró; False si no.</returns>
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

        /// <summary>
        /// Verifica si una clave existe en el diccionario.
        /// </summary>
        /// <param name="key">Clave a verificar.</param>
        /// <returns>True si existe; False si no.</returns>
        public bool ContainsKey(TKey key)
        {
            return _items.Exists(kvp => kvp.Key.Equals(key));
        }

        /// <summary>
        /// Obtiene una colección enumerable de todas las claves.
        /// </summary>
        public IEnumerable<TKey> Keys => _items.ConvertAll(kvp => kvp.Key);

        /// <summary>
        /// Obtiene una colección enumerable de todos los valores.
        /// </summary>
        public IEnumerable<TValue> Values => _items.ConvertAll(kvp => kvp.Value);
    }
}
