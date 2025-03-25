using System;

namespace MQClient
{

    public class Queue 
    {
        private Nodo _inicio;
        private int count;

        public Queue()
        {
            _inicio = null;
            count = 0;
        }

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



        private Nodo BuscarUltimo(Nodo unNodo)
        {
            return (unNodo.SiguienteNodo == null) ? unNodo : BuscarUltimo(unNodo.SiguienteNodo);
        }

        

        public bool IsEmpty()
        {
            return count == 0;
        }

        public int Count()
        {
            return count;
        }

        public Nodo Inicio
        {
            get { return _inicio; }
        }
    }
}
