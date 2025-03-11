using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQClient
{
    public class Nodo // Cambia 'class' por 'public class'
    {
        public string Data { get; set; }
        public Nodo SiguienteNodo { get; set; }

        public Nodo(string data)
        {
            Data = data;
            SiguienteNodo = null;
        }
    }
}
