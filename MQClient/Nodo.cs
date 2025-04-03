using System;

namespace MQClient
{
    /// <summary>
    /// Representa un nodo en una estructura de lista enlazada simple.
    /// Utilizado como unidad encolada dentro del sistema de mensajería.
    /// </summary>
    public class Nodo
    {
        /// <summary>
        /// Contenido de datos del nodo (mensaje).
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Referencia al siguiente nodo en la lista.
        /// </summary>
        public Nodo SiguienteNodo { get; set; }

        /// <summary>
        /// Crea un nuevo nodo con el contenido especificado.
        /// </summary>
        /// <param name="data">Datos que contiene el nodo.</param>
        public Nodo(string data)
        {
            Data = data;
            SiguienteNodo = null;
        }
    }
}
