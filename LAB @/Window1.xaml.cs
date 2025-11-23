using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LAB__
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private TcpClient client;
        User usr;
        public Window1()
        {
            InitializeComponent();
            usr = new User();
            this.DataContext = usr;
        }

        private void ConnectNewUser(object sender, RoutedEventArgs e)
        {
            if(PortField!=null) 
            {
                int port;
                if(int.TryParse(PortField.Text, out port))
                    usr.Port = port;
                else
                {
                    MessageBox.Show("Invalid Port Number !!");
                    return;
                }
            }

            ((MainWindow)Application.Current.MainWindow).Name = UserNameField.Text;
            usr.Name = UserNameField.Text;
            usr.Address = AddressField.Text;
            usr.Password = PasswordField.Password;
            usr.Connected = false;
            Thread perforamnce = new Thread(PerformConnection);
            perforamnce.Start();
        }
        private void PerformConnection()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(new Action(() => { Bar.Visibility = Visibility.Visible; }));
                Application.Current.Dispatcher.Invoke(new Action(() => { Submit.IsEnabled = false; }));
            

                client = new TcpClient();
                client.Connect(usr.Address, usr.Port);
                NetworkStream stream = client.GetStream();
                StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                writer.WriteLine($"{usr.Name}:{usr.Password}");
                MessageBox.Show("The login is sent");
                StreamReader reader = new StreamReader(stream);
                string response =" ";
                try
                {
                    response = reader.ReadLine();
                if (response == null)
                {
                    MessageBox.Show("Connection closed by server.");
                    return;
                }
                }
                catch (IOException)
                {
                    MessageBox.Show("The reader was closed to fast");
                }
                MessageBox.Show("The login Received");

                switch (response)
                {
                    case "OK":
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ((MainWindow)Application.Current.MainWindow).connected = true;
                            ((MainWindow)Application.Current.MainWindow).ConnectMessage();
                        });
                        Application.Current.Dispatcher.Invoke(() => {
                            MessageBox.Show("Connected successfully!");
                            ((MainWindow)Application.Current.MainWindow).StartMesseging(client);
                            this.Close();
                        });
                        return;
                    case "Pass":
                        MessageBox.Show("Incorect Password");
                        Application.Current.Dispatcher.Invoke(new Action(() => { Submit.IsEnabled = true; Bar.Visibility = Visibility.Hidden; }));
                        return;
                    case "Name":
                        MessageBox.Show("Incorect Name");
                        Application.Current.Dispatcher.Invoke(new Action(() => { Submit.IsEnabled = true; Bar.Visibility = Visibility.Hidden; }));
                        return;
                    default:
                        Application.Current.Dispatcher.Invoke(new Action(() => { Submit.IsEnabled = true; Bar.Visibility = Visibility.Hidden; }));
                        return;
                }


            }
            catch(Exception ex) {
            MessageBox.Show($"Performance error : {ex.Message}");
            Application.Current.Dispatcher.Invoke(new Action(() => { Submit.IsEnabled = true; Bar.Visibility = Visibility.Hidden; }));
            return;
            }
        }
    }
    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public bool Connected { get; set; }
        public User()
        {}
    }

}
