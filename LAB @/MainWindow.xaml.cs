using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
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
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace LAB__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // private Chatter Chatter;
        private TcpClient TcpClient;
        private StreamReader reader;
        private StreamWriter writer;
        public string Name;

        public bool connected = false;
        int turn = 0;
        public MainWindow()
        {
            InitializeComponent();
            MessegeField.Text = " ";
            this.DataContext = "{Binding RelativeSource={RelativeSource Self}}";
            var message = new Message(new DateTime(2025, 3, 20, 10, 30, 0))
            {
                Username = "Admin",
                SenderId = 1,
                _Message = " Hello",
            };
            Chat.Items.Add(message);
            var message1 = new Message(new DateTime(2025, 5, 26, 13, 30, 0))
            {
                Username = "Administrator",
                SenderId = 0,
                _Message = " Hi",
            };
            Chat.Items.Add(message1);

        }

        private void Connect_Chatter(object sender, RoutedEventArgs e)
        {
            Window1 win = new Window1();
            win.Show();
        }
        public void ConnectMessage()
        {
            var message = new Message()
            {
                SenderId = -1,
                _Message = "Connected"
            };
            con_menu.IsEnabled = false;
            dis_menu.IsEnabled = true;
            Chat.Items.Add(message);
        }

        private void Disconnect_Chatter(object sender, RoutedEventArgs e)
        {
            this.connected = false;
            dis_menu.IsEnabled = false;
            var mess = new Message
            {
                SenderId = -1,
                _Message = "Disconnected"
            };
            Chat.Items.Add(mess);
            if (TcpClient != null)
            {
                writer.Write("DISCONNECT");
                writer.Close();
                reader.Close();
                TcpClient.Close();
            }
        }

        private void Exit_event(object sender, RoutedEventArgs e)
        {
            if (TcpClient != null)
            {
                writer.Write("DISCONNECT");
                writer.Close();
                reader.Close();
                TcpClient.Close();
            }
        }

        private void About_click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Standart message application");
        }

        private void Send_Message(object sender, RoutedEventArgs e)
        {
            //string name = "someone";
            string name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            /*switch (turn)
            {
                case 0:
                      name ="hermant";
                    break;
                case 1:
                    name =  System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    break;
                case -1:
                    name = "System";
                    break;
            }*/
            if (connected == false)
            {
                MessegeField.Text = " ";
                return;
            }
            var message = new Message
            {
                Username = Name,
                SenderId = 1,
                _Message = MessegeField.Text,
            };
            writer.WriteLine($"{Name}|{MessegeField.Text}|{DateTime.Now.ToString("HH:mm")}");
            Chat.Items.Add(message);
            //turn =  turn  == 0 ? 1 : 0;
            MessegeField.Text = " ";
        }
        public void StartMesseging(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;
            NetworkStream stream = TcpClient.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            Thread listen = new Thread(ListenToMessages);
            listen.Start();
        }
        public void ListenToMessages()
        {
            try
            {
                while (connected)
                {
                    string message = reader.ReadLine();
                    if (message == null)
                    {
                        continue;
                    }
                    else
                    {
                        if (message == "DISCONNECT")
                        {
                            MessageBox.Show("Disconnecting");
                            var mess = new Message
                            {
                                SenderId = -1,
                                _Message = "Disconnected"
                            };
                            Dispatcher.Invoke(() =>
                            {
                                this.connected = false;
                                dis_menu.IsEnabled = false;
                                Chat.Items.Add(mess);
                            });
                            writer?.Close();
                            TcpClient?.Close();
                            reader?.Close();
                            return;
                        }
                        if (message.StartsWith("C"))
                        {
                            string name = message.Substring(1);
                            var mess = new Message
                            {
                                SenderId = -1,
                                _Message = $"New User {name} Connected"
                            };
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                Chat.Items.Add(mess);
                            });
                            continue;
                        }
                        string[] facts = message.Split(':');
                        var messages = new Message()
                        {
                            Username = facts[0],
                            SenderId = 0,
                            _Message = facts[1]

                        };
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            Chat.Items.Add(messages);
                        });
                    }
                }
            }
            catch (Exception ex)
            { }

        }

        private void EnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                writer.WriteLine($"{Name}|{MessegeField.Text}|{DateTime.Now.ToString("HH:mm")}");
                var message = new Message
                {
                    Username = Name,
                    SenderId = 0,
                    _Message = MessegeField.Text,
                };
                Chat.Items.Add(message);
                MessegeField.Text = " ";
            }
        }

        private void ShiftEneterd(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                MessegeField.Text += "\n";
                return;
            }
            else if (e.Key == Key.Enter)
            {
                if (MessegeField.Text == " ")
                {
                    return;
                }
                string name = " ";

                switch (turn)
                {
                    case 0:
                        name = "hermant";
                        break;
                    case 1:
                        name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                        break;
                    case -1:
                        name = "System";
                        break;
                }
                if (connected == false)
                {
                    MessegeField.Text = " ";
                    return;
                }
                var message = new Message
                {
                    Username = Name,
                    SenderId = 1,
                    _Message = MessegeField.Text,
                };
                Chat.Items.Add(message);
                writer.WriteLine($"{Name}|{MessegeField.Text}|{DateTime.Now.ToString("HH:mm")}");

            }
        }

        private void CloseWindow(object sender, CancelEventArgs e)
        {
            if (TcpClient != null)
            {
                writer.WriteLine("DISCONNECT");
                writer.Close();
                reader.Close();
                TcpClient.Close();
            }
        }
    }
    public class Message : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String Info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(Info));
            }
        }
        public string Username { get; set; }
        public int SenderId { get; set; }
        public string _Message { get; set; }
        private string RealStamp;
        public string TimeStamp { get { return RealStamp; } set { RealStamp = value; NotifyPropertyChanged("TimeStamp"); } }
        public DispatcherTimer Timer { get; set; }
        public DateTime Sending_time { get; set; }
        public Message(DateTime? Sendingtime = null)
        {
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += Timer_Tick;
            if (Sendingtime != null)
            {
                Sending_time = (DateTime)Sendingtime;
            }
            else
            {
                Sending_time = DateTime.Now;
            }
            Timer_Tick(null, null);
            Timer.Start();
            //Sending_time = DateTime.Now;


        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan time = DateTime.Now - Sending_time;
            if (time.TotalSeconds < 60)
            {
                TimeStamp = "Now";
            }
            if (time.TotalSeconds > 60 && time.Minutes < 15)
            {
                TimeStamp = $"{time.Minutes}m ago";
            }
            if (time.TotalHours < 24 && time.TotalMinutes > 15)
            {
                TimeStamp = Sending_time.ToString("HH:mm");
            }
            if (time.TotalHours > 24)
            {
                TimeStamp = Sending_time.ToString("dd/MM/yyyy");
            }
        }
    }
    public class MessageSelector : DataTemplateSelector
    {
        public DataTemplate LeftMessage { get; set; }
        public DataTemplate RightMessage { get; set; }
        public DataTemplate SystemMessage { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var message = item as Message;
            if (message == null)
                return base.SelectTemplate(item, container);
            switch (message.SenderId)
            {
                case 0: return LeftMessage;
                case 1: return RightMessage;
                case -1: return SystemMessage;
            }

            return base.SelectTemplate(item, container);
        }


    }

}