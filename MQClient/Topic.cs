using System;

namespace MQClient
{
    /// <summary>
    /// Representa un tópico (tema) al cual los clientes pueden suscribirse, publicar o recibir mensajes.
    /// </summary>
    public class Topic
    {
        /// <summary>
        /// Nombre del tópico.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Crea un nuevo tópico con el nombre especificado.
        /// </summary>
        /// <param name="name">Nombre del tópico.</param>
        /// <exception cref="ArgumentException">Se lanza si el nombre es nulo, vacío o contiene solo espacios.</exception>
        public Topic(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del tema no puede estar vacío.", nameof(name));

            Name = name;
        }

        /// <summary>
        /// Devuelve el nombre del tópico como cadena de texto.
        /// </summary>
        /// <returns>Nombre del tópico.</returns>
        public override string ToString() => Name;
    }
}
