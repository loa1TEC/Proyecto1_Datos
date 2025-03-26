using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQClient
{
    public class Message
    {
        public string Content { get; }

        public Message(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("El mensaje no puede estar vacío.", nameof(content));

            Content = content;
        }

        public byte[] Serialize() => Encoding.UTF8.GetBytes(Content);
        public static Message Deserialize(byte[] data) => new Message(Encoding.UTF8.GetString(data));

        public override string ToString() => Content;
    }
}
