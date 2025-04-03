using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using MQClient;

namespace MQGUI
{
    /// <summary>
    /// Ventana principal de la aplicación MQGUI.
    /// Permite conectar a un broker, suscribirse a tópicos, publicar y recibir mensajes.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Cliente que se conecta al servidor MQ.
        /// </summary>
        private Client _client;

        /// <summary>
        /// Conjunto de tópicos a los que el cliente está actualmente suscrito.
        /// </summary>
        private HashSet<Topic> _subscribedTopics = new HashSet<Topic>();

        /// <summary>
        /// Inicializa la ventana principal y los controles de la interfaz.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += (s, e) => _client?.Dispose();

            btnSubscribe.IsEnabled = false;
            btnUnsubscribe.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
            btnReceiveMessage.IsEnabled = false;
        }

        /// <summary>
        /// Evento que se ejecuta al hacer clic en el botón "Conectar".
        /// Realiza validaciones y establece la conexión con el broker.
        /// </summary>
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtBrokerIP.Text.Trim();
            string portText = txtBrokerPort.Text.Trim();

            if (!IPAddress.TryParse(ip, out _))
            {
                lstMessages.Items.Add("❌ IP inválida.");
                return;
            }

            if (!int.TryParse(portText, out int port) || port != 5000)
            {
                lstMessages.Items.Add("❌ El puerto debe ser 5000.");
                return;
            }

            try
            {
                string appId = Guid.NewGuid().ToString();
                _client?.Dispose();
                _client = new Client(appId, ip, port);

                txtAppID.Text = appId;
                txtStatus.Text = $"Estado: Conectado a {ip}:{port}";
                lstMessages.Items.Add($"✅ Conectado con App ID: {appId}");

                btnSubscribe.IsEnabled = true;
                btnUnsubscribe.IsEnabled = true;
                btnSendMessage.IsEnabled = true;
                btnReceiveMessage.IsEnabled = true;
                btnConnect.IsEnabled = false;
            }
            catch (Exception ex)
            {
                lstMessages.Items.Add($"❌ Error al conectar: {ex.Message}");
            }
        }

        /// <summary>
        /// Evento que se ejecuta al hacer clic en el botón "Suscribirse".
        /// Suscribe al cliente a un tópico especificado.
        /// </summary>
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
                        lstTopics.Items.Add(topic.Name);

                        if (lstTopics.SelectedItem == null)
                            lstTopics.SelectedItem = topic.Name;

                        UpdateStatus();
                    }
                    else
                    {
                        lstMessages.Items.Add("❌ Error al suscribirse");
                    }
                }
            }
        }

        /// <summary>
        /// Evento que se ejecuta al hacer clic en "Desuscribirse".
        /// Elimina la suscripción del cliente al tópico seleccionado.
        /// </summary>
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
                    lstTopics.Items.Remove(topic.Name);

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

        /// <summary>
        /// Evento que se ejecuta al hacer clic en "Enviar".
        /// Publica un mensaje en el tópico indicado.
        /// </summary>
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

        /// <summary>
        /// Evento que se ejecuta al hacer clic en "Recibir".
        /// Solicita un mensaje del tópico seleccionado.
        /// </summary>
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

        /// <summary>
        /// Determina el tópico para publicar el mensaje.
        /// Prioriza el texto ingresado si existe, o usa el tópico seleccionado.
        /// </summary>
        private Topic GetTopicForMessage()
        {
            string typedTopic = txtTopic.Text.Trim();
            if (!string.IsNullOrEmpty(typedTopic))
                return new Topic(typedTopic);

            if (lstTopics.SelectedItem is string selectedTopic)
                return new Topic(selectedTopic);

            MessageBox.Show("Escribe un tema o selecciona uno antes de enviar un mensaje.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        /// <summary>
        /// Obtiene el tópico actualmente seleccionado en la lista.
        /// </summary>
        private Topic GetSelectedTopic()
        {
            if (lstTopics.SelectedItem is string topicName)
                return new Topic(topicName);

            MessageBox.Show("Selecciona un tema antes de recibir mensajes.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        /// <summary>
        /// Actualiza el estado mostrado en la interfaz indicando los tópicos suscritos.
        /// </summary>
        private void UpdateStatus()
        {
            txtStatus.Text = _subscribedTopics.Count > 0
                ? $"Estado: Conectado | Suscrito a {string.Join(", ", _subscribedTopics)}"
                : "Estado: Conectado | Sin suscripciones";
        }
    }
}
