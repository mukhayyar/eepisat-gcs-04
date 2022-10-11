
#region Library
//Library Gmap
using GMap.NET;
using GMap.NET.ObjectModel;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;

// Library MQTT
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

// System
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Device.Location;
using System.IO;
using System.IO.Ports;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
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
using System.Text.RegularExpressions;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Drawing;
using System.Net;
using System.Media;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Runtime.CompilerServices;
using ZedGraph;
using MindFusion.Charting.Wpf;

//Graph View Model
using GCS_G03.ViewModels.AltitudeGraph;
using GCS_G03.ViewModels.TemperatureGraph;
using GCS_G03.ViewModels.VoltageGraph;
using GCS_G03.ViewModels.AcceleroGraph;
using GCS_G03.ViewModels.GyroGraph;
using GCS_G03.ViewModels.MagnitudeGraph;
using System.Management;
using System.Timers;

//Custom MessageBox
#endregion

namespace GCS_G03
{
    public partial class MainWindow : Window
    {
        #region Message Box Color
        public void exitmsgbx()
        {
            bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#B22727");
            MessageOK.Visibility = Visibility.Visible;
            MessageOK.Focus();
            MessageNO.Visibility = Visibility.Visible;
            CMBox.Visibility = Visibility.Visible;
            screenfilter.Visibility = Visibility.Visible;
            winhost.Visibility = Visibility.Hidden;
        }

        public void restartmsgbx()
        {
            bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#3BACB6");
            MessageOK.Visibility = Visibility.Visible;
            MessageOK.Focus();
            MessageNO.Visibility = Visibility.Visible;
            CMBox.Visibility = Visibility.Visible;
            screenfilter.Visibility = Visibility.Visible;
            winhost.Visibility = Visibility.Hidden;
        }

        public void errormsgbx()
        {
            bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D"); ;
            MessageOK.Visibility = Visibility.Hidden;
            MessageNO.Visibility = Visibility.Hidden;
            MessageOK1.Visibility = Visibility.Visible;
            MessageOK1.Focus();
            CMBox.Visibility = Visibility.Visible;
            screenfilter.Visibility = Visibility.Visible;

        }

        public void warningmsgbx()
        {
            bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFEEAD");
            MessageOK.Visibility = Visibility.Hidden;
            MessageNO.Visibility = Visibility.Hidden;
            MessageOK1.Visibility = Visibility.Visible;
            winhost.Visibility = Visibility.Hidden;
            MessageOK1.Focus();
            CMBox.Visibility = Visibility.Visible;
            screenfilter.Visibility = Visibility.Visible;
        }
        #endregion

        #region Public Variable and Enumerations

        //Graph View Model Variable
        public readonly AltSeriesVM AltDataGraph = new AltSeriesVM();
        public readonly PayloadSeriesVM PydDataGraph = new PayloadSeriesVM();
        public readonly GpsSeriesVM GpsDataGraph = new GpsSeriesVM();

        public readonly CtrTempSeriesVM AltTempDataGraph = new CtrTempSeriesVM();
        public readonly PydTempSeriesVM PydTempDataGraph = new PydTempSeriesVM();

        public readonly VoltVM VoltDataGraph = new VoltVM();

        public readonly RAccelVM RAccelDataGraph = new RAccelVM();
        public readonly PAccelVM PAccelDataGraph = new PAccelVM();
        public readonly YAccelVM YAccelDataGraph = new YAccelVM();

        public readonly RGyroVM RGyroDataGraph = new RGyroVM();
        public readonly PGyroVM PGyroDataGraph = new PGyroVM();
        public readonly YGyroVM YGyroDataGraph = new YGyroVM();

        public readonly RMagVM RMagDataGraph = new RMagVM();
        public readonly PMagVM PMagDataGraph = new PMagVM();
        public readonly YMagVM YMagDataGraph = new YMagVM();

        public double t = 0;

        //Variabel Map Overlay
        PointLatLng mapPointContainer;
        PointLatLng mapPointGCS;
        public static GMapMarker mapMarkerContainer;
        public static GMapMarker mapMarkerGCS;
        public static GMapOverlay mapOverlay;
        public readonly GMap.NET.ObjectModel.ObservableCollection<GMapMarker> Markers;
        private List<PointLatLng> points;
        private GeoCoordinateWatcher watcher = null;
        double distance = 0;

        //Variabel dan Enumerasi Baca Serial Port
        enum Sequencer { readSensor };
        Sequencer stepData = Sequencer.readSensor;
        char[] splitterData = { ',' };
        char[] trimmerData = { '#', '~' };
        string[] splitData;
        string dataSensor;
        public delegate void AddDataDelegate(String myString);
        public AddDataDelegate WriteTelemetryData;
        int indexData;
        int dataSum = 0;
        double checkSum;
        bool isAscii = false;
        SerialPort serialport = new SerialPort();

        // Container Telemetry
        double containerTeamID;
        string containerMissionTime; // 23:23:59.01
        double containerPacketCount, containerMaxPacketCount, containerMaxMaxPacketCount;
        string containerPacketType; // C
        string mode; // F & S
        string tpReleased; // R
        double altitude, maxAltitude, maxMaxAltitude;
        double temperature, maxTemperature, maxMaxTemperature;
        double voltage, maxVoltage, maxMaxVoltage;
        string gpsTime; // 23:23:59
        double gpsLatitude;
        double gpsLongitude;
        double gpsAltitude;
        double gpsSatelite;
        string softwareState; // BOOT
        double containerPayloadPacketCount;
        string cmdEcho; // CXON
        double checkSumCon;

        // Payload Telemetry
        double payloadTeamID;
        string payloadMissionTime;
        double payloadPacketCount;
        string payloadPacketType; //S1
        double payloadAltitude;
        double payloadTemperature;
        double payloadVoltage;
        double payloadGyro_R;
        double payloadGyro_P;
        double payloadGyro_Y;
        double payloadAccel_R;
        double payloadAccel_P;
        double payloadAccel_Y;
        double payloadMag_R;
        double payloadMag_P;
        double payloadMag_Y;
        double payloadPointingError;
        string payloadSoftwareState;
        double checkSumSP;

        // Variabel Save Telemetry CSV
        StreamWriter writeCon;
        StreamWriter writePay;
        bool containerLogStart = false;
        bool payloadLogStart = false;
        string containerFileLog = "";
        string payloadFileLog = "";
        int logDataCountCon = 0;
        int logDataCountTP = 0;
        ushort clickCon = 0;
        ushort clickTP = 0;

        // Variabel Visualisasi 3D
        List<HelixToolkit.Wpf.Polygon> polygons = new List<HelixToolkit.Wpf.Polygon>();
        string fileobj;
        float rotate = 0;
        double zoomx = 0.600;
        double zoomy = 0.600;
        double zoomz = 0.600;

        // Variabel MQTT
        //string topic = "teams/1010";
        //string username = "1010";
        //string password = "Noupjory474";
        //MqttClient client;
        //byte code;
        //bool mqtt = true;
        //bool mqttcon = false;

        // Variabel CSV Simulation Mode
        List<String> Col1 = new List<String>();
        List<String> Col2 = new List<String>();
        List<String> Col3 = new List<String>();
        List<String> Col4 = new List<String>();
        List<String> Col5 = new List<String>();
        List<String> Col6 = new List<String>();
        string[] fields;
        int timerCSV = 0;
        int checkCSV = 0;

        // Variabel Command
        int lineCommand = 0;
        string pressure;
        string time = DateTime.UtcNow.ToString("HH:mm:ss");
        string totalWaktu;

        // Variabel Grafik
        double timer = 0;
        double maxvoltage, maxMaxvoltage;

        // Variabel Path File
        string binAppPath = System.AppDomain.CurrentDomain.BaseDirectory;

        // TimerSimulation
        DispatcherTimer timerSimulation = new DispatcherTimer();
        DispatcherTimer timergraph = new DispatcherTimer();

        // Open File Dialog
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

        bool resetapp = false;
        bool exitapp = false;

        // Detector Device
        ManagementEventWatcher detector;
        private static string MustHavePort = "COM3";
        string SerialPortNumber;

        //Map
        DispatcherTimer aTimer;
        #endregion

        #region Form Configuration

        public MainWindow()
        {
            InitializeComponent();
            ConnectPortBtn.Visibility = Visibility.Visible;
            DisconnectPortBtn.Visibility = Visibility.Hidden;

            SE.Visibility = Visibility.Hidden;
            SD.Visibility = Visibility.Hidden;
            SettingMenu.Visibility = Visibility.Visible;

            serialport.DataReceived += new SerialDataReceivedEventHandler(serialport_datareceive);
            MainPage.SelectedIndex = 0;

            Graph_Load();
            MissionTime();

            CMDTextBox1.IsReadOnly = false;
            Homemenu.IsChecked = true;
            MapSatelliteButton.IsChecked = true;

            //client = new MqttClient("cansat.info");
            //code = client.Connect(Guid.NewGuid().ToString(), username, password);

            timerSimulation.Interval = TimeSpan.FromMilliseconds(1000);
            timerSimulation.Tick += timerSimulation_Tick;
            timerSimulation.Start();

            timergraph.Interval = TimeSpan.FromMilliseconds(250);
            timergraph.Tick += Timergraph_Tick;

            //fileobj = System.AppDomain.CurrentDomain.BaseDirectory + "/3DModel/rudal5.obj";
        }

        public void USBChangedEvent(object sender, EventArrivedEventArgs e)
        {
            (sender as ManagementEventWatcher).Stop();

            Dispatcher.Invoke((MethodInvoker)delegate
            {
                ManagementObjectSearcher deviceList = new ManagementObjectSearcher("Select Name, Description, DeviceID from Win32_SerialPort");

                // List to store available USB serial devices plugged in. (Arduino(s)/Cell phone(s)).
                List<String> ComPortList = new List<String>();

                ComportDropdown.Items.Clear();
                // Any results? There should be!
                if (deviceList != null)
                {
                    // Enumerate the devices
                    foreach (ManagementObject device in deviceList.Get())
                    {
                        SerialPortNumber = device["DeviceID"].ToString();
                        string serialName = device["Name"].ToString();
                        string SerialDescription = device["Description"].ToString();
                        ComPortList.Add(SerialPortNumber);
                        ComportDropdown.Items.Add(SerialPortNumber);
                    }
                }
                else
                {
                    ComportDropdown.Items.Add("NO SerialPorts AVAILABLE!");
                    // Inform the user about the disconnection of the arduino ON PORT 3 etc...
                }

                if (!ComPortList.Contains(MustHavePort))
                {
                    // Inform the user about the disconnection of the arduino ON PORT 3 etc...
                }

            });
            (sender as ManagementEventWatcher).Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Mengambil data lokasi GCS
            watcher = new GeoCoordinateWatcher();
            watcher.StatusChanged += watcher_StatusChanged;
            watcher.Start();

            detector = new ManagementEventWatcher("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 or EventType = 3");
            detector.EventArrived += new EventArrivedEventHandler(USBChangedEvent);
            detector.Start();

            try
            {
                // Mengambil Port yang tersedia
                string[] MyComPort = SerialPort.GetPortNames();
                foreach (string ComPort in MyComPort)
                {
                    ComportDropdown.Items.Add(ComPort);
                }

                string[] baudRate = { "2400", "4800", "9600", "19200", "38400", "57600", "74880", "115200" };
                foreach (string baud in baudRate)
                {
                    BaudrateDropdown.Items.Add(baud);
                }

                softwareStateLabel.Content = "BOOT";
                payloadSoftwareStateLabel.Content = "STAND_BY";
                teamIDLabel.Content = "1010";
                FlightOnOffSwitch.IsChecked = true;
                SimulationStatus.Content = "DISABLE";
                SimulationStatusPane.Background = System.Windows.Media.Brushes.DarkRed;
                CMDEchoLabel.Content = "CMD Echo";

                this.WriteTelemetryData = new AddDataDelegate(NewLine);
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                #region messageBox
                errormsgbx();
                TBContent.Text = "Error!";
                TBContent1.Text = ex.Message;
                #endregion
                return;
            }
        }

