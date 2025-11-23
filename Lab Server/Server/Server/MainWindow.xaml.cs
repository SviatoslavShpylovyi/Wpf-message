using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace ServerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Server server;
        private bool isRunning;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void StartButton(object sender, RoutedEventArgs e)
         { if (!isRunning)
            {

                int num;
                if (!(int.TryParse(Port.Text, out num)))
                {
                    string time = DateTime.Now.ToString("HH:mm");
                    Logs.Items.Add($"[{time}] No port");
                    return;
                };
                server = new Server(Log);
                ClientList.ItemsSource = server.Clients;
                server.port = num;
                server.ServerName = NameField.Text;
                server.password = Password.Password;
                try
                {
                    server.ip = IPAddress.Parse(Address.Text);

                }
                catch
                {
                    server.ip = null;
                }
                server.Start();
                Log("Server sterted");
                isRunning = true;
                Baton.Content = "Stop";
            }
            else
            {
                isRunning = false;
                server.Stop();
                Baton.Content = "Start";
            }
        }
        public void Log(string message)
        {
            string time = DateTime.Now.ToString("HH:mm");
            Dispatcher.Invoke(() =>
            {
                Logs.Items.Add($"[{time}] {message}");
            });
        }

        private void Kick(object sender, RoutedEventArgs e)
        {
            if(ClientList.SelectedItems.Count== 0) {
                return;
            }
            var kickers = ClientList.SelectedItems.Cast<ClientHandler>().ToList();
            foreach (var client in kickers)
            {
                client.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    server.clients.Remove(client);
                });
                Log($"Kicked user: {client.UserName}");
            }

        }

        private void SendAdminMessage(object sender, RoutedEventArgs e)
        {
            foreach(var client in server.Clients)
            {
                server.BroadCast($"{server.ServerName}:{AdminText.Text} ");
                server.Log($"Admin '{server.ServerName}' {AdminText.Text}");
            }
                AdminText.Text = " ";
        }
       
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (server != null) { server.Stop(); }
        }

        private void ShiftEneterd(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                AdminText.Text += "\n";
                return;
            }
            else if(e.Key == Key.Enter)
                {
                server.Log($"Admin '{server.ServerName}' {AdminText.Text}");
                foreach (var client in server.Clients)
                {
                    server.BroadCast($"{server.ServerName}:{AdminText.Text} ");
                }
                AdminText.Text = " ";
                return;
            }
        }

        private void Closing_event(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }

    public class Server
    {
        public string password;
        public bool isGoing;
        public int port;
        public IPAddress? ip;
        private TcpListener _listener;
        public Action<string> Log;
        public string ServerName = "Admin";
        public ObservableCollection<ClientHandler> clients = new ObservableCollection<ClientHandler>();
        public ObservableCollection<ClientHandler> Clients => clients;
        public Server(Action<string> log)
        {
            Log = log;  
        }
        public void Start()
        {


                if (ip != null)
                {
                    _listener = new TcpListener(ip, port);
                }
                else
                {

                    _listener = new TcpListener(IPAddress.Any, port);
                }
                _listener.Start();
                isGoing = true;
            var endpoint = (IPEndPoint)_listener.LocalEndpoint;
            Log($"Listening on {endpoint.Address}:{endpoint.Port}");
            new Thread(() =>
            {
                
                while (isGoing)
                {
                    try
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        ClientHandler handler;
                        Log("Accepting new client");
                        Thread.Sleep(500);
                        try
                        {
                            handler = new ClientHandler(client, this);
                            new Thread(handler.Start).Start();
                        }
                        catch (Exception ex)
                        {
                            Log("Failed to create ClientHandler: " + ex.Message);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error accepting client: {ex.Message}");
                        continue;
                    }
                }
            }).Start();
        }

        public void HandleAction(string Action, ClientHandler sender)
        {
            try
            {
                if(Action == "DISCONNECT")
                {
                    sender.Stop();
                }
                string[] text = Action.Split('|');
                if (text.Length == 3) {
                    Log($"Client '{text[0]}': {text[1]}");
                    BroadCast($"{text[0]}:{text[1]}:{text[2]}", sender);
                }
                else
                {
                    Log($"WEird Message");
                }

            }
            catch (Exception ex)
            {
                Log($"{ex.Message}");
            }
        }
        public void BroadCast(string message, ClientHandler sender=null)
        {
            foreach (ClientHandler handler in clients)
            {
                if (sender != handler)
                {

                handler.Send(message);
                }
            }
        }
        public void Stop()
        {
            isGoing = false;
            foreach(ClientHandler handler in clients)
            {
                handler.Stop();
            }
            _listener.Stop();
            Log("server stopped ");
        }
    }

    public class ClientHandler
    {
        public TcpClient Client;
        public NetworkStream Stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Server server;
        public string UserName;
        public ClientHandler(TcpClient client, Server server)
        {
            Client = client;
            Stream = Client.GetStream();
            this.server = server;
            reader = new StreamReader(Stream);
            writer = new StreamWriter(Stream);
            writer.AutoFlush = true;
        }
        public void Start()
        {
            try
            {
                string firstLine = reader.ReadLine();
                if (firstLine == null)
                {
                    Client.Close();
                    return;
                }
                string[] output = firstLine.Split(':');
               if (output.Length == 2) {
                    if(server.password!= output[1])
                    {
                        writer.WriteLine("Pass");
                        server.Log($"User {UserName} is with Wrong pass");
                        Thread.Sleep(200);
                        Client.Close();
                        return;
                    }
                    UserName = output[0];
                    server.Log($"User {output[0]} is connected");
                    writer.WriteLine("OK");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        server.clients.Add(this);
                    });
                    server.BroadCast($"C{output[0]}", this);
                }

            }
            catch (Exception ex)
            {
                server.Log(ex.Message);
                Client.Close();
            }
            try
            {
                while (true)
                {
                    string json = reader.ReadLine();
                    if (json != null)
                    {
                        server.HandleAction(json, this);
                    }
                }
            }
            catch (Exception ex)
            {
                server.Log($" Client '{UserName}' Disconnected");
                Application.Current.Dispatcher.Invoke(() => server.Clients.Remove(this));
                Client.Close();
            }
        }
        public void Send(string message)
        {
            try
            {
                writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                server.Log( $"Error on send{ex.Message}");
            }
        }
        public void Stop()
        {
            try
            {
                writer.WriteLine("DISCONNECT");
                Thread.Sleep(200);
                writer?.Close();
                reader?.Close();
                Stream?.Close();
                Client?.Close();
            }
            catch (Exception ex)
            {
                server.Log("Error stopping client: " + ex.Message);
            }
        }
        public override string ToString()
        {
            return UserName; 
        }
    }
    
    //Message for the server 
    

}