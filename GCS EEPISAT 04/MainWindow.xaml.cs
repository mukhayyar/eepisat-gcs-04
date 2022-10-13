using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;

namespace GCS_EEPISAT_04
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool _continue;
        static SerialPort _serialPort;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShutdownOpenModal(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Are you sure to shutdown the GCS?", "", MessageBoxButton.OKCancel);
        }

        private void RestartOpenModal(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure to restart the GCS?", "", MessageBoxButton.OKCancel);
            switch (result)
            {
                case MessageBoxResult.OK:
                    Application.Current.Shutdown();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        public static void SerialPortDetect()
        {
            string name;
            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
            Thread readThread = new Thread(Read);

            // create a new serial object with default settings.
            _serialPort = new SerialPort();

            // Set properties 
            _serialPort.PortName = SetPortName(_serialPort.PortName);
            _serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);

            _serialPort.Open();
            _continue = true;
            readThread.Start();

            Console.Write("Name : ");
            name = Console.ReadLine();

            Console.WriteLine("Type QUIT to exit");

            while(_continue)
            {
                message = Console.ReadLine();

                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }
                else
                {
                    _serialPort.WriteLine(
                        String.Format("<{0}>: {1}", name, message));
                }
            }

            readThread.Join();
            _serialPort.Close();
        }

        public static void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    Console.WriteLine(message);
                }
                catch (TimeoutException) { return; }
            }
        }

        public static string SetPortName(string defaultPortName)
        {
            string portName;

            Console.WriteLine("Available Ports: ");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("     {0}", s);
            }
            Console.WriteLine("Enter COM port value (Default: {0}): ", defaultPortName);
            portName = Console.ReadLine();

            if (portName == "" || !(portName.ToLower().StartsWith("com")))
            {
                portName = defaultPortName;
            }
            return portName;
        }

        public static int SetPortBaudRate(int defaultBaudRate)
        {
            string baudRate;

            Console.WriteLine("Baud Rate(default:{0}): ", defaultBaudRate);
            baudRate = Console.ReadLine();

            if (baudRate == "")
            {
                baudRate = defaultBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }
    }
}
