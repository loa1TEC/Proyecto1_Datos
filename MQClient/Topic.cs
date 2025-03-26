using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQClient
{
    public class Topic
    {
        public string Name { get; }

        public Topic(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del tema no puede estar vacío.", nameof(name));

            Name = name;
        }

        public override string ToString() => Name;
    }
}
