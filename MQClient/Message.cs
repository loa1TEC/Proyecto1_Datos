using System;
using System.Text;

namespace MQClient
{
    /// <summary>
    /// Representa un mensaje que puede enviarse o recibirse a través del sistema MQ.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Contenido del mensaje.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Inicializa un nuevo mensaje con el contenido especificado.
        /// </summary>
        /// <param name="content">Texto del mensaje.</param>
        /// <exception cref="ArgumentException">Se lanza si el contenido es nulo, vacío o solo espacios.</exception>
        public Message(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("El mensaje no puede estar vacío.", nameof(content));

            Content = content;
        }

        /// <summary>
        /// Devuelve el contenido del mensaje como string.
        /// </summary>
        /// <returns>Contenido del mensaje.</returns>
        public override string ToString() => Content;
    }
}
