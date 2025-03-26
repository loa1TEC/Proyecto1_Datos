using System;
using System.Collections.Generic;
using System.Windows;
using MQClient;

namespace MQGUI
{
    public partial class MainWindow : Window
    {
        private Client _client;
        private HashSet<string> _subscribedTopics = new HashSet<string>(); // Lista de temas suscritos

        public MainWindow()
        {
            InitializeComponent();
            string appId = Guid.NewGuid().ToString();
            _client = new Client(appId);
            this.Closed += (s, e) => _client.Dispose();
        }

        private async void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            string topic = txtTopic.Text.Trim();
            if (!string.IsNullOrEmpty(topic) && !_subscribedTopics.Contains(topic))
            {
                lstMessages.Items.Add($"⌛ Suscribiendo a {topic}...");
                bool success = await _client.Subscribe(topic);

                if (success)
                {
                    _subscribedTopics.Add(topic);
                    lstMessages.Items.Add($"✅ Suscrito a {topic}");
                    lstTopics.Items.Add(topic); // Agregar tema a la lista de suscritos
                    if (lstTopics.SelectedItem == null)
                        lstTopics.SelectedItem = topic; // Seleccionar automáticamente el primer tema
                    UpdateStatus();
                }
                else
                {
                    lstMessages.Items.Add("❌ Error al suscribirse");
                }
            }
        }

        private async void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (lstTopics.SelectedItem is string topic)
            {
                lstMessages.Items.Add($"⌛ Desuscribiendo de {topic}...");
                bool success = await _client.Unsubscribe(topic);

                if (success)
                {
                    _subscribedTopics.Remove(topic);
                    lstMessages.Items.Add($"✅ Desuscrito de {topic}");
                    lstTopics.Items.Remove(topic); // Quitar tema de la lista

                    // Si hay más temas en la lista, seleccionar otro automáticamente
                    if (lstTopics.Items.Count > 0)
                        lstTopics.SelectedIndex = 0;
                    else
                        lstTopics.SelectedItem = null;

                    UpdateStatus();
                }
            }
            else
            {
                MessageBox.Show("Selecciona un tema antes de desuscribirte.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string topic = GetTopicForMessage();
            if (topic != null)
            {
                string message = txtMessage.Text.Trim();
                if (!string.IsNullOrEmpty(message))
                {
                    lstMessages.Items.Add($"✉️ Enviando a {topic}: {message}");

                    bool success = await _client.Publish(topic, message);
                    if (success)
                    {
                        lstMessages.Items.Add("✔️ Mensaje enviado");
                        txtMessage.Clear();
                    }
                }
                else
                {
                    MessageBox.Show("No puedes enviar un mensaje vacío.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void ReceiveMessage_Click(object sender, RoutedEventArgs e)
        {
            string topic = GetSelectedTopic();
            if (topic != null)
            {
                lstMessages.Items.Add($"🔍 Buscando mensajes en {topic}...");
                string message = await _client.Receive(topic);

                if (message != "EMPTY" && !message.StartsWith("ERROR"))
                    lstMessages.Items.Add($"📩 Recibido en {topic}: {message}");
                else
                    lstMessages.Items.Add($"📭 No hay mensajes nuevos en {topic}");
            }
        }

        private string GetTopicForMessage()
        {
            // Si el usuario ha escrito un tema en txtTopic, usarlo aunque haya selección en la lista
            string typedTopic = txtTopic.Text.Trim();
            if (!string.IsNullOrEmpty(typedTopic))
                return typedTopic;

            // Si no hay texto en txtTopic, usar el tema seleccionado en la lista
            if (lstTopics.SelectedItem is string selectedTopic)
                return selectedTopic;

            // Si no hay nada, mostrar advertencia
            MessageBox.Show("Escribe un tema o selecciona uno antes de enviar un mensaje.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        private string GetSelectedTopic()
        {
            if (lstTopics.SelectedItem is string topic)
                return topic;

            MessageBox.Show("Selecciona un tema antes de recibir mensajes.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        private void UpdateStatus()
        {
            txtStatus.Text = _subscribedTopics.Count > 0
                ? $"Estado: Conectado | Suscrito a {string.Join(", ", _subscribedTopics)}"
                : "Estado: Conectado | Sin suscripciones";
        }
    }
}
