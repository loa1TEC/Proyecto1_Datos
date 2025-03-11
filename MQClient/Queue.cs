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
            if (_inicio == null)
            {
                _inicio = unNodo;
            }
            else
            {
                Nodo aux = BuscarUltimo(_inicio);
                aux.SiguienteNodo = unNodo;
            }
            count++;
        }

        private Nodo BuscarUltimo(Nodo unNodo)
        {
            return (unNodo.SiguienteNodo == null) ? unNodo : BuscarUltimo(unNodo.SiguienteNodo);
        }

        public Nodo Dequeue()
        {
            if (IsEmpty())
            {
                throw new InvalidOperationException("La cola está vacía");
            }

            Nodo nodoEliminado = _inicio;
            _inicio = _inicio.SiguienteNodo;
            count--;
            return nodoEliminado;
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
