using System;
using System.Windows;
using MQClient;

namespace MQGUI
{
    public partial class MainWindow : Window
    {
        private Client _client;
        private string _currentTopic = "";

        public MainWindow()
        {
            InitializeComponent();
            string appId = Guid.NewGuid().ToString();
            _client = new Client(appId);
            this.Closed += (s, e) => _client.Dispose();
        }

        private async void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            _currentTopic = txtTopic.Text;
            if (!string.IsNullOrEmpty(_currentTopic))
            {
                lstMessages.Items.Add($"⌛ Suscribiendo a {_currentTopic}...");
                bool success = await _client.Subscribe(_currentTopic);

                if (success)
                {
                    lstMessages.Items.Add($"✅ Suscrito a {_currentTopic}");
                    btnSubscribe.Visibility = Visibility.Collapsed;
                    btnUnsubscribe.Visibility = Visibility.Visible;
                    UpdateStatus($"Conectado | Tema: {_currentTopic}");
                }
                else
                {
                    lstMessages.Items.Add("❌ Error al suscribirse");
                }
            }
        }

        private async void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentTopic))
            {
                lstMessages.Items.Add($"⌛ Desuscribiendo de {_currentTopic}...");
                bool success = await _client.Unsubscribe(_currentTopic);

                if (success)
                {
                    lstMessages.Items.Add($"✅ Desuscrito de {_currentTopic}");
                    btnUnsubscribe.Visibility = Visibility.Collapsed;
                    btnSubscribe.Visibility = Visibility.Visible;
                    UpdateStatus("Conectado | Sin tema");
                    _currentTopic = "";
                }
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentTopic))
            {
                string message = txtMessage.Text;
                lstMessages.Items.Add($"✉️ Enviando a {_currentTopic}: {message}");

                bool success = await _client.Publish(_currentTopic, message);
                if (success)
                {
                    lstMessages.Items.Add("✔️ Mensaje enviado");
                    txtMessage.Clear();
                }
            }
        }

        private async void ReceiveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentTopic))
            {
                lstMessages.Items.Add($"🔍 Buscando mensajes en {_currentTopic}...");
                string message = await _client.Receive(_currentTopic);

                if (message != "EMPTY" && !message.StartsWith("ERROR"))
                    lstMessages.Items.Add($"📩 Recibido: {message}");
                else
                    lstMessages.Items.Add("📭 No hay mensajes nuevos");
            }
        }

        private void UpdateStatus(string status)
        {
            txtStatus.Text = $"Estado: {status}";
        }
    }
}