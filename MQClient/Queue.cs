using System;

namespace MQClient
{
    /// <summary>
    /// Representa una cola simple de nodos utilizada para almacenar mensajes.
    /// Implementación personalizada con operaciones básicas de encolado y desencolado.
    /// </summary>
    public class Queue
    {
        /// <summary>
        /// Referencia al primer nodo de la cola.
        /// </summary>
        private Nodo _inicio;

        /// <summary>
        /// Contador de elementos en la cola.
        /// </summary>
        private int count;

        /// <summary>
        /// Inicializa una nueva instancia de la cola.
        /// </summary>
        public Queue()
        {
            _inicio = null;
            count = 0;
        }

        /// <summary>
        /// Encola un nuevo nodo al final de la cola.
        /// </summary>
        /// <param name="unNodo">Nodo a encolar.</param>
        public void Enqueue(Nodo unNodo)
        {
            Console.WriteLine($"\n[COLA] Encolando mensaje: {unNodo.Data}");
            if (_inicio == null)
            {
                Console.WriteLine("Primer mensaje en cola");
                _inicio = unNodo;
            }
            else
            {
                Console.WriteLine($"Agregando mensaje al final de la cola (Total: {count})");
                Nodo aux = BuscarUltimo(_inicio);
                aux.SiguienteNodo = unNodo;
            }
            count++;
            Console.WriteLine($"Mensaje encolado. Tamaño cola: {count}");
        }

        /// <summary>
        /// Desencola el primer nodo de la cola.
        /// </summary>
        /// <returns>El nodo desencolado.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si la cola está vacía.</exception>
        public Nodo Dequeue()
        {
            if (IsEmpty())
            {
                Console.WriteLine("Intento de desencolar cola vacía");
                throw new InvalidOperationException("La cola está vacía");
            }

            Nodo nodoEliminado = _inicio;
            _inicio = _inicio.SiguienteNodo;
            count--;

            Console.WriteLine($"\n[COLA] Desencolando mensaje: {nodoEliminado.Data}");
            Console.WriteLine($"Mensaje desencolado. Tamaño cola: {count}");

            return nodoEliminado;
        }

        /// <summary>
        /// Busca el último nodo en la cola de manera recursiva.
        /// </summary>
        /// <param name="unNodo">Nodo desde el cual comenzar la búsqueda.</param>
        /// <returns>Último nodo de la cola.</returns>
        private Nodo BuscarUltimo(Nodo unNodo)
        {
            return (unNodo.SiguienteNodo == null) ? unNodo : BuscarUltimo(unNodo.SiguienteNodo);
        }

        /// <summary>
        /// Verifica si la cola está vacía.
        /// </summary>
        /// <returns>True si está vacía; False si contiene elementos.</returns>
        public bool IsEmpty()
        {
            return count == 0;
        }

        /// <summary>
        /// Obtiene la cantidad actual de elementos en la cola.
        /// </summary>
        /// <returns>Número de elementos en la cola.</returns>
        public int Count()
        {
            return count;
        }

        /// <summary>
        /// Propiedad de solo lectura para acceder al primer nodo de la cola.
        /// </summary>
        public Nodo Inicio
        {
            get { return _inicio; }
        }
    }
}