        private void MissionTime()
        {
            string jamStr = DateTime.UtcNow.ToString("HH");
            int jamInt;
            jamInt = Convert.ToInt32(jamStr);
            jamInt *= 3600;

            string menitStr = DateTime.UtcNow.ToString("mm");
            int menitInt;
            menitInt = Convert.ToInt32(menitStr);
            menitInt *= 60;

            string detikStr = DateTime.UtcNow.ToString("ss");
            int detikInt;
            detikInt = Convert.ToInt32(detikStr);
            detikInt += menitInt + jamInt;

            //string Hundredth_detikStr = DateTime.UtcNow.ToString("ss");
            //int Hundredth_detikInt;
            //Hundredth_detikInt = Convert.ToInt32(Hundredth_detikStr);
            //Hundredth_detikInt = (detikInt % 60 )/ 100;
            //Hundredth_detikInt += detikInt + menitInt + jamInt; 

            string jam;
            string menit;
            string detik;
            //string Hundredth_detik;

            jam = String.Format("{0:00}", jamInt / 3600);
            menit = String.Format("{0:00}", (menitInt / 60) % 60);
            detik = String.Format("{0:00}", detikInt % 60);
            //Hundredth_detik = String.Format("{0:00}", Hundredth_detikInt % 100);

            totalWaktu = jam + ":" + menit + ":" + detik /*+ "." + Hundredth_detik*/;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            timerSimulation.Stop();
            timergraph.Stop();
            timerCSV = 0;
            containerLogStart = false;
            payloadLogStart = false;
        }

        #endregion

        #region GUI Button

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            /*MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to exit the application?", "Exit Application", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    System.Windows.Application.Current.Shutdown();
                    mqtt = false;
                    client.Disconnect();
                    break;
                case MessageBoxResult.No:

                    break;
            }*/
            exitmsgbx();
            TBContent.Text = "Exit Application";
            TBContent1.Text = "Do you want to exit the application?";
            exitapp = true;
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs e)
        {
            /*MessageBoxResult result = System.Windows.MessageBox.Show("Do you want to restart the application?", "Restart Application", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
                    System.Windows.Application.Current.Shutdown();
                    mqtt = false;
                    client.Disconnect();
                    break;
                case MessageBoxResult.No:

                    break;
            }*/
            restartmsgbx();
            TBContent.Text = "Restart Application";
            TBContent1.Text = "Do you want to restart the application?";
            resetapp = true;
        }

