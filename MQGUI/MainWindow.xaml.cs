using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using MQClient;

namespace MQGUI
{
    public partial class MainWindow : Window
    {
        private Client _client;
        private HashSet<Topic> _subscribedTopics = new HashSet<Topic>();

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += (s, e) => _client?.Dispose(); // El cliente ahora puede ser null
            btnSubscribe.IsEnabled = false;
            btnUnsubscribe.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
            btnReceiveMessage.IsEnabled = false;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtBrokerIP.Text.Trim();
            string portText = txtBrokerPort.Text.Trim();

            // Validación de IP
            if (!IPAddress.TryParse(ip, out _))
            {
                lstMessages.Items.Add("❌ IP inválida.");
                return;
            }

            // Validación de puerto
            if (!int.TryParse(portText, out int port) || port != 5000)
            {
                lstMessages.Items.Add("❌ El puerto debe ser 5000.");
                return;
            }

            try
            {
                string appId = Guid.NewGuid().ToString();
                _client?.Dispose(); // Cierra cualquier instancia anterior
                _client = new Client(appId, ip, port);

                txtAppID.Text = appId;
                txtStatus.Text = $"Estado: Conectado a {ip}:{port}";
                lstMessages.Items.Add($"✅ Conectado con App ID: {appId}");
                btnSubscribe.IsEnabled = true;
                btnUnsubscribe.IsEnabled = true;
                btnSendMessage.IsEnabled = true;
                btnReceiveMessage.IsEnabled = true;

                // Desactivar botón de conexión para evitar reconectar
                btnConnect.IsEnabled = false;
            }
            catch (Exception ex)
            {
                lstMessages.Items.Add($"❌ Error al conectar: {ex.Message}");
            }
        }
        private async void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            string topicName = txtTopic.Text.Trim();
            if (!string.IsNullOrEmpty(topicName))
            {
                Topic topic = new Topic(topicName);

                if (!_subscribedTopics.Contains(topic))
                {
                    lstMessages.Items.Add($"⌛ Suscribiendo a {topic}...");
                    bool success = await _client.Subscribe(topic);

                    if (success)
                    {
                        _subscribedTopics.Add(topic);
                        lstMessages.Items.Add($"✅ Suscrito a {topic}");
                        lstTopics.Items.Add(topic.Name); // Agregar nombre del tema a la lista de suscritos
                        if (lstTopics.SelectedItem == null)
                            lstTopics.SelectedItem = topic.Name; // Seleccionar automáticamente el primer tema
                        UpdateStatus();
                    }
                    else
                    {
                        lstMessages.Items.Add("❌ Error al suscribirse");
                    }
                }
            }
        }

        private async void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (lstTopics.SelectedItem is string topicName)
            {
                Topic topic = new Topic(topicName);
                lstMessages.Items.Add($"⌛ Desuscribiendo de {topic}...");
                bool success = await _client.Unsubscribe(topic);

                if (success)
                {
                    _subscribedTopics.Remove(topic);
                    lstMessages.Items.Add($"✅ Desuscrito de {topic}");
                    lstTopics.Items.Remove(topic.Name); // Quitar tema de la lista

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
            Topic topic = GetTopicForMessage();
            if (topic != null)
            {
                string messageText = txtMessage.Text.Trim();
                if (!string.IsNullOrEmpty(messageText))
                {
                    Message message = new Message(messageText);

                    lstMessages.Items.Add($"✉️ Enviando a {topic}: {message}");

                    bool success = await _client.Publish(message, topic);
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
            Topic topic = GetSelectedTopic();
            if (topic != null)
            {
                lstMessages.Items.Add($"🔍 Buscando mensajes en {topic}...");
                Message receivedMessage = await _client.Receive(topic);

                if (receivedMessage != null)
                    lstMessages.Items.Add($"📩 Recibido en {topic}: {receivedMessage}");
                else
                    lstMessages.Items.Add($"📭 No hay mensajes nuevos en {topic}");
            }
        }

        private Topic GetTopicForMessage()
        {
            // Si el usuario ha escrito un tema en txtTopic, usarlo aunque haya selección en la lista
            string typedTopic = txtTopic.Text.Trim();
            if (!string.IsNullOrEmpty(typedTopic))
                return new Topic(typedTopic);

            // Si no hay texto en txtTopic, usar el tema seleccionado en la lista
            if (lstTopics.SelectedItem is string selectedTopic)
                return new Topic(selectedTopic);

            // Si no hay nada, mostrar advertencia
            MessageBox.Show("Escribe un tema o selecciona uno antes de enviar un mensaje.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        private Topic GetSelectedTopic()
        {
            if (lstTopics.SelectedItem is string topicName)
                return new Topic(topicName);

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