        private void ConnectPortBtn_Click(object sender, RoutedEventArgs e)
        {
            if (serialport.IsOpen == false)
            {
                try
                {
                    //ConnectPort.Image = Properties.Resources.icon_Disconnect;
                    if (ComportDropdown.SelectedItem == null)
                    {
                        bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                        TBContent.Text = "Warning!";
                        TBContent1.Text = "Comport Can't be Empty!";
                        MessageOK.Visibility = Visibility.Hidden;
                        MessageNO.Visibility = Visibility.Hidden;
                        MessageOK1.Visibility = Visibility.Visible;
                        winhost.Visibility = Visibility.Hidden;
                        MessageOK1.Focus();
                        CMBox.Visibility = Visibility.Visible;
                        //screenfilter.Visibility = Visibility.Visible;
                    }

                    else if (BaudrateDropdown.SelectedItem == null)
                    {
                        bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                        TBContent.Text = "Warning!";
                        TBContent1.Text = "Serial Can't be Empty!";
                        MessageOK.Visibility = Visibility.Hidden;
                        MessageNO.Visibility = Visibility.Hidden;
                        MessageOK1.Visibility = Visibility.Visible;
                        winhost.Visibility = Visibility.Hidden;
                        MessageOK1.Focus();
                        CMBox.Visibility = Visibility.Visible;
                        //screenfilter.Visibility = Visibility.Visible;
                    }

                    else
                    {
                        serialport.PortName = ComportDropdown.SelectedItem.ToString();
                        serialport.BaudRate = Convert.ToInt32(BaudrateDropdown.SelectedItem);
                        serialport.NewLine = "\r";
                        serialport.Close();
                        serialport.Open();
                        PortStatus.Content = "Connected To Serial Port";
                        PortStatusPane.Background = System.Windows.Media.Brushes.ForestGreen;
                        ConnectPortBtn.Visibility = Visibility.Hidden;
                        DisconnectPortBtn.Visibility = Visibility.Visible;
                        timergraph.Start();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                    #region messageBox
                    bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                    TBContent.Text = "Error!";
                    TBContent1.Text = ex.Message;
                    MessageOK.Visibility = Visibility.Hidden;
                    MessageNO.Visibility = Visibility.Hidden;
                    MessageOK1.Visibility = Visibility.Visible;
                    winhost.Visibility = Visibility.Hidden;
                    MessageOK1.Focus();
                    CMBox.Visibility = Visibility.Visible;
                    //screenfilter.Visibility = Visibility.Visible;
                    #endregion
                    PortStatus.Content = "Can't Connect To Serial Port, Try Again!";
                    PortStatusPane.Background = System.Windows.Media.Brushes.Firebrick;
                    return;
                }
            }
        }

        private void DisconnectPortBtn_Click(object sender, RoutedEventArgs e)
        {

            if (serialport.IsOpen == true)
            {
                try
                {
                    serialport.Close();
                    PortStatus.Content = "Disconnected From Serial Port";
                    PortStatusPane.Background = System.Windows.Media.Brushes.Firebrick;
                    ConnectPortBtn.Visibility = Visibility.Visible;
                    DisconnectPortBtn.Visibility = Visibility.Hidden;
                    CMDTextBox1.IsReadOnly = true;
                    timergraph.Stop();
                }
                catch
                {
                    #region messageBox
                    bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                    TBContent.Text = "Warning!";
                    TBContent1.Text = "Serial Port Error!";
                    MessageOK.Visibility = Visibility.Hidden;
                    MessageNO.Visibility = Visibility.Hidden;
                    MessageOK1.Visibility = Visibility.Visible;
                    winhost.Visibility = Visibility.Hidden;
                    MessageOK1.Focus();
                    CMBox.Visibility = Visibility.Visible;
                    //screenfilter.Visibility = Visibility.Visible;
                    #endregion
                    return;
                }
            }
        }

        private void RestartPortBtn_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                serialport.Close();
                PortStatus.Content = "Disconnected From Serial Port";
                PortStatusPane.Background = System.Windows.Media.Brushes.Firebrick;
                /*ComportDropdown.Items.Clear();
                string[] MyComPort = SerialPort.GetPortNames();
                foreach (string ComPort in MyComPort)
                {
                    ComportDropdown.Items.Add(SerialPortNumber);
                }*/

                /*BaudrateDropdown.Items.Clear();
                string[] baudRate = { "2400", "4800", "9600", "19200", "38400", "57600", "74880", "115200" };
                foreach (string baud in baudRate)
                {
                    BaudrateDropdown.Items.Add(baud);
                }*/

                ComportDropdown.SelectedValue = null;
                BaudrateDropdown.SelectedValue = null;

                ConnectPortBtn.Visibility = Visibility.Visible;
                DisconnectPortBtn.Visibility = Visibility.Hidden;
            }
            catch
            {
                return;
            }
        }

        private void MapZoomSlider_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

        }

        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog.Title = "Import CSV File";
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv";

            if (openFileDialog.ShowDialog() == true)
            {
                csvTextBox.Text = openFileDialog.FileName;
                BindData(csvTextBox.Text);
            }
        }
        #endregion

        #region Custom MessageBpx

        private void MessageOK_Click(object sender, RoutedEventArgs e)
        {
            if (exitapp == true)
            {
                System.Windows.Application.Current.Shutdown();
                Environment.Exit(0);
                //mqtt = false;
                //client.Disconnect();
                CMBox.Visibility = Visibility.Hidden;
            }

            else if (resetapp == true)
            {
                System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
                System.Windows.Application.Current.Shutdown();
                //mqtt = false;
                //client.Disconnect();
                CMBox.Visibility = Visibility.Hidden;
                screenfilter.Visibility = Visibility.Hidden;
            }
        }

        private void MessageNO_Click(object sender, RoutedEventArgs e)
        {
            CMBox.Visibility = Visibility.Hidden;
            screenfilter.Visibility = Visibility.Hidden;
            winhost.Visibility = Visibility.Visible;
            exitapp = false;
            resetapp = false;
        }


        private void MessageOK1_Click(object sender, RoutedEventArgs e)
        {
            CMBox.Visibility = Visibility.Hidden;
            MessageOK1.Visibility = Visibility.Hidden;
            screenfilter.Visibility = Visibility.Hidden;
            winhost.Visibility = Visibility.Visible;
        }

        #endregion

        #region Button Animation

        private void SidebarEnable_Click(object sender, RoutedEventArgs e)
        {
            SE.Visibility = Visibility.Hidden;
            SD.Visibility = Visibility.Visible;

            SettingMenu.Visibility = Visibility.Visible;
        }

        private void SidebarDisable_Click(object sender, RoutedEventArgs e)
        {
            SE.Visibility = Visibility.Visible;
            SD.Visibility = Visibility.Hidden;

            SettingMenu.Visibility = Visibility.Hidden;
        }

        private void Homemenu_Click(object sender, RoutedEventArgs e)
        {

            MainPage.SelectedIndex = 0;
        }

        private void Graphmenu_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 1;
        }

        private void Mapmenu_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 2;
            GmapView_LoadMap();
        }

        private void LoadingModelmenu_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 3;
        }

        private void DataCsvmenu_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 4;
        }

        private void ContainerCsvmenu_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 5;
        }

        private void PayloadCsv_Click(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 6;
        }

        private void SerialControlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SerialControlTextBox.ScrollToEnd();
        }

        #endregion

        #region CSV Data and Simulation Mode Configuration

        private void BindData(string filePath) //Pengambilan Data
        {
            try
            {

                DataTable dt = new DataTable();
                string[] data = File.ReadAllLines(filePath);
                string[] kolom = null;
                int x = 0;

                foreach (string baris in data)
                {
                    kolom = baris.Split(',');

                    if (x == 0)
                    {
                        for (int i = 0; i <= kolom.Count() - 1; i++)
                        {
                            dt.Columns.Add(kolom[i]);
                        }
                        x++;
                    }
                    else
                    {
                        dt.Rows.Add(kolom);
                    }
                }
                DataCsv.ItemsSource = dt.DefaultView;
                DataCsv.ScrollIntoView(DataCsv.Items[DataCsv.Items.Count - 1]);

            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        private void SendCSV() //Penamaan File CSV
        {
            try
            {


                var filePath = openFileDialog.FileName;

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        while (!reader.EndOfStream)
                        {
                            string data = reader.ReadLine();

                            if (!data.Contains(@"#"))
                            {
                                var values = data.Split(',');

                                if (!values.Any(s => s == ""))
                                {
                                    string field = values[3];
                                    Col4.Add(field);
                                }
                            }
                        }
                        timerSimulation.Start();
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private void btnPressure_Click(object sender, EventArgs e)
        {

        }

        #endregion

        #region Map Configuration

        private void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e) //watcher status map
        {
            if (e.Status == GeoPositionStatus.Ready)
            {
                if (watcher.Position.Location.IsUnknown)
                {
                    GCSLocationStatus.Content = "Can't get GCS location";
                    GCSLocationStatusPane.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 67, 53));
                }
                else
                {
                    GCSLocationStatus.Content = "GCS Location Founded";
                    GCSLatitudeValue.Content = String.Format("{0:00.0000}", watcher.Position.Location.Latitude);
                    GCSLongitudeValue.Content = String.Format("{0:000.0000}", watcher.Position.Location.Longitude);
                    GCSLocationStatusPane.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 168, 83));
                }
            }
        }

        private void GmapView_Load(object sender, EventArgs e)
        {
            try
            {
                //GMapProvider.WebProxy = WebRequest.GetSystemWebProxy();
                //GMapProvider.WebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                GmapView.MapProvider = GoogleSatelliteMapProvider.Instance;
                //GmapView.Manager.Mode = AccessMode.ServerAndCache;
                ////GMaps.Instance.Mode = AccessMode.ServerAndCache;
                ////GMaps.Instance.Mode = AccessMode.ServerOnly;
                //GmapView.CacheLocation = binAppPath + "\\MapCache\\";
                //Map.Position = new PointLatLng(-7.276632, 112.793830); //Lokasi PENS

                GmapView.MinZoom = 0;
                GmapView.MaxZoom = 18;
                GmapView.Zoom = 10;
                GmapView.DragButton = MouseButtons.Left;
                GmapView.ShowCenter = false;
                GmapView.RoutesEnabled = false;
                GmapView.MarkersEnabled = true;
                //Map.ForceDoubleBuffer = true;
                //mapOverlay = new GMapOverlay("mapOverlay");

                GmapView.Position = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);

                //if (serialport.IsOpen == true)
                //{
                //    Bitmap IconRocket = new Bitmap(binAppPath + "/icon/rocket1.png");
                //    Bitmap IconGCS = new Bitmap(binAppPath + "/icon/EEPISat.png");

                //    var markerContainer = new GMapOverlay("markers");
                //    mapMarkerContainer = new GMarkerGoogle(new PointLatLng(gpsLatitude, gpsLongitude), IconRocket);


                //    mapMarkerContainer.ToolTipMode = MarkerTooltipMode.Always;

                //    var tooltipContainer = new GMapToolTip(mapMarkerContainer);
                //    tooltipContainer.Fill = new SolidBrush(System.Drawing.Color.FromArgb(255, 153, 51));
                //    tooltipContainer.Foreground = new SolidBrush(System.Drawing.Color.White);
                //    tooltipContainer.Offset = new System.Drawing.Point(10, -50);
                //    tooltipContainer.Stroke = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.FromArgb(234, 67, 53)));

                //    var markerGCS = new GMapOverlay("markers");
                //    mapMarkerGCS = new GMarkerGoogle(new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude), IconGCS);

                //    mapMarkerGCS.ToolTipMode = MarkerTooltipMode.Always;

                //    var toolTipGCS = new GMapToolTip(mapMarkerGCS);
                //    toolTipGCS.Fill = new SolidBrush(System.Drawing.Color.FromArgb(255, 153, 51));
                //    toolTipGCS.Foreground = new SolidBrush(System.Drawing.Color.White);
                //    toolTipGCS.Offset = new System.Drawing.Point(10, -50);
                //    toolTipGCS.Stroke = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.FromArgb(234, 67, 53)));
                //    //mapPointContainer = new PointLatLng(gpsLatitude, gpsLongitude);
                //    //mapPointGCS = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);

                //    GmapView.Overlays.Add(mapOverlay);
                //    GmapView.Overlays.Add(markerContainer);
                //    mapOverlay.Markers.Add(mapMarkerContainer);
                //    GmapView.Overlays.Add(markerGCS);
                //    mapOverlay.Markers.Add(mapMarkerGCS);


                //}
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        private void GmapView_LoadMap()
        {
            try
            {
                GMapProvider.WebProxy = WebRequest.GetSystemWebProxy();
                GMapProvider.WebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                //GmapView.MapProvider = GoogleSatelliteMapProvider.Instance;
                GmapView.Manager.Mode = AccessMode.ServerAndCache;

                GmapView.CacheLocation = binAppPath + "\\MapCache\\";

                GmapView.Position = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);

                if (mapMarkerContainer != null && mapMarkerGCS != null)
                {
                    mapOverlay.Markers.Remove(mapMarkerContainer);
                    mapOverlay.Markers.Remove(mapMarkerGCS);
                }

                if (serialport.IsOpen == true)
                {
                    Bitmap IconRocket = new Bitmap(binAppPath + "/icon/rocket1.png");
                    Bitmap IconGCS = new Bitmap(binAppPath + "/icon/EEPISat.png");

                    mapOverlay = new GMapOverlay("mapOverlay");

                    var markerContainer = new GMapOverlay("markers");
                    mapMarkerContainer = new GMarkerGoogle(new PointLatLng(gpsLatitude, gpsLongitude), IconRocket);


                    mapMarkerContainer.ToolTipMode = MarkerTooltipMode.Always;

                    var tooltipContainer = new GMapToolTip(mapMarkerContainer);
                    tooltipContainer.Fill = new SolidBrush(System.Drawing.Color.FromArgb(255, 153, 51));
                    tooltipContainer.Foreground = new SolidBrush(System.Drawing.Color.White);
                    tooltipContainer.Offset = new System.Drawing.Point(10, -50);
                    tooltipContainer.Stroke = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.FromArgb(234, 67, 53)));

                    var markerGCS = new GMapOverlay("markers");
                    mapMarkerGCS = new GMarkerGoogle(new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude), IconGCS);

                    mapMarkerGCS.ToolTipMode = MarkerTooltipMode.Always;

                    var toolTipGCS = new GMapToolTip(mapMarkerGCS);
                    toolTipGCS.Fill = new SolidBrush(System.Drawing.Color.FromArgb(255, 153, 51));
                    toolTipGCS.Foreground = new SolidBrush(System.Drawing.Color.White);
                    toolTipGCS.Offset = new System.Drawing.Point(10, -50);
                    toolTipGCS.Stroke = new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.FromArgb(234, 67, 53)));
                    //mapPointContainer = new PointLatLng(gpsLatitude, gpsLongitude);
                    //mapPointGCS = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);

                    //Dispatcher.Invoke(() =>
                    //{
                    GmapView.Overlays.Add(mapOverlay);
                    //}
                    GmapView.Overlays.Add(markerContainer);
                    mapOverlay.Markers.Add(mapMarkerContainer);
                    GmapView.Overlays.Add(markerGCS);
                    mapOverlay.Markers.Add(mapMarkerGCS);

                    //GmapView.ReloadMap();
                }
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        public void GmapView_Region()
        {
            try
            {
                if (serialport.IsOpen == true)
                {
                    try
                    {
                        if (gpsLatitude != 0 && gpsLongitude != 0)
                        {
                            if (mapMarkerContainer != null)
                            {
                                mapMarkerContainer.Position = new PointLatLng(gpsLatitude, gpsLongitude);
                                mapMarkerContainer.ToolTipText = $"Container\n" + $" Latitude : {gpsLatitude}, \n" + $" Longitude : {gpsLongitude}";

                                double betweenlat = gpsLatitude + watcher.Position.Location.Latitude;
                                double betweenlng = gpsLongitude + watcher.Position.Location.Longitude;
                                GmapView.Position = new PointLatLng(betweenlat / 2, betweenlng / 2);
                            }
                        }
                    }
                    catch (NullReferenceException)
                    {
                        return;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return;
                    }
                    catch (FormatException)
                    {
                        return;
                    }
                    catch
                    {
                        return;
                    }

                    double dLat1InRad = watcher.Position.Location.Latitude * (Math.PI / 180);
                    double dLong1InRad = watcher.Position.Location.Longitude * (Math.PI / 180);

                    double dLat2InRad = gpsLatitude * (Math.PI / 180);
                    double dLong2InRad = gpsLongitude * (Math.PI / 180);

                    double dLongitude = dLong2InRad - dLong1InRad;
                    double dLatitude = dLat2InRad - dLat1InRad;

                    // Intermediate result a.
                    double a = Math.Pow(Math.Sin(dLatitude / 2), 2) + Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) * Math.Pow(Math.Sin(dLongitude / 2), 2);

                    // Intermediate result c (great circle distance in Radians).
                    double c = 2 * Math.Asin(Math.Sqrt(a));

                    // Perhitungan
                    const Double kEarthRadiusKms = 6371;
                    distance = kEarthRadiusKms * c * 1000;

                }

                if (mapMarkerGCS != null)
                {
                    mapMarkerGCS.Position = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);
                    mapMarkerGCS.ToolTipText = $"GCS\n" + $" Latitude : {Math.Round(watcher.Position.Location.Latitude, 4)}, \n" + $" Longitude : {Math.Round(watcher.Position.Location.Longitude, 4)}";
                }

                GCSDistanceValue.Content = String.Format("{0:0000}", distance);

                if (gpsLatitude == 0 && gpsLongitude == 0)
                {
                    GCSDistanceValue.Content = "Unknown";
                }
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch (Exception ex)
            {
                return;
            }

        }

        private void MapSatelliteButton_Click(object sender, RoutedEventArgs e)
        {
            if (GmapView.MapProvider == null)
            {
                GmapView.MapProvider = GoogleSatelliteMapProvider.Instance;
            }
            else
            {
                if (GmapView.MapProvider != GoogleSatelliteMapProvider.Instance)
                {
                    GmapView.MapProvider = GoogleSatelliteMapProvider.Instance;
                }
            }
        }

        private void mapDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (GmapView.MapProvider == null)
            {
                GmapView.MapProvider = GoogleMapProvider.Instance;
            }
            else
            {
                if (GmapView.MapProvider != GoogleMapProvider.Instance)
                {
                    GmapView.MapProvider = GoogleMapProvider.Instance;
                }
            }
        }

        private void MapTerrainButton_Click(object sender, RoutedEventArgs e)
        {
            if (GmapView.MapProvider == null)
            {
                GmapView.MapProvider = GoogleTerrainMapProvider.Instance;
            }
            else
            {
                if (GmapView.MapProvider != GoogleTerrainMapProvider.Instance)
                {
                    GmapView.MapProvider = GoogleTerrainMapProvider.Instance;
                }
            }
        }

        #endregion

        #region Graph Configuration

        //public SeriesCollection AltSeries { get; set; }

        private void Graph_Load() //Tampilan Grafik
        {
            try
            {

            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        private void GraphCurve()
        {
            try
            {
                #region Altitude Graph
                DataContext = AltDataGraph;
                DataContext = PydDataGraph;
                DataContext = GpsDataGraph;
                AltitudeSeries.ItemsSource = AltDataGraph.DataPoints;
                PayloadSeries.ItemsSource = PydDataGraph.DataPoints;
                GpsSeries.ItemsSource = GpsDataGraph.DataPoints;
                #endregion

                #region Temperature Graph
                DataContext = AltTempDataGraph;
                DataContext = PydTempDataGraph;
                AltitudeTempSeries.ItemsSource = AltTempDataGraph.DataPoints;
                PayloadTempSeries.ItemsSource = PydTempDataGraph.DataPoints;
                #endregion

                #region Voltage Graph
                DataContext = VoltDataGraph;
                VoltageSeries.ItemsSource = VoltDataGraph.DataPoints;
                #endregion

                #region Accelerometer Graph
                DataContext = RAccelDataGraph;
                DataContext = PAccelDataGraph;
                DataContext = YAccelDataGraph;
                RAccelSeries.ItemsSource = RAccelDataGraph.DataPoints;
                PAccelSeries.ItemsSource = PAccelDataGraph.DataPoints;
                YAccelSeries.ItemsSource = YAccelDataGraph.DataPoints;
                #endregion

                #region Gyrometer Graph
                DataContext = RGyroDataGraph;
                DataContext = PGyroDataGraph;
                DataContext = YGyroDataGraph;
                RGyroSeries.ItemsSource = RGyroDataGraph.DataPoints;
                PGyroSeries.ItemsSource = PGyroDataGraph.DataPoints;
                YGyroSeries.ItemsSource = YGyroDataGraph.DataPoints;
                #endregion

                #region Magnitude Graph
                DataContext = RMagDataGraph;
                DataContext = PMagDataGraph;
                DataContext = YMagDataGraph;
                RMagSeries.ItemsSource = RMagDataGraph.DataPoints;
                PMagSeries.ItemsSource = PMagDataGraph.DataPoints;
                YMagSeries.ItemsSource = YMagDataGraph.DataPoints;
                #endregion
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        private void Timergraph_Tick(object sender, EventArgs e)
        {
            if (serialport.IsOpen == true)
            {
                var timer = t * 20 / 300;
                //Altitude Add Point
                var ekg = altitude;
                var ekg1 = payloadAltitude;
                var ekg2 = gpsAltitude;
                AltitudeGraph.SuspendSeriesNotification();
                AltDataGraph.AddDataPoint(ekg, timer, 242);
                PydDataGraph.AddDataPoint(ekg1, timer, 242);
                GpsDataGraph.AddDataPoint(ekg2, timer, 242);
                AltitudeGraph.ResumeSeriesNotification();

                //Temperature Add Point
                var ekg3 = temperature;
                var ekg4 = payloadTemperature;
                TemperatureGraph.SuspendSeriesNotification();
                AltTempDataGraph.AddDataPoint(ekg3, timer, 242);
                PydTempDataGraph.AddDataPoint(ekg4, timer, 242);
                TemperatureGraph.ResumeSeriesNotification();

                //Voltage Add Point
                var ekg5 = voltage;
                VoltageGraph.SuspendSeriesNotification();
                VoltDataGraph.AddDataPoint(ekg5, timer, 242);
                VoltageGraph.ResumeSeriesNotification();

                //Accel Add Point
                var ekg6 = payloadAccel_R;
                var ekg7 = payloadAccel_P;
                var ekg8 = payloadAccel_Y;
                AcceleroGraph.SuspendSeriesNotification();
                RAccelDataGraph.AddDataPoint(ekg6, timer, 242);
                PAccelDataGraph.AddDataPoint(ekg7, timer, 242);
                YAccelDataGraph.AddDataPoint(ekg8, timer, 242);
                AcceleroGraph.ResumeSeriesNotification();

                //Gyro Add Point
                var ekg9 = payloadGyro_R;
                var ekg10 = payloadGyro_P;
                var ekg11 = payloadGyro_Y;
                GyroGraph.SuspendSeriesNotification();
                RGyroDataGraph.AddDataPoint(ekg9, timer, 242);
                PGyroDataGraph.AddDataPoint(ekg10, timer, 242);
                YGyroDataGraph.AddDataPoint(ekg11, timer, 242);
                GyroGraph.ResumeSeriesNotification();

                //Mag Add Point
                var ekg12 = payloadMag_R;
                var ekg13 = payloadMag_P;
                var ekg14 = payloadMag_Y;
                MagnitudeGraph.SuspendSeriesNotification();
                RMagDataGraph.AddDataPoint(ekg12, timer, 242);
                PMagDataGraph.AddDataPoint(ekg13, timer, 242);
                YMagDataGraph.AddDataPoint(ekg14, timer, 242);
                MagnitudeGraph.ResumeSeriesNotification();

                t += 4;
            }
        }
        #endregion

        #region Telemetry Data Configuration 

        //Delegate Variable untuk update ui
        public delegate void uiupdater();
        public delegate void tableupdater();

        private void NewLine(string NewLine)
        {
            SerialControlTextBox.AppendText(NewLine);
        }

        public void serialport_datareceive(object sender, SerialDataReceivedEventArgs e)
        {
            if (stepData == Sequencer.readSensor)
            {

                try
                {
                    this.dataSensor = serialport.ReadLine();
                    //System.Diagnostics.Debug.WriteLine("SerialData_DataReceive : dataSensor " + dataSensor.ToString());
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new uiupdater(VerifyData));
                }

                catch (NullReferenceException)
                {
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                catch (FormatException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                    //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        public void VerifyData()
        {
            SerialControlTextBox.Dispatcher.BeginInvoke(this.WriteTelemetryData, new Object[]
            {
                string.Concat(dataSensor, "------------#END OF PACKET DATA------------\n")
            });
            SerialControlTextBox.SelectionLength = SerialControlTextBox.Text.Length;
            SerialControlTextBox.ScrollToEnd();
            CMDTextBox1.IsReadOnly = false;
            //CMDTextBox1.Focus();
            CMDTextBox2.SelectAll();
            CMDTextBox2.SelectionLength = CMDTextBox2.Text.Length;
            CMDTextBox2.ScrollToEnd();
            dataSum++;

            //if (dataSum == 50)
            //{
            //SerialControlTextBox.Clear();
            //dataSum = 0;
            //}

            isAscii = Regex.IsMatch(dataSensor, @"[^\u0021-\u007E]", RegexOptions.None);

            if (isAscii == false)
            {

                try
                {
                    splitData = dataSensor.Split((char)44); // (char)44 = ','
                    //System.Diagnostics.Debug.WriteLine("VerifyData : splitData " + splitData.ToString());

                    //long sumData = 0;
                    //for(int data = 0; data < dataSensor.Length; data++)
                    //{
                    //    sumData += (byte)dataSensor[data];
                    //}  

                    //Panjang Data              Container Team ID Container Team ID           Container Mission Time Container Packet Count
                    if (splitData.Length == 34 && splitData[0] == "1010" && splitData[0].Length == 4 && splitData[1].Length == 11 && splitData[2].Length <= 4 &&

                        //  Packet Type            Mode                        TP Release                
                        splitData[3] == "C" && splitData[4].Length == 1 && splitData[5].Length == 1 &&

                        //  GPS Time                     GPS Latitude                 GPS Longitude                GPS Satelite                 
                        splitData[9].Length == 8 && splitData[10].Length == 7 && splitData[11].Length == 8 && splitData[13].Length <= 3 &&

                        //  TP Team ID                 TP Mission Time            TP Packet Count            
                        splitData[16].Length == 4 && splitData[17].Length == 11 && splitData[18].Length <= 4 && splitData[19] == "T")
                    {
                        try
                        {
                            //CheckTelemetryData();
                            //WriteLogContainer();
                            //WriteLogPayload();
                            //ConDataLog();
                            //PayDataLog();
                        }
                        catch (Exception ex)
                        {
                            //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                            //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    //Pengecekan Container
                    //  Panjang Data              Container Team ID         Container Team ID           Container Mission Time      Container Packet Count         
                    else if (splitData.Length == 16 && splitData[0] == "1010" && splitData[0].Length == 4 && splitData[1].Length == 11 && splitData[2].Length <= 4 &&

                         //  Packet Type            Mode                        TP Release                
                         splitData[3] == "C" && splitData[4].Length == 1 && splitData[5].Length == 1 &&

                         //  GPS Time                     GPS Latitude                 GPS Longitude                GPS Satelite                 
                         splitData[9].Length == 8 && splitData[10].Length <= 8 && splitData[11].Length <= 8 && splitData[13].Length <= 3)

                    {

                        //if (sumData == checkSumCon)
                        //{
                        try
                        {
                            CheckTelemetryData();
                            WriteLogContainer();
                            ConDataLog();
                        }
                        catch (Exception ex)
                        {
                            //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                            //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    //Pengecekan Payload
                    //  Panjang Data              Payload Team ID         Payload Team ID           Payload Mission Time      Payload Packet Count         
                    else if (splitData.Length == 18 && splitData[0] == "6010" && splitData[0].Length == 4 && splitData[1].Length == 11 && splitData[2].Length <= 4 &&

                             //  Packet Type         //Payload Altitude             
                             splitData[3] == "T" && splitData[4].Length <= 7)

                    {
                        try
                        {
                            CheckTelemetryData();
                            WriteLogPayload();
                            PayDataLog();
                        }
                        catch (Exception ex)
                        {
                            //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                            //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    //Cek cmdEcho
                    else if (splitData.Length == 1 && splitData[0].Length <= 7)

                    {
                        try
                        {
                            CheckTelemetryData();
                        }
                        catch (Exception ex)
                        {
                          
                            return;
                        }
                    }

                    //System.Diagnostics.Debug.WriteLine("VerifyData : splitData Length " + splitData.Length);

                }
                catch (Exception ex)
                {
                    //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                    //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        private void CheckTelemetryData()
        {
            try
            {

                if (splitData.Length == 34)
                {
                    //containerTeamID = Convert.ToDouble(splitData[0]);
                    if (splitData[0].Length > 0 && splitData[0].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        containerTeamID = Convert.ToDouble(splitData[0]);
                    }
                    else
                    {
                        containerTeamID = 0;
                    }

                    containerMissionTime = splitData[1];

                    //containerPacketCount = Convert.ToDouble(splitData[2]);
                    if (splitData[2].Length > 0 && splitData[2].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        containerPacketCount = Convert.ToDouble(splitData[2]);
                    }
                    else
                    {
                        containerPacketCount = 0;
                    }

                    //containerPacketType = splitData[3];
                    if (splitData[3].Length > 0 && splitData[3].All(Char.IsLetter))
                    {
                        containerPacketType = splitData[3];
                    }
                    else
                    {
                        containerPacketType = "";
                    }
                    mode = splitData[4];
                    tpReleased = splitData[5];

                    //altitude = Convert.ToDouble(splitData[6]);
                    if (splitData[6].Length > 0 && splitData[6].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        altitude = Convert.ToDouble(splitData[6]);
                    }
                    else
                    {
                        altitude = 0;
                    }

                    //temperature = Convert.ToDouble(splitData[7]);
                    if (splitData[7].Length > 0 && splitData[7].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        temperature = Convert.ToDouble(splitData[7]);
                    }
                    else
                    {
                        temperature = 0;
                    }

                    //voltage = Convert.ToDouble(splitData[8]);
                    if (splitData[8].Length > 0 && splitData[8].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        voltage = Convert.ToDouble(splitData[8]);
                    }
                    else
                    {
                        voltage = 0;
                    }

                    gpsTime = splitData[9];

                    gpsLatitude = Convert.ToDouble(splitData[10]);

                    if (splitData[11].Length > 0 && splitData[11].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gpsLongitude = Convert.ToDouble(splitData[11]);
                    }
                    else
                    {
                        gpsLongitude = 0;
                    }

                    //gpsAltitude = Convert.ToDouble(splitData[12]);
                    if (splitData[12].Length > 0 && splitData[12].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gpsAltitude = Convert.ToDouble(splitData[12]);
                    }
                    else
                    {
                        gpsAltitude = 0;
                    }

                    //gpsSatelite = Convert.ToDouble(splitData[13]);
                    if (splitData[13].Length > 0 && splitData[13].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gpsSatelite = Convert.ToDouble(splitData[13]);
                    }
                    else
                    {
                        gpsSatelite = 0;
                    }

                    softwareState = splitData[14];
                    //if (splitData[14].Length > 0 && splitData[14].All(Char.IsLetter))
                    //{
                    //    softwareState = splitData[14];
                    //}
                    //else
                    //{
                    //    softwareState = "";
                    //}

                    cmdEcho = splitData[15];

                    //Payload
                    //payloadTeamID = Convert.ToDouble(splitData[16]);
                    if (splitData[16].Length > 0 && splitData[16].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadTeamID = Convert.ToDouble(splitData[16]);
                    }
                    else
                    {
                        payloadTeamID = 0;
                    }

                    payloadMissionTime = splitData[17];

                    //payloadPacketCount = Convert.ToDouble(splitData[18]);
                    if (splitData[18].Length > 0 && splitData[18].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadPacketCount = Convert.ToDouble(splitData[18]);
                    }
                    else
                    {
                        payloadPacketCount = 0;
                    }

                    payloadPacketType = splitData[19];

                    //payloadAltitude = Convert.ToDouble(splitData[20]);
                    if (splitData[20].Length > 0 && splitData[20].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAltitude = Convert.ToDouble(splitData[20]);
                    }
                    else
                    {
                        payloadAltitude = 0;
                    }

                    //payloadTemperature = Convert.ToDouble(splitData[21]);
                    if (splitData[21].Length > 0 && splitData[21].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadTemperature = Convert.ToDouble(splitData[21]);
                    }
                    else
                    {
                        payloadTemperature = 0;
                    }

                    //payloadVoltage = Convert.ToDouble(splitData[22]);
                    if (splitData[22].Length > 0 && splitData[22].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadVoltage = Convert.ToDouble(splitData[22]);
                    }
                    else
                    {
                        payloadVoltage = 0;
                    }

                    //payloadGyro_R = Convert.ToDouble(splitData[23]);
                    if (splitData[23].Length > 0 && splitData[23].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadGyro_R = Convert.ToDouble(splitData[23]);
                    }
                    else
                    {
                        payloadGyro_R = 0;
                    }

                    //payloadGyro_P = Convert.ToDouble(splitData[24]);
                    if (splitData[24].Length > 0 && splitData[24].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadGyro_P = Convert.ToDouble(splitData[24]);
                    }
                    else
                    {
                        payloadGyro_P = 0;
                    }

                    //payloadGyro_Y = Convert.ToDouble(splitData[25]);
                    if (splitData[25].Length > 0 && splitData[25].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadGyro_Y = Convert.ToDouble(splitData[25]);
                    }
                    else
                    {
                        payloadGyro_Y = 0;
                    }

                    //payloadAccel_R = Convert.ToDouble(splitData[26]);
                    if (splitData[26].Length > 0 && splitData[26].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAccel_R = Convert.ToDouble(splitData[26]);
                    }
                    else
                    {
                        payloadAccel_R = 0;
                    }

                    //payloadAccel_P = Convert.ToDouble(splitData[27]);
                    if (splitData[27].Length > 0 && splitData[27].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAccel_P = Convert.ToDouble(splitData[27]);
                    }
                    else
                    {
                        payloadAccel_P = 0;
                    }

                    //payloadAccel_Y = Convert.ToDouble(splitData[28]);
                    if (splitData[28].Length > 0 && splitData[28].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAccel_Y = Convert.ToDouble(splitData[28]);
                    }
                    else
                    {
                        payloadAccel_Y = 0;
                    }

                    //payloadMag_R = Convert.ToDouble(splitData[29]);
                    if (splitData[29].Length > 0 && splitData[29].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadMag_R = Convert.ToDouble(splitData[29]);
                    }
                    else
                    {
                        payloadMag_R = 0;
                    }

                    //payloadMag_P = Convert.ToDouble(splitData[30]);
                    if (splitData[30].Length > 0 && splitData[30].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadMag_P = Convert.ToDouble(splitData[30]);
                    }
                    else
                    {
                        payloadMag_P = 0;
                    }

                    //payloadMag_Y = Convert.ToDouble(splitData[31]);
                    if (splitData[31].Length > 0 && splitData[31].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadMag_Y = Convert.ToDouble(splitData[31]);
                    }
                    else
                    {
                        payloadMag_Y = 0;
                    }

                    //payloadPointingError = Convert.ToDouble(splitData[32]);
                    if (splitData[32].Length > 0 && splitData[32].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadPointingError = Convert.ToDouble(splitData[32]);
                    }
                    else
                    {
                        payloadPointingError = 0;
                    }

                    payloadSoftwareState = splitData[33];
                    //if (splitData[33].Length > 0 && splitData[33].All(Char.IsLetter))
                    //{
                    //    payloadSoftwareState = splitData[33];
                    //}
                    //else
                    //{
                    //    payloadSoftwareState = "";
                    //}


                    //Pengecekan Telemetry Container
                }
                else if (splitData.Length == 16)
                {

                    //containerTeamID = Convert.ToDouble(splitData[0]);
                    if (splitData[0].Length > 0 && splitData[0].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        containerTeamID = Convert.ToDouble(splitData[0]);
                    }
                    else
                    {
                        containerTeamID = 0;
                    }

                    containerMissionTime = splitData[1];

                    //containerPacketCount = Convert.ToDouble(splitData[2]);
                    if (splitData[2].Length > 0 && splitData[2].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        containerPacketCount = Convert.ToDouble(splitData[2]);
                    }
                    else
                    {
                        containerPacketCount = 0;
                    }

                    //containerPacketType = splitData[3];
                    if (splitData[3].Length > 0 && splitData[3].All(Char.IsLetter))
                    {
                        containerPacketType = splitData[3];
                    }
                    else
                    {
                        containerPacketType = "";
                    }
                    mode = splitData[4];
                    tpReleased = splitData[5];

                    //altitude = Convert.ToDouble(splitData[6]);
                    if (splitData[6].Length > 0 && splitData[6].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        altitude = Convert.ToDouble(splitData[6]);
                    }
                    else
                    {
                        altitude = 0;
                    }

                    //temperature = Convert.ToDouble(splitData[7]);
                    if (splitData[7].Length > 0 && splitData[7].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        temperature = Convert.ToDouble(splitData[7]);
                    }
                    else
                    {
                        temperature = 0;
                    }

                    //voltage = Convert.ToDouble(splitData[8]);
                    if (splitData[8].Length > 0 && splitData[8].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        voltage = Convert.ToDouble(splitData[8]);
                    }
                    else
                    {
                        voltage = 0;
                    }

                    gpsTime = splitData[9];

                    gpsLatitude = Convert.ToDouble(splitData[10]);

                    if (splitData[11].Length > 0 && splitData[11].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gpsLongitude = Convert.ToDouble(splitData[11]);
                    }
                    else
                    {
                        gpsLongitude = 0;
                    }

                    //gpsAltitude = Convert.ToDouble(splitData[12]);
                    if (splitData[12].Length > 0 && splitData[12].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gpsAltitude = Convert.ToDouble(splitData[12]);
                    }
                    else
                    {
                        gpsAltitude = 0;
                    }

                    //gpsSatelite = Convert.ToDouble(splitData[13]);
                    if (splitData[13].Length > 0 && splitData[13].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gpsSatelite = Convert.ToDouble(splitData[13]);
                    }
                    else
                    {
                        gpsSatelite = 0;
                    }

                    softwareState = splitData[14];
                    //if (splitData[14].Length > 0 && splitData[14].All(Char.IsLetter))
                    //{
                    //    softwareState = splitData[14];
                    //}
                    //else
                    //{
                    //    softwareState = "";
                    //}

                    cmdEcho = splitData[15];

                }


                //Pengecekan Telemetry Payload
                else if (splitData.Length == 18)
                {
                    //payloadTeamID = Convert.ToDouble(splitData[0]);
                    if (splitData[0].Length > 0 && splitData[0].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadTeamID = Convert.ToDouble(splitData[0]);
                    }
                    else
                    {
                        payloadTeamID = 0;
                    }

                    payloadMissionTime = splitData[1];

                    //payloadPacketCount = Convert.ToDouble(splitData[2]);
                    if (splitData[2].Length > 0 && splitData[2].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadPacketCount = Convert.ToDouble(splitData[2]);
                    }
                    else
                    {
                        payloadPacketCount = 0;
                    }

                    payloadPacketType = splitData[3];

                    //payloadAltitude = Convert.ToDouble(splitData[4]);
                    if (splitData[4].Length > 0 && splitData[4].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAltitude = Convert.ToDouble(splitData[4]);
                    }
                    else
                    {
                        payloadAltitude = 0;
                    }

                    //payloadTemperature = Convert.ToDouble(splitData[5]);
                    if (splitData[5].Length > 0 && splitData[5].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadTemperature = Convert.ToDouble(splitData[5]);
                    }
                    else
                    {
                        payloadTemperature = 0;
                    }

                    //payloadVoltage = Convert.ToDouble(splitData[6]);
                    if (splitData[6].Length > 0 && splitData[6].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadVoltage = Convert.ToDouble(splitData[6]);
                    }
                    else
                    {
                        payloadVoltage = 0;
                    }

                    //payloadGyro_R = Convert.ToDouble(splitData[7]);
                    if (splitData[7].Length > 0 && splitData[7].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadGyro_R = Convert.ToDouble(splitData[7]);
                    }
                    else
                    {
                        payloadGyro_R = 0;
                    }

                    //payloadGyro_P = Convert.ToDouble(splitData[8]);
                    if (splitData[8].Length > 0 && splitData[8].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadGyro_P = Convert.ToDouble(splitData[8]);
                    }
                    else
                    {
                        payloadGyro_P = 0;
                    }

                    //payloadGyro_Y = Convert.ToDouble(splitData[9]);
                    if (splitData[9].Length > 0 && splitData[9].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadGyro_Y = Convert.ToDouble(splitData[9]);
                    }
                    else
                    {
                        payloadGyro_Y = 0;
                    }

                    //payloadAccel_R = Convert.ToDouble(splitData[10]);
                    if (splitData[10].Length > 0 && splitData[10].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAccel_R = Convert.ToDouble(splitData[10]);
                    }
                    else
                    {
                        payloadAccel_R = 0;
                    }

                    //payloadAccel_P = Convert.ToDouble(splitData[11]);
                    if (splitData[11].Length > 0 && splitData[11].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAccel_P = Convert.ToDouble(splitData[11]);
                    }
                    else
                    {
                        payloadAccel_P = 0;
                    }

                    //payloadAccel_Y = Convert.ToDouble(splitData[12]);
                    if (splitData[12].Length > 0 && splitData[12].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadAccel_Y = Convert.ToDouble(splitData[12]);
                    }
                    else
                    {
                        payloadAccel_Y = 0;
                    }

                    //payloadMag_R = Convert.ToDouble(splitData[13]);
                    if (splitData[13].Length > 0 && splitData[13].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadMag_R = Convert.ToDouble(splitData[13]);
                    }
                    else
                    {
                        payloadMag_R = 0;
                    }

                    //payloadMag_P = Convert.ToDouble(splitData[14]);
                    if (splitData[14].Length > 0 && splitData[14].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadMag_P = Convert.ToDouble(splitData[14]);
                    }
                    else
                    {
                        payloadMag_P = 0;
                    }

                    //payloadMag_Y = Convert.ToDouble(splitData[15]);
                    if (splitData[15].Length > 0 && splitData[15].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadMag_Y = Convert.ToDouble(splitData[15]);
                    }
                    else
                    {
                        payloadMag_Y = 0;
                    }

                    //payloadPointingError = Convert.ToDouble(splitData[16]);
                    if (splitData[16].Length > 0 && splitData[16].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        payloadPointingError = Convert.ToDouble(splitData[16]);
                    }
                    else
                    {
                        payloadPointingError = 0;
                    }

                    payloadSoftwareState = splitData[17];
                    //if (splitData[17].Length > 0 && splitData[17].All(Char.IsLetter))
                    //{
                    //    payloadSoftwareState = splitData[17];
                    //}
                    //else
                    //{
                    //    payloadSoftwareState = "";
                    //}
                }
                else if (splitData.Length == 1)
                {
                    cmdEcho = splitData[0];
                }


                    // checkSum = Convert.ToDouble(splitData[34]);

                    //for 3d Modelling
                    foreach (HelixToolkit.Wpf.Polygon polygon in polygons)
                {
                    //polygon.Transformation.RotateX = (float)payloadGyro_R;
                    //polygon.Transformation.RotateY = (float)payloadGyro_P;
                    //polygon.Transformation.RotateZ = (float)payloadGyro_Y;
                }

                maxVoltage = voltage;
                if (Math.Abs(maxVoltage) > this.maxMaxVoltage)
                {
                    maxMaxVoltage = Math.Abs(maxVoltage);
                }

                maxAltitude = altitude;
                if (Math.Abs(maxAltitude) > this.maxMaxAltitude)
                {
                    maxMaxAltitude = Math.Abs(maxAltitude);
                }

                maxTemperature = temperature;
                if (Math.Abs(maxTemperature) > this.maxMaxTemperature)
                {
                    maxMaxTemperature = Math.Abs(maxTemperature);
                }

                containerMaxPacketCount = containerPacketCount;
                if (Math.Abs(containerMaxPacketCount) > this.containerMaxMaxPacketCount)
                {
                    containerMaxMaxPacketCount = Math.Abs(containerMaxPacketCount);
                }

                this.Dispatcher.BeginInvoke(new uiupdater(ShowTelemetryData));
                //this.Dispatcher.Invoke(new uiupdater(GraphCurve));
                GraphCurve();
                //this.Dispatcher.BeginInvoke(new uiupdater(GmapView_Refresh));
                this.Dispatcher.BeginInvoke(new uiupdater(GmapView_Region));
                //Dispatcher.BeginInvoke((Action)(() => GmapView.Refresh()));
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        internal void ShowTelemetryData()
        {
            missionTimeLabel.Content = String.Format("{00:00:00.00}", containerMissionTime);
            softwareStateLabel.Content = String.Format("{0}", softwareState);

            #region 3D Item

            String payloadGyro_R_formated = String.Format("{0:0.00}", payloadGyro_R);
            String payloadGyro_P_formated = String.Format("{0:0.00}", payloadGyro_P);
            String payloadGyro_Y_formated = String.Format("{0:0.00}", payloadGyro_Y);

            //Magnetometer
            RollValue.Content = payloadGyro_R_formated;
            PitchValue.Content = payloadGyro_P_formated;
            YawValue.Content = payloadGyro_Y_formated;
            RPY_TextBox.AppendText(payloadGyro_R_formated + "," + payloadGyro_P_formated + "," + payloadGyro_Y_formated + "\n");
            RPY_TextBox.SelectionLength = RPY_TextBox.Text.Length;
            RPY_TextBox.ScrollToEnd();

            //Vector3D axis;

            //if (payloadGyro_R != 0 && payloadGyro_P != 0 && payloadGyro_Y != 0)
            //{
            //    axis = new Vector3D((float)payloadGyro_R, (float)payloadGyro_P, (float)payloadGyro_Y);
            //}
            //else
            //{
            //    axis = new Vector3D(0, 0, 0);
            //}
            //var angle = 50;

            //Matrix3D matrix = model.Content.Transform.Value;

            //matrix.Rotate(new Quaternion(axis, angle));

            //model.Content.Transform = new MatrixTransform3D(matrix);

            #endregion

            #region Graph Item

            //Payload
            PayloadPacketCountValue.Content = String.Format("{0:0000}", payloadPacketCount);
            PayloadAltitudeValue.Content = String.Format("{0:000.0}", payloadAltitude);
            PayloadTemperatureValue.Content = String.Format("{0:00.0}", payloadTemperature);
            PayloadVoltageValue.Content = String.Format("{0:0.0}", payloadVoltage);

            //Container
            ContainerAltitudeGraph.Content = String.Format("{0:000.0}", altitude);
            PayloadAltitudeGraph.Content = String.Format("{0:000.0}", payloadAltitude);
            GPSAltitudeGraph.Content = String.Format("{0:000.0}", gpsAltitude);
            ContainerTemperatureGraph.Content = String.Format("{0:00.0}", temperature);
            PayloadTemperatureGraph.Content = String.Format("{0:00.0}", payloadTemperature);

            //Voltage
            ContainerVoltageGraph.Content = String.Format("{0:0.0}", voltage);

            //Accelerometer
            PayloadAccelerometer_R.Content = String.Format("{0:0.00}", payloadAccel_R);
            PayloadAccelerometer_P.Content = String.Format("{0:0.00}", payloadAccel_P);
            PayloadAccelerometer_Y.Content = String.Format("{0:0.00}", payloadAccel_Y);

            //Gyrometer
            PayloadGyrometer_R.Content = String.Format("{0:0.00}", payloadGyro_R);
            PayloadGyrometer_P.Content = String.Format("{0:0.00}", payloadGyro_P);
            PayloadGyrometer_Y.Content = String.Format("{0:0.00}", payloadGyro_Y);

            //Magnetometer
            PayloadMagnetometer_R.Content = String.Format("{0:0.00}", payloadMag_R);
            PayloadMagnetometer_P.Content = String.Format("{0:0.00}", payloadMag_P);
            PayloadMagnetometer_Y.Content = String.Format("{0:0.00}", payloadMag_Y);

            #endregion

            try
            {
                #region Dashboard Item

                teamIDLabel.Content = String.Format("{0:0000}", containerTeamID);
                //missionTimeLabel.Text = DateTime.Now.ToString("hh:mm:ss.ss");
                //missionTimeLabel.Text = String.Format("{00:00:00.00}", containerMissionTime);
                altitudeValue.Content = String.Format("{0:000.0}", altitude);
                altitudeMaxValue.Content = String.Format("{0:000.0}", maxMaxAltitude);
                voltageValue.Content = String.Format("{0:0.00}", voltage);
                temperatureValue.Content = String.Format("{0:00.0}", temperature);
                containerPacketCountValue.Content = String.Format("{0:0000}", containerPacketCount);
                GPSLatitudeValue.Content = String.Format("{0:0.0000}", gpsLatitude);
                GPSLongitudeValue.Content = String.Format("{0:000.0000}", gpsLongitude);
                GPSAltitudeValue.Content = String.Format("{0:000.0}", gpsAltitude);
                GPSSatsValue.Content = String.Format("{0:00}", gpsSatelite);
                GPSTimeValue.Content = String.Format("{0:00:00.00}", gpsTime);
                //softwareStateLabel.Text = String.Format("{0}", softwareState);
                CMDEchoLabel.Content = String.Format("{0}", cmdEcho);

                //Payload
                PayloadPacketCountValue.Content = String.Format("{0:0000}", payloadPacketCount);
                PayloadTemperatureValue.Content = String.Format("{0:00.0}", payloadTemperature);
                PayloadAltitudeValue.Content = String.Format("{0:000.0}", payloadAltitude);
                PayloadVoltageValue.Content = String.Format("{0:0.00}", payloadVoltage);
                payloadSoftwareStateLabel.Content = String.Format("{0}", payloadSoftwareState);

                //accelerometer
                ACCEL_RValue.Content = String.Format("{0:0.000}", payloadAccel_R);
                ACCEL_PValue.Content = String.Format("{0:0.000}", payloadAccel_P);
                ACCEL_YValue.Content = String.Format("{0:0.000}", payloadAccel_Y);


                //gyro
                GYRO_RValue.Content = String.Format("{0:0.000}", payloadGyro_R);
                GYRO_PValue.Content = String.Format("{0:0.000}", payloadGyro_P);
                GYRO_YValue.Content = String.Format("{0:0.000}", payloadGyro_Y);

                //magnetometer
                MAG_RValue.Content = String.Format("{0:0.000}", payloadMag_R);
                MAG_PValue.Content = String.Format("{0:0.000}", payloadMag_P);
                MAG_YValue.Content = String.Format("{0:0.000}", payloadMag_Y);

                //pointing error
                pointingErrorValue.Content = String.Format("{0:0.0}", payloadPointingError);
                #endregion

                #region Map Item
                
                #endregion

                PayloadReleased.Content = String.Format("{0}", tpReleased);
                if (tpReleased == "R")
                {
                    PayloadReleased.Content = "R";
                    PayloadReleasedPane.Background = System.Windows.Media.Brushes.ForestGreen;
                }
                else if (tpReleased == "N")
                {
                    PayloadReleased.Content = "N";
                    PayloadReleasedPane.Background = System.Windows.Media.Brushes.Firebrick;
                }

                if (cmdEcho == "SIMENABLE" && mode == "F")
                {
                    FlightOnOffSwitch.IsChecked = false;

                    SimulationStatus.Content = "ENABLE";
                    SimulationStatus.Background = System.Windows.Media.Brushes.ForestGreen;
                }
                else if (cmdEcho == "SIMACTIVATE" && mode == "S")
                {
                    FlightOnOffSwitch.IsChecked = false;

                    SimulationStatus.Content = "ACTIVE";
                    SimulationStatus.Background = System.Windows.Media.Brushes.ForestGreen;
                }
                else if (cmdEcho == "SIMDISABLE" && mode == "F")
                {
                    FlightOnOffSwitch.IsChecked = false;

                    SimulationStatus.Content = "DISABLE";
                    SimulationStatus.Background = System.Windows.Media.Brushes.Firebrick;
                }
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            catch (FormatException)
            {
                return;
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        #endregion
        //Masih ada yg di comment

        #region Log Data CSV Configuration

        private void LogDataContainerSwitch_Checked(object sender, RoutedEventArgs e)
        {
            //if (serialport.IsOpen == true)
            //{
            //    try
            //    {
            //        BackgroundWorker worker1 = new BackgroundWorker();

            //        if (LogDataContainerSwitch.IsChecked == true)
            //        {
            //            try
            //            {
            //                worker1.DoWork += delegate (object s, DoWorkEventArgs args)
            //                {
            //                    containerLogStart = true;
            //                    logDataCountCon = 0;
            //                    clickCon++;
            //                    containerFileLog = binAppPath + "\\LogData\\FLIGHT_1010_C_"+ System.DateTime.Today.ToString("MM-dd-yyyy") + ".csv";
            //                    if (File.Exists(containerFileLog))
            //                    {
            //                        File.Delete(containerFileLog);
            //                    }
            //                    else
            //                    {
            //                        var fileCon = File.Open(containerFileLog, (FileMode)FileIOPermissionAccess.Append, FileAccess.Write, FileShare.Read);
            //                        writeCon = new StreamWriter(fileCon, Encoding.GetEncoding(1252));
            //                        writeCon.AutoFlush = true;
            //                        writeCon.Write("Team ID, Mission Time, Packet Count, Packet Type, Mode, TP Released, Altitude, Temperature, " +
            //                                         "Voltage, GPS Time, GPS Latitude, GPS Longitude, GPS Altitude, GPS Satellites, " +
            //                                         "Software State, CMD Echo \n");
            //                    }
            //                };
            //                worker1.RunWorkerAsync();
            //            }
            //            catch (NullReferenceException)
            //            {
            //                return;
            //            }
            //            catch (IndexOutOfRangeException)
            //            {
            //                return;
            //            }
            //            catch (FormatException)
            //            {
            //                return;
            //            }
            //            catch
            //            {
            //                return;
            //            }
            //        }
            //        else
            //        {
            //            containerLogStart = false;
            //            writeCon.Flush();
            //            writeCon.Close();
            //            worker1.CancelAsync();
            //        }
            //    }
            //    catch (NullReferenceException)
            //    {
            //        return;
            //    }
            //    catch (IndexOutOfRangeException)
            //    {
            //        return;
            //    }
            //    catch (FormatException)
            //    {
            //        return;
            //    }
            //    catch
            //    {
            //        return;
            //    }
            //}
        }

        private void LogDataPayloadSwitch_Checked(object sender, RoutedEventArgs e)
        {
            //if (serialport.IsOpen == true)
            //{
            //    try
            //    {
            //        BackgroundWorker worker = new BackgroundWorker();

            //        if (LogDataPayloadSwitch.IsChecked == true)
            //        {
            //            try
            //            {
            //                worker.DoWork += delegate (object s, DoWorkEventArgs args)
            //                {
            //                    payloadLogStart = true;
            //                    logDataCountTP = 0;
            //                    //LogDataPayload1Indicator.BackColor = Color.FromArgb(52, 168, 83);
            //                    clickTP++;
            //                    payloadFileLog = binAppPath + "\\LogData\\FLIGHT_1010_T_"+ System.DateTime.Today.ToString("MM-dd-yyyy") + ".csv";
            //                    if (File.Exists(payloadFileLog))
            //                    {
            //                        File.Delete(payloadFileLog);
            //                    }
            //                    else
            //                    {
            //                        var filePayload = File.Open(payloadFileLog, (FileMode)FileIOPermissionAccess.Append, FileAccess.Write, FileShare.Read);
            //                        writePay = new StreamWriter(filePayload, Encoding.GetEncoding(1252));
            //                        writePay.AutoFlush = true;
            //                        writePay.Write("Team ID, Mission Time, Packet Count, Packet Type, TP Altitude, TP Temperature, TP Voltage," +
            //                                       "Gyro R, Gyro P, Gyro Y, Accel R, Accel P, Accel Y, Mag R, Mag P, Mag Y," +
            //                                       "Pointing Error, TP Software State \n");
            //                    }
            //                };
            //                worker.RunWorkerAsync();
            //            }
            //            catch (NullReferenceException)
            //            {
            //                return;
            //            }
            //            catch (IndexOutOfRangeException)
            //            {
            //                return;
            //            }
            //            catch (FormatException)
            //            {
            //                return;
            //            }
            //            catch
            //            {
            //                return;
            //            }
            //        }

            //        else
            //        {
            //            //LogDataPayload1Indicator.BackColor = Color.FromArgb(234, 67, 53);
            //            payloadLogStart = false;
            //            writePay.Flush();
            //            writePay.Close();
            //            worker.CancelAsync();
            //        }
            //    }
            //    catch (NullReferenceException)
            //    {
            //        return;
            //    }
            //    catch (IndexOutOfRangeException)
            //    {
            //        return;
            //    }
            //    catch (FormatException)
            //    {
            //        return;
            //    }
            //    catch
            //    {
            //        return;
            //    }
            //}
            //else
            //{

            //}
        }

        private void ConDataLog()
        {
            if (serialport.IsOpen == true)
            {
                try
                {
                    if (true)
                    {
                        try
                        {
                            BackgroundWorker worker1 = new BackgroundWorker();
                            worker1.DoWork += delegate (object s, DoWorkEventArgs args)
                            {
                                containerLogStart = true;
                                logDataCountCon = 0;
                                clickCon++;
                                containerFileLog = binAppPath + "\\LogData\\FLIGHT_1010_C.csv";
                                var fileCon = File.Open(containerFileLog, (FileMode)FileIOPermissionAccess.Append, FileAccess.Write, FileShare.Read);
                                writeCon = new StreamWriter(fileCon, Encoding.GetEncoding(1252));
                                writeCon.AutoFlush = true;
                                writeCon.Write("Team ID, Mission Time, Packet Count, Packet Type, Mode, TP Released, Altitude, Temperature, " +
                                                 "Voltage, GPS Time, GPS Latitude, GPS Longitude, GPS Altitude, GPS Satellites, " +
                                                 "Software State, CMD Echo \n");
                            };
                            worker1.RunWorkerAsync();
                        }
                        catch (NullReferenceException)
                        {
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return;
                        }
                        catch (FormatException)
                        {
                            return;
                        }
                        catch
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (containerLogStart == true)
                        {
                            containerLogStart = false;
                            writeCon.Flush();
                            writeCon.Close();
                        }
                        else
                        {

                        }
                    }
                }
                catch (NullReferenceException)
                {
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                catch (FormatException)
                {
                    return;
                }
                catch
                {
                    return;
                }
            }
        }
        
        private void PayDataLog()
        {
            if (serialport.IsOpen == true)
            {
                try
                {
                    if (true)
                    {
                        try
                        {
                            BackgroundWorker worker = new BackgroundWorker();
                            worker.DoWork += delegate (object s, DoWorkEventArgs args)
                            {
                                payloadLogStart = true;
                                logDataCountTP = 0;
                                //LogDataPayload1Indicator.BackColor = Color.FromArgb(52, 168, 83);
                                clickTP++;
                                payloadFileLog = binAppPath + "\\LogData\\FLIGHT_1010_T.csv";
                                var filePayload = File.Open(payloadFileLog, (FileMode)FileIOPermissionAccess.Append, FileAccess.Write, FileShare.Read);
                                writePay = new StreamWriter(filePayload, Encoding.GetEncoding(1252));
                                writePay.AutoFlush = true;
                                writePay.Write("Team ID, Mission Time, Packet Count, Packet Type, TP Altitude, TP Temperature, TP Voltage," +
                                               "Gyro R, Gyro P, Gyro Y, Accel R, Accel P, Accel Y, Mag R, Mag P, Mag Y," +
                                               "Pointing Error, TP Software State \n");
                            };
                            worker.RunWorkerAsync();
                        }
                        catch (NullReferenceException)
                        {
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return;
                        }
                        catch (FormatException)
                        {
                            return;
                        }
                        catch
                        {
                            return;
                        }
                    }

                    else
                    {
                        //LogDataPayload1Indicator.BackColor = Color.FromArgb(234, 67, 53);
                        if (payloadLogStart == true)
                        {
                            payloadLogStart = false;
                            writePay.Flush();
                            writePay.Close();
                        }

                        else
                        {

                        }
                    }
                }
                catch (NullReferenceException)
                {
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                catch (FormatException)
                {
                    return;
                }
                catch
                {
                    return;
                }
            }
            else
            {

            }
        }

        private void WriteLogContainer()
        {

            if (true)
            {
                try
                {
                    if (containerLogStart == true)
                    {
                        try
                        {
                            string dataBuffer = String.Format("{0:0000}", containerTeamID);

                            writeCon.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},\n"
                                             , containerTeamID, containerMissionTime, containerPacketCount, containerPacketType
                                             , mode, tpReleased, altitude, temperature, voltage, gpsTime
                                             , gpsLatitude, gpsLongitude, gpsAltitude, gpsSatelite, softwareState
                                             , cmdEcho);
                            writeCon.Flush();

                            DataTable ContainerDataCsv;
                            DataContainer dtc = new DataContainer();
                            dtc.cteamid = containerTeamID;
                            dtc.cmissiontime = containerMissionTime;
                            dtc.cpacketcount = containerPacketCount;
                            dtc.cpacketype = containerPacketType;
                            dtc.cmode = mode;
                            dtc.ctpreleased = tpReleased;
                            dtc.caltitude = altitude;
                            dtc.ctemperature = temperature;
                            dtc.cvoltage = voltage;
                            dtc.cgpstime = gpsTime;
                            dtc.cgpslatitude = gpsLatitude;
                            dtc.cgpslongtitude = gpsLongitude;
                            dtc.cgpsaltitude = gpsAltitude;
                            dtc.cgpssatelite = gpsSatelite;
                            dtc.csoftstate = softwareState;
                            dtc.ccmdecho = cmdEcho;

                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                ContainerDataCSv.Items.Add(dtc);
                                ContainerDataCSv.ScrollIntoView(ContainerDataCSv.Items[ContainerDataCSv.Items.Count - 1]);
                            }));
                        }
                        catch (NullReferenceException)
                        {
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return;
                        }
                        catch (FormatException)
                        {
                            return;
                        }
                        catch
                        {
                            return;
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                catch (FormatException)
                {
                    return;
                }
                catch
                {
                    return;
                }
            }
        }

        private void WriteLogPayload()
        {
            if (true)
            {
                try
                {
                    if (payloadLogStart == true)
                    {
                        try
                        {
                            string dataBuffer = String.Format("{0:0000}", payloadTeamID);
                            writePay.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},\n"
                                         , payloadTeamID, payloadMissionTime, payloadPacketCount, payloadPacketType
                                         , payloadAltitude, payloadTemperature, payloadVoltage, payloadAccel_R, payloadAccel_P,
                                         payloadAccel_Y, payloadGyro_R, payloadGyro_P, payloadGyro_Y, payloadMag_R, payloadMag_P,
                                         payloadMag_Y, payloadPointingError, payloadSoftwareState);
                            writePay.Flush();
                            DataTable PayloadDataCsv;
                            DataPayload dtp = new DataPayload();
                            dtp.pteamid = payloadTeamID;
                            dtp.pmissiontime = payloadMissionTime;
                            dtp.ppacketcount = payloadPacketCount;
                            dtp.ppacketype = payloadPacketType;
                            dtp.paltitude = payloadAltitude;
                            dtp.ptemperature = payloadTemperature;
                            dtp.pvoltage = payloadVoltage;
                            dtp.paccel_r = payloadAccel_R;
                            dtp.paccel_p = payloadAccel_P;
                            dtp.paccel_y = payloadAccel_Y;
                            dtp.pgyro_r = payloadGyro_R;
                            dtp.pgyro_p = payloadGyro_P;
                            dtp.pgyro_y = payloadGyro_Y;
                            dtp.pmag_r = payloadMag_R;
                            dtp.pmag_p = payloadMag_P;
                            dtp.pmag_y = payloadMag_Y;
                            dtp.ppointingerror = payloadPointingError;
                            dtp.psoftstate = payloadSoftwareState;

                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                PayloadDataCSv.Items.Add(dtp);
                                PayloadDataCSv.ScrollIntoView(PayloadDataCSv.Items[PayloadDataCSv.Items.Count - 1]);
                            }));
                        }
                        catch (NullReferenceException)
                        {
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return;
                        }
                        catch (FormatException)
                        {
                            return;
                        }
                        catch
                        {
                            return;
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                catch (FormatException)
                {
                    return;
                }
                catch
                {
                    return;
                }
            }
        }

        public class DataContainer
        {
            public Double cteamid { get; set; }
            public String cmissiontime { get; set; }
            public Double cpacketcount { get; set; }
            public String cpacketype { get; set; }
            public String cmode { get; set; }
            public String ctpreleased { get; set; }
            public Double caltitude { get; set; }
            public Double ctemperature { get; set; }
            public Double cvoltage { get; set; }
            public String cgpstime { get; set; }
            public Double cgpslatitude { get; set; }
            public Double cgpslongtitude { get; set; }
            public Double cgpsaltitude { get; set; }
            public Double cgpssatelite { get; set; }
            public String csoftstate { get; set; }
            public String ccmdecho { get; set; }
        }

        public class DataPayload
        {
            public Double pteamid { get; set; }
            public String pmissiontime { get; set; }
            public Double ppacketcount { get; set; }
            public String ppacketype { get; set; }
            public Double paltitude { get; set; }
            public Double ptemperature { get; set; }
            public Double pvoltage { get; set; }
            public Double paccel_r { get; set; }
            public Double paccel_p { get; set; }
            public Double paccel_y { get; set; }
            public Double pgyro_r { get; set; }
            public Double pgyro_p { get; set; }
            public Double pgyro_y { get; set; }
            public Double pmag_r { get; set; }
            public Double pmag_p { get; set; }
            public Double pmag_y { get; set; }
            public Double ppointingerror { get; set; }
            public String psoftstate { get; set; }
        }

        #endregion

        #region MQTT

        public void MQTT()
        {
            //if (serialport.IsOpen == true)
            //{
            //    SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\MQTT Publish.wav");
            //    player.Play();

            //    while (mqtt == true)
            //    {
            //        if (softwareState != "TOUCH_DOWN")
            //        {

            //            if (!String.IsNullOrEmpty(dataSensor))
            //            {
            //                try
            //                {
            //                    var pathFile = binAppPath + "\\LogData\\FLIGHT_1010_C.csv";
            //                    var filename = File.Open(pathFile, FileMode.Open, (FileAccess)FileIOPermissionAccess.Read, FileShare.ReadWrite);
            //                    StreamReader reader = new StreamReader(filename);

            //                    var pathFile1 = binAppPath + "\\LogData\\FLIGHT_1010_T.csv";
            //                    var filename1 = File.Open(pathFile1, FileMode.Open, (FileAccess)FileIOPermissionAccess.Read, FileShare.ReadWrite);
            //                    StreamReader reader1 = new StreamReader(filename1);

            //                    while (!reader.EndOfStream)
            //                    {
            //                        string line = reader.ReadLine();

            //                        if (!String.IsNullOrWhiteSpace(line))
            //                        {
            //                            string[] values = line.Split(',');

            //                            if (values.Length > 1)
            //                            {
            //                                if (values[3] == "C")
            //                                {
            //                                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            //                                    client.Publish(topic, Encoding.UTF8.GetBytes(line), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);

            //                                    int f = 0;
            //                                    while (!reader1.EndOfStream && f < 4)
            //                                    {

            //                                        string line1 = reader1.ReadLine();

            //                                        if (!String.IsNullOrWhiteSpace(line1))
            //                                        {
            //                                            string[] values1 = line1.Split(',');

            //                                            if (values1.Length > 1)
            //                                            {
            //                                                if (values1[3] == "T")
            //                                                {
            //                                                    Thread.Sleep(TimeSpan.FromMilliseconds(250));
            //                                                    client.Publish(topic, Encoding.UTF8.GetBytes(line1), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
            //                                                }
            //                                            }
            //                                        }
            //                                        f++;
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
            //                    //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //                    return;
            //                }
            //            }
            //        }
            //        else if (softwareState == "TOUCH_DOWN")
            //        {
            //            mqtt = false;
            //            client.Disconnect();
            //        }
            //    }

            //}
            //else
            //{
            //    mqtt = false;
            //}
        }

        private void MqttBtn_Click(object sender, RoutedEventArgs e)
        {
            //Thread thread = new Thread(MQTT);

            //if (mqtt == true)
            //{
            //    thread.Start();
            //}
            //else
            //{
            //    thread.Abort();
            //}
        }

        #endregion

        #region Command

        private void CMDTextBox_KeyPress(object sender, System.Windows.Input.KeyEventArgs e)
        {
            StringCollection Lines = new StringCollection();

            if (serialport.IsOpen == true)
            {
                serialport.DiscardInBuffer();
                serialport.DiscardOutBuffer();

                try
                {
                    if (e.Key == Key.Return)
                    {
                        e.Handled = true;

                        try
                        {
                            if (CMDTextBox1.GetLineText(0) == "" && lineCommand == 0)
                            {
                                if (CMDTextBox1.IsFocused == true)
                                {
                                    CMDTextBox1.Clear();
                                }
                                else
                                {
                                    CMDTextBox2.Text += "-----------------------ENTER COMMAND!!!------------------------" + Environment.NewLine;
                                }
                            }
                            else
                            {
                                if (CMDTextBox1.GetLineText(0) == "CMD,1010,CX,ON")
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,CX,ON\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "--------------------Container Telemetry ON---------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Container Telemetry On.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,CX,OFF")
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,CX,OFF\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor; ;

                                        CMDTextBox2.Text += "\r\n" + "--------------------Container Telemetry OFF--------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Container Telemetry Off.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,TP,ON" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,TP,ON\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "--------------------Payload Telemetry ON---------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Payload Telemetry On.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,TP,OFF" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,TP,OFF\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "--------------------Payload Telemetry OFF--------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Payload Telemetry Off.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }

                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,SIM,ENABLE" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,SIM,ENABLE\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "--------------------Simulation Mode Enabled--------------------" + "\r\n";

                                        SimulationStatus.Content = "ENABLE";
                                        SimulationStatusPane.Background = System.Windows.Media.Brushes.ForestGreen;

                                        FlightOnOffSwitch.IsChecked = false;

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Simulation Enable.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,SIM,ACTIVATE" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,SIM,ACTIVATE\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "-------------------Simulation Mode Activated-------------------" + "\r\n";

                                        FlightOnOffSwitch.IsChecked = false;

                                        SimulationStatus.Content = "ACTIVATE";
                                        SimulationStatusPane.Background = System.Windows.Media.Brushes.ForestGreen;

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Simulation Activate.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,SIM,DISABLE" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,SIM,DISABLE\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "-------------------Simulation Mode Disabled--------------------" + "\r\n";

                                        SimulationStatus.Content = "DISABLE";
                                        SimulationStatusPane.Background = System.Windows.Media.Brushes.Firebrick;

                                        FlightOnOffSwitch.IsChecked = true;

                                        timerSimulation.Stop();
                                        timerCSV = 0;

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Simulation Disable.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,SIMP,PRESSURE" && lineCommand == 0)
                                {
                                    try
                                    {
                                        CMDTextBox2.Text += "\r\n" + "-----------------------Pressure Simulated------------------------" + "\r\n";

                                        FlightOnOffSwitch.IsChecked = false;

                                        SimulationStatus.Content = "ACTIVATE";
                                        SimulationStatusPane.Background = System.Windows.Media.Brushes.ForestGreen;

                                        SendCSV();

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Pressure Simulated.wav");
                                        player.Play();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,ST,UTCTIME" && lineCommand == 0)
                                {
                                    try
                                    {
                                        MissionTime();

                                        string cmd = "CMD,1010,ST," + totalWaktu + "\r";
                                        serialport.Write(cmd);
                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "-------------------TIME SET TO " + totalWaktu + "--------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Set UTC Time.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,CR,RESET" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,CR,RESET\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "------------------------COUNT RESET----------------------------" + "\r\n";

                                        timerSimulation.Stop();
                                        timerCSV = 0;

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Container Count Reset.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,TES,ON" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,TES,ON\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "------------------------TES ON----------------------------" + "\r\n";
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,TES,OFF" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,TES,OFF\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "------------------------TES OFF----------------------------" + "\r\n";
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,CAL" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,CAL\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "------------------------CALIBRATION----------------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\Calibration.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,SPL" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,SPL\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "------------------------SPOOLING----------------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\spooling.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "CMD,1010,UPL" && lineCommand == 0)
                                {
                                    try
                                    {
                                        string cmd = "CMD,1010,UPL\r";

                                        serialport.Write(cmd);

                                        stepData = Sequencer.readSensor;

                                        CMDTextBox2.Text += "\r\n" + "------------------------UNSPOOLING----------------------------" + "\r\n";

                                        SoundPlayer player = new SoundPlayer(binAppPath + "\\Audio\\unspooling.wav");
                                        player.Play();
                                        CMDTextBox1.Clear();
                                    }
                                    catch
                                    {
                                        return;
                                    }
                                }
                                else if (CMDTextBox1.GetLineText(0) == "Q" && lineCommand == 0)
                                {
                                    CMDTextBox2.Clear();
                                }
                                else
                                {
                                    #region messageBox
                                    errormsgbx();
                                    TBContent.Text = "Error!";
                                    TBContent1.Text = "Command Not Found!";
                                    #endregion
                                    return;
                                }
                            }
                        }
                        catch (NullReferenceException)
                        {
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return;
                        }
                        catch (FormatException)
                        {
                            return;
                        }
                        catch
                        {
                            return;
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    return;
                }
                catch (IndexOutOfRangeException)
                {
                    return;
                }
                catch (FormatException)
                {
                    return;
                }
                catch
                {
                    return;
                }
            }
        }

        #endregion

        #region Timer

        private void timerSimulation_Tick(object sender, EventArgs e)
        {
            try
            {
                string cmd = "CMD,1010,SIMP," + Col4[timerCSV] + "\r";
                serialport.Write(cmd);
                stepData = Sequencer.readSensor;

                SerialControlTextBox.Text += "CMD,1010,SIMP," + Col4[timerCSV] + "\n";
            }
            catch (Exception ex)
            {
                return;
            }
            timerCSV++;
        }

        #endregion

        #region 3D Model

        private void HelixViewport3D_Loaded(object sender, RoutedEventArgs e)
        {
            //ModelImporter import = new ModelImporter();
            //Model3DGroup model1 = import.Load(fileobj);
            //model.Content = model1;
        }

        #endregion

    }
}
