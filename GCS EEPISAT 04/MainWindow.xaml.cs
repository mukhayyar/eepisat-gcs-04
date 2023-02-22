using System;
using System.Collections.Generic;
using System.Data;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Device.Location;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Media;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Management;

// Map
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.ObjectModel;
using GMap.NET.WindowsForms.ToolTips;

// Graph
using ScottPlot;
using ScottPlot.Styles;
using System.Security.Permissions;

// 3D
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Windows.Media;

using Emoji;
using System.Numerics;
using System.Windows.Media.Animation;

namespace GCS_EEPISAT_04
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly SerialPort _serialPort = new();
        enum Sequencer { readSensor };
        string dataSensor;

        public delegate void uiupdater();

        int dataSum = 0;
        bool isAscii = false;
        string[] splitData;

        public delegate void AddDataDelegate(String myString);

        public AddDataDelegate WriteTelemetryData;

        StreamWriter writePay;
        string payloadFileLog = "";

        // bin App path
        readonly string binAppPath = System.AppDomain.CurrentDomain.BaseDirectory;


        public static GMapMarker mapMarkerPayload;
        public static GMapMarker mapMarkerGCS;
        public static GMapOverlay mapOverlay;

        // Variabel Probe Log
        uint teamId = 1085;
        string state;
        string missionTime;

        uint packetCount;
        uint max_packetCount;
        uint max_max_packetCount;
        char mode; // F & S

        float altitude;
        float max_altitude;
        float max_max_altitude;
        char hs_status;
        char pc_status;
        char mast_status;
        float temperature;
        float max_temperature;
        float max_max_temperature;
        string time_max_max_temperature;
        float pressure;
        float max_pressure;
        float max_max_pressure;
        string time_max_max_pressure;
        float voltage;
        float max_voltage;
        float max_max_voltage;
        string time_max_max_voltage;
        string gps_time;
        float gps_altitude;
        float gps_latitude;
        float gps_longitude;
        uint gps_sats_count;
        float tilt_x;
        float tilt_y;
        string cmd_echo;
        bool checkSumHasil;
        int validCount = 0;
        int corruptCount = 0;

        bool hs_deployed = false;
        bool mast_raised = false;
        bool auto_scroll = false;
        
        int totalPayloadData = 0;
        

        Microsoft.Win32.OpenFileDialog openFileDialog;

        // battery
        static int jumlahDataRegLin;
        static float sumX, sumY, sumX2, sumXY;
        static float x, y, m, c;
        float hasilRegLin;
        readonly int lineCommand = 0;

        // 3D
        Vector3D axis;
        const int angle = 50;

        // graph
        bool graphOnline = false;


        // Variabel CSV Simulation Mode
        readonly List<String> Col4 = new();
        
        int timerCSV = 0;

        
        readonly DispatcherTimer timerSimulation = new();
        readonly DispatcherTimer timergraph = new();

        string fileobj;

        private GeoCoordinateWatcher watcher = null;
        double distance = 0;

        
        public delegate void MethodInvoker();

        // Serial COM PORT
        ManagementEventWatcher detector;
        string SerialPortNumber;
        

        readonly System.Windows.Threading.DispatcherTimer timer = new();
        
        readonly double[] GPSSats = new double[100_000];
        readonly double[] Long = new double[100_000];
        readonly double[] Lat = new double[100_000];
        readonly double[] TiltX = new double[100_000];
        readonly double[] TiltY = new double[100_000];
        readonly double[] Pressure = new double[100_000];
        readonly double[] Temperature = new double[100_000];
        readonly double[] Voltage = new double[100_000];
        readonly double[] PayloadAlt = new double[100_000];
        readonly double[] GPSAlt = new double[100_000];
        
        readonly ScottPlot.Plottable.SignalPlot SignalPlot;
        int NextPointIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            // #region Graph
            

            // #endregion
            SoundPlayer player = new(binAppPath + "/Audio/GCSSTART.wav");
            player.Play();

            watcher = new GeoCoordinateWatcher();
            watcher.Start();


            _serialPort.DataReceived += new SerialDataReceivedEventHandler(Serialport_Datareceive);

            
            timerSimulation.Interval = TimeSpan.FromMilliseconds(1000);
            timerSimulation.Tick += TimerSimulation_Tick;
            timerSimulation.Start();

            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();

            fileobj = System.AppDomain.CurrentDomain.BaseDirectory + "/Assets/3D/Probe_Stowed.obj";

            // CSV
            if (!Directory.Exists(binAppPath + "\\LogData\\"))
            {
                Directory.CreateDirectory(binAppPath + "\\LogData\\");
            }
            if (!Directory.Exists(binAppPath + "\\LogData\\FLIGHT"))
            {
                Directory.CreateDirectory(binAppPath + "\\LogData\\FLIGHT");
            }
            if (!Directory.Exists(binAppPath + "\\LogData\\SIMULATION"))
            {
                Directory.CreateDirectory(binAppPath + "\\LogData\\SIMULATION");
            }

            // setup graph
            SatellitCountPlot.Plot.Style(ScottPlot.Style.Gray1);
            SatellitCountPlot.Plot.Title("GPS Sats");
            PressurePlot.Plot.Style(ScottPlot.Style.Gray1);
            PressurePlot.Plot.Title("Pressure");
            TemperaturePlot.Plot.Style(ScottPlot.Style.Gray1);
            TemperaturePlot.Plot.Title("Temperature");
            VoltagePlot.Plot.Style(ScottPlot.Style.Gray1);
            VoltagePlot.Plot.Title("Voltage");
            AltitudePlot.Plot.Style(ScottPlot.Style.Gray1);
            AltitudePlot.Plot.Title("Altitude");
            TiltPlot.Plot.Style(ScottPlot.Style.Gray1);
            TiltPlot.Plot.Title("Tilt");
            LatLongPlot.Plot.Style(ScottPlot.Style.Gray1);
            LatLongPlot.Plot.Title("Latitude & Longitude");

            SatellitCountPlot.Plot.AddSignal(GPSSats, color: System.Drawing.Color.FromArgb(255, 222, 158, 93), label: "GPS Sats");
            SatellitCountPlot.Plot.Legend();
            SatellitCountPlot.Plot.SetAxisLimits(0, 10, -2, 30);

            PressurePlot.Plot.AddSignal(Pressure, color: System.Drawing.Color.FromArgb(255, 222, 158, 93), label: "Pressure");
            PressurePlot.Plot.Legend();
            PressurePlot.Plot.SetAxisLimits(0, 10, -5, 200);


            TemperaturePlot.Plot.AddSignal(Temperature, color: System.Drawing.Color.FromArgb(255, 222, 158, 93), label: "Temperature");
            TemperaturePlot.Plot.Legend();
            TemperaturePlot.Plot.SetAxisLimits(0, 10, -5, 60);

            VoltagePlot.Plot.AddSignal(Voltage, color: System.Drawing.Color.FromArgb(255, 222, 158, 93), label: "Voltage");
            VoltagePlot.Plot.Legend();
            VoltagePlot.Plot.SetAxisLimits(0, 10, -10, 45);

            TiltPlot.Plot.AddSignal(TiltX, label: "Tilt X");
            TiltPlot.Plot.AddSignal(TiltY, label: "Tilt Y");
            TiltPlot.Plot.Legend();
            TiltPlot.Plot.SetAxisLimits(0, 50, -50, 50);

            LatLongPlot.Plot.AddScatter(Lat, Long, color: System.Drawing.Color.FromArgb(255, 222, 158, 93), markerSize: 0, label: "Long & Lat");
            //LatLongPlot.Plot.AddSignal(Lat, label: "Latitude");
            //LatLongPlot.Plot.AddSignal(Long, label: "Longitude");
            LatLongPlot.Plot.Legend();
            LatLongPlot.Plot.SetAxisLimits(-180, 180, -180, 180);

            SignalPlot = AltitudePlot.Plot.AddSignal(PayloadAlt, color: System.Drawing.Color.FromArgb(255, 222, 158, 93), label: "Payload");
            AltitudePlot.Plot.AddSignal(GPSAlt, color: System.Drawing.Color.FromArgb(255, 106, 128, 184), label: "GPS");
            AltitudePlot.Plot.Legend();
            AltitudePlot.Plot.SetAxisLimits(0, 10, -80, 1250);

            timergraph.Interval = new TimeSpan(0,0,1);
            timergraph.Tick += new EventHandler(TimerGraph_Tick);
        }

        private void TimerGraph_Tick(object sender, EventArgs e)
        {
            timerSatOne_Tick(sender, e);
            timerSatTwo_Tick(sender, e);
        }


        // This timer adds data frequently (1000 times / second)
        private void timerSatOne_Tick(object sender, EventArgs e)
        {
            if(graphOnline)
            {
                if(mast_status == 'M' && state == "LANDED")
                {
                    timergraph.Stop();
                }
                GPSSats[NextPointIndex] = gps_sats_count;
                PayloadAlt[NextPointIndex] = altitude;
                GPSAlt[NextPointIndex] = gps_altitude;
                Pressure[NextPointIndex] = pressure;
                Temperature[NextPointIndex] = temperature;
                Voltage[NextPointIndex] = voltage;
                Lat[NextPointIndex] = gps_latitude;
                Long[NextPointIndex] = gps_longitude;
                TiltX[NextPointIndex] = tilt_x;
                TiltY[NextPointIndex] = tilt_y;
            }
            else
            {
                GPSSats[NextPointIndex] = 0;
                PayloadAlt[NextPointIndex] = 0;
                GPSAlt[NextPointIndex] = 0;
                Pressure[NextPointIndex] = 0;
                Temperature[NextPointIndex] = 0;
                Voltage[NextPointIndex] = 0;
                Lat[NextPointIndex] = 0;
                Long[NextPointIndex] = 0;
                TiltX[NextPointIndex] = 0;
                TiltY[NextPointIndex] = 0;
            }

            SignalPlot.MaxRenderIndex = NextPointIndex;
            NextPointIndex += 1;
        }

        // This timer renders infrequently (10 times per second)
        private void timerSatTwo_Tick(object sender, EventArgs e)
        {
            // adjust the axis limits only when needed
            double currentRightEdge = SatellitCountPlot.Plot.GetAxisLimits().XMax;
            
            if (NextPointIndex > currentRightEdge)
            {
                SatellitCountPlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
                PressurePlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
                VoltagePlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
                TemperaturePlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
                TiltPlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
                AltitudePlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
                LatLongPlot.Plot.SetAxisLimits(xMax: currentRightEdge + 100);
            }

            SatellitCountPlot.Render();
            PressurePlot.Render();
            VoltagePlot.Render();
            TemperaturePlot.Render();
            TiltPlot.Render();
            AltitudePlot.Render();
            LatLongPlot.Render();
        }

        public void USBChangedEvent(object sender, EventArrivedEventArgs e)
        {
            (sender as ManagementEventWatcher).Stop();

            Dispatcher.Invoke((MethodInvoker) delegate
            {
                ManagementObjectSearcher deviceList = new("Select Name, Description, Description, DeviceID from Win32_SerialPort");

                // List to store available USB serial devices plugged in 
                List<String> CompPortList = new();

                ComportDropdown.Items.Clear();
                // Any results? There should be!
                if (deviceList != null)
                {
                    // Enumerate the devices
                    foreach (ManagementObject device in deviceList.Get().Cast<ManagementObject>())
                    {
                        SerialPortNumber = device["DeviceID"].ToString();
                        string serialName = device["Name"].ToString();
                        string SerialDescription = device["Description"].ToString();
                        CompPortList.Add(SerialPortNumber);
                        ComportDropdown.Items.Add(SerialPortNumber);
                    }
                }
                else
                {
                    ComportDropdown.Items.Add("NO SerialPorts AVAILABLE!");
                }
            });
            (sender as ManagementEventWatcher).Start();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            watcher = new GeoCoordinateWatcher();
            watcher.StatusChanged += Watcher_StatusChanged;
            watcher.Start();


            detector = new ManagementEventWatcher("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            detector.EventArrived += new EventArrivedEventHandler(USBChangedEvent);
            detector.Start();

            try
            {
                string[] CompPorts = SerialPort.GetPortNames();
                foreach(string ComPort in CompPorts)
                {
                    ComportDropdown.Items.Add(ComPort);
                }
                string[] BaudRate = {"2400", "4800", "9600", "14400", "19200", "38400", "57600", "115200", "128000", "256000" };
                foreach(string baud in BaudRate)
                {
                    BaudrateDropdown.Items.Add(baud);
                }

                FlightOnOffSwitch.IsChecked = true;
                SimulationStatus.Content = "DISABLE";
                SimulationStatusPane.Background = System.Windows.Media.Brushes.DarkRed;

                this.WriteTelemetryData = new AddDataDelegate(NewLine);
                GC.Collect();
            }
            catch (NullReferenceException)
            {
                return;
            }

        }

        private void WindowClosed(object sender, EventHandler e)
        {
            SoundPlayer player = new(binAppPath + "/Audio/GCSSTOP.wav");
            player.Play();
            timerSimulation.Stop();
            timerCSV = 0;
            timer.Stop();
        }

        private void NewLine(string NewLine)
        {
            SerialControlTextBox.AppendText(NewLine);
        }

        private void GmapView_Load()
        {
            try
            {
                //Bitmap IconGCS = new Bitmap(binAppPath + "/Icon/EEPISat.png");

                GmapView.MapProvider = GoogleMapProvider.Instance;
                GmapView.Manager.Mode = AccessMode.ServerAndCache;
                GmapView.CacheLocation = binAppPath + "\\MapCache\\";
                GmapView.Position = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude); 
                GmapView.MinZoom = 0;
                GmapView.MaxZoom = 18;
                GmapView.Zoom = 18;
                GmapView.ShowCenter = false;
                GmapView.DragButton = MouseButtons.Left;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    GMapOverlay markers = new("markers");
                    var markerGCS = new GMapOverlay("markers");
                    mapMarkerGCS = new GMarkerGoogle(new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude), GMarkerGoogleType.blue_pushpin);

                    markers.Markers.Add(mapMarkerGCS);
                    GmapView.Overlays.Add(markers);
                }));
                GC.Collect();
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
            catch (OverflowException)
            {
                return;
            }
            catch
            {
                return;
            }
        }

        private void GmapViewHome_Load(object sender, EventArgs e)
        {
            try
            {
                //Bitmap IconGCS = new Bitmap(binAppPath + "/Icon/EEPISat.png");

                GmapViewHome.MapProvider = GoogleSatelliteMapProvider.Instance;
                GmapViewHome.Manager.Mode = AccessMode.ServerAndCache;
                GmapViewHome.CacheLocation = binAppPath + "\\MapCache\\";
                GmapViewHome.Position = new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);
                GmapViewHome.MinZoom = 0;
                GmapViewHome.MaxZoom = 18;
                GmapViewHome.Zoom = 18;
                GmapViewHome.ShowCenter = false;
                GmapViewHome.DragButton = MouseButtons.Left;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    GCSStatusText.Content = "In Service";
                    GCSStatusIcon.Foreground = System.Windows.Media.Brushes.Green;
                    string latitude = watcher.Position.Location.Latitude.ToString();
                    string longitude = watcher.Position.Location.Longitude.ToString();
                    GCSCoordinateText.Content = latitude.Substring(0, 7) + ", " + longitude.Substring(0, 7);
                    GMapOverlay markers = new("markers");
                    var markerGCS = new GMapOverlay("markers");
                    mapMarkerGCS = new GMarkerGoogle(new PointLatLng(watcher.Position.Location.Latitude, watcher.Position.Location.Longitude), GMarkerGoogleType.arrow);

                    markers.Markers.Add(mapMarkerGCS);
                    GmapViewHome.Overlays.Add(markers);
                }));

                
                if (_serialPort.IsOpen == true)
                {
                    if(gps_latitude != 0 && gps_longitude != 0)
                    {
                        PayloadStatusText.Content = "In Service";
                        PayloadStatusIcon.Foreground = System.Windows.Media.Brushes.Green;
                        string Platitude = gps_latitude.ToString();
                        string Plongitude = gps_longitude.ToString();
                        PayloadCoordinateText.Content = Platitude.Substring(0, 7) + ", " + Plongitude.Substring(0, 7);
                        GMapOverlay markersP = new("markers");
                        var markerPayload = new GMapOverlay("markers");
                        mapMarkerPayload = new GMarkerGoogle(new PointLatLng(gps_latitude, gps_longitude), GMarkerGoogleType.blue);

                        markersP.Markers.Add(mapMarkerPayload);
                        GmapViewHome.Overlays.Add(markersP);
                        
                    } else
                    {
                        PayloadStatusText.Content = "Locking GPS....";
                        PayloadStatusIcon.Foreground = System.Windows.Media.Brushes.Blue;
                    }
                } else
                {
                    PayloadStatusText.Content = "Out Of Service";
                    PayloadStatusIcon.Foreground = System.Windows.Media.Brushes.Red;
                }
                GC.Collect();
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
            catch (OverflowException)
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
                if (_serialPort.IsOpen == true)
                {
                    try
                    {
                        if (gps_latitude != 0 && gps_longitude != 0)
                        {
                            GmapView.HoldInvalidation = true;
                            if (mapMarkerPayload != null)
                            {
                                mapMarkerPayload.Position = new PointLatLng(gps_latitude, gps_longitude);
                                mapMarkerPayload.ToolTipText = $"Payload\n" + $" Latitude : {gps_latitude}, \n" + $" Longitude : {gps_longitude}";

                                double betweenlat = gps_latitude + watcher.Position.Location.Latitude;
                                double betweenlng = gps_longitude + watcher.Position.Location.Longitude;
                                GmapView.Position = new PointLatLng(betweenlat / 2, betweenlng / 2);
                                
                            } else
                            {
                                string Platitude = gps_latitude.ToString();
                                string Plongitude = gps_longitude.ToString();
                                PayloadCoordinateText.Content = Platitude.Substring(0, 7) + ", " + Plongitude.Substring(0, 7);
                                GMapOverlay markersP = new("markers");
                                var markerPayload = new GMapOverlay("markers");
                                mapMarkerPayload = new GMarkerGoogle(new PointLatLng(gps_latitude, gps_longitude), GMarkerGoogleType.blue);

                                markersP.Markers.Add(mapMarkerPayload);
                                GmapViewHome.Overlays.Add(markersP);
                            }
                            GmapView.Refresh();
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

                    double dLat2InRad = gps_latitude * (Math.PI / 180);
                    double dLong2InRad = gps_longitude * (Math.PI / 180);

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

                GCSDistanceText.Content = String.Format("{0:0}", distance);

                if (gps_latitude == 0 && gps_longitude == 0)
                {
                    GCSDistanceText.Content = "Unknown";
                }
                GC.Collect();
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
            catch (Exception)
            {
                return;
            }
        }

        public void Serialport_Datareceive(object sender, SerialDataReceivedEventArgs e)
        {
            //if (stepData == Sequencer.readSensor)
            //{
            try
                {
                    this.dataSensor = _serialPort.ReadLine();
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
                    System.Windows.Forms.MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            //}
        }

        public async void VerifyData()
        {
            await SerialControlTextBox.Dispatcher.BeginInvoke(this.WriteTelemetryData, new Object[]
            {
                string.Concat(Emoji.Use.Speech_Balloon + " \0" +dataSensor, "------------#END OF PACKET DATA------------\n")
            });
            SerialControlTextBox.SelectionLength = SerialControlTextBox.Text.Length;
            SerialControlTextBox.ScrollToEnd();
            CMDTextBox1.IsReadOnly = false;
            //CMDTextBox1.Focus();
            CMDTextBox2.SelectAll();
            CMDTextBox2.SelectionLength = CMDTextBox2.Text.Length;
            CMDTextBox2.ScrollToEnd();
            dataSum++;

            if (dataSum == 50)
            {
                SerialControlTextBox.Clear();
                dataSum = 0;
            }
            isAscii = Regex.IsMatch(dataSensor, @"[^\u0021-\u007E]", RegexOptions.None);

            if (isAscii == false)
            {
                try
                {
                    splitData = dataSensor.Split((char)44); // (char)44 = ','
                    System.Diagnostics.Debug.WriteLine("VerifyData : splitData {0}", dataSensor);

                    //System.Diagnostics.Debug.WriteLine("VerifyData : Receiver Checksum " + recCheckSum);
                    //Pengecekan 
                    //  Panjang Data               Team ID                  Team ID Testing             Team ID                     Mission Time                Packet Count         
                    if ((splitData.Length == 20 || splitData.Length == 21 || splitData.Length == 22 || splitData.Length == 23) && (splitData[0] == "1085" || splitData[0] == "1000") && splitData[0].Length == 4 && splitData[1].Length == 11 && splitData[2].Length <= 4 )
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("VerifyData : Write Data " + splitData.ToString());
                            CheckTelemetryData();
                            PayDataLog();
                            WriteLogPayload();
                            GC.Collect();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
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
                        catch (Exception)
                        {
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                    //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        public static byte CheckHasil(char[] data_, byte checksum, byte length)
        {
            ushort buff = 0;
            byte hasil, buffhasil;
            for (int i = 0; i < 150; i++)
            {
                buff += data_[i];
                if (data_[i] == '\0' || i > length - 2) break;
            }
            buff += checksum;
            hasil = (byte)buff;
            buffhasil = (byte)(buff >> 8);
            hasil += buffhasil;
            return hasil;
        }

        private void RotateBackModel()
        {
            axis *= -1;
            Matrix3D matrix = model.Content.Transform.Value;

            matrix.Rotate(new System.Windows.Media.Media3D.Quaternion(axis, angle));
            model.Content.Transform = new MatrixTransform3D(matrix);
        }

        private void CheckTelemetryData()
        {
            try
            {
                if (splitData.Length == 20 || splitData.Length == 21 || splitData.Length == 22 || splitData.Length == 23)
                {
                    System.Diagnostics.Debug.WriteLine("Error - Masuk " + splitData[21]);
                    totalPayloadData++;
                    int checkSum = Int32.Parse(splitData[21]);
                    splitData[21] = null;
                    string checkString = String.Join(",", splitData);
                    char[] dataSensorChar = checkString.ToCharArray();
                    byte hasil = CheckHasil(dataSensorChar, (byte)checkSum, (byte)dataSensorChar.Length);
                    if (hasil == 255)
                    {
                        validCount++;
                        checkSumHasil = true;
                    }
                    else {
                        corruptCount++;
                        checkSumHasil = false;
                    }


                    if (splitData[0].Length > 0 && splitData[0].All(c => Char.IsNumber(c)))
                    {
                        teamId = Convert.ToUInt32(splitData[0]);
                    }
                    else
                    {
                        teamId = 0;
                    }


                    if (splitData[2].Length > 0 && splitData[2].All(c => Char.IsNumber(c)))
                    {
                        packetCount = Convert.ToUInt32(splitData[2]);
                    }
                    else
                    {
                        packetCount = 0;
                    }


                    if (splitData[5].Length > 0 && splitData[5].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        altitude = Convert.ToSingle(splitData[5]);
                    }
                    else
                    {
                        altitude = 0.0f;
                    }

                    if (splitData[9].Length > 0 && splitData[9].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        temperature = Convert.ToSingle(splitData[9]);
                    }
                    else
                    {
                        temperature = 0.0f;
                    }

                    if (splitData[10].Length > 0 && splitData[10].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        pressure = Convert.ToSingle(splitData[10]);
                    }
                    else
                    {
                        pressure = 0.0f;
                    }
                    
                    if (splitData[11].Length > 0 && splitData[11].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        voltage = Convert.ToSingle(splitData[11]);
                    }
                    else
                    {
                        voltage = 0.0f;
                    }



                    if (splitData[12].Length > 0 && splitData[12].All(c => Char.IsNumber(c) || c == '.' || c == ':'))
                    {
                        gps_time = Convert.ToString(splitData[12]);
                    }
                    else
                    {
                        gps_time = "00:00:00";
                    }

                    if (splitData[13].Length > 0 && splitData[13].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        gps_altitude = Convert.ToSingle(splitData[13]);
                    }
                    else
                    {
                        gps_altitude = 0.0f;
                    }

                    gps_latitude = Convert.ToSingle(splitData[14]);
                    gps_longitude = Convert.ToSingle(splitData[15]);

                    if (splitData[16].Length > 0 && splitData[16].All(c => Char.IsNumber(c)))
                    {
                        gps_sats_count = Convert.ToUInt32(splitData[16]);
                    }
                    else
                    {
                        gps_sats_count = 0;
                    }
                    
                    tilt_x = Convert.ToSingle(splitData[17]);
                    tilt_y = Convert.ToSingle(splitData[18]);
                    missionTime = splitData[1];
                    mode = splitData[3].ToCharArray()[0];   
                    state = splitData[4];
                    hs_status = splitData[6].ToCharArray()[0];
                    pc_status = splitData[7].ToCharArray()[0];
                    mast_status = splitData[8].ToCharArray()[0];
                    cmd_echo = splitData[19];
                    System.Diagnostics.Debug.WriteLine("VerifyData : The Data " + teamId + "," + missionTime + "," + packetCount + "," + altitude + "," + temperature + "," + voltage + "," + gps_time + "," + gps_altitude + "," + gps_latitude + "," + gps_longitude + "," + gps_sats_count + "," + tilt_x + "," + tilt_y + "," + mode + "," + state + "," + hs_status + "," + pc_status + "," + mast_status + "," + cmd_echo + "," + pressure);
                }
                else if (splitData.Length == 1)
                {
                    cmd_echo = splitData[18];
                }


                // checkSum = Convert.ToDouble(splitData[34]);

                max_voltage = voltage;
                if (Math.Abs(max_voltage) > this.max_max_voltage)
                {
                    max_max_voltage = Math.Abs(max_voltage);
                    time_max_max_voltage = missionTime;
                }

                max_altitude = altitude;
                if (Math.Abs(max_altitude) > this.max_max_altitude)
                {
                    max_max_altitude = Math.Abs(max_altitude);
                }

                max_temperature = temperature;
                if (Math.Abs(max_temperature) > this.max_max_temperature)
                {
                    max_max_temperature = Math.Abs(max_temperature);
                    time_max_max_temperature = missionTime;
                }

                max_pressure = pressure;
                if (Math.Abs(max_pressure) > this.max_max_pressure)
                {
                    max_max_pressure = Math.Abs(max_pressure);
                    time_max_max_pressure = missionTime;
                }

                max_packetCount = packetCount;
                if (Math.Abs(max_packetCount) > this.max_max_packetCount)
                {
                    max_max_packetCount = max_packetCount;
                }

                this.Dispatcher.BeginInvoke(new uiupdater(ShowTelemetryData));
                graphOnline = true;
                this.Dispatcher.BeginInvoke(new uiupdater(GmapView_Region));
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
            MissionTimeLabel.Text = String.Format("{00:00:00}", missionTime);
            SoftwareStateLabel.Text = String.Format("{0}", state);

            //#region 3D Item

            if(axis != null && model.Content != null)
            {
                RotateBackModel();
            }

            if ((string)ThreeDModelStatus.Content != "Started" && (string)ThreeDModelBtn.Content != "Started")
            {
                ThreeDModelStatus.Content = "Started";
                ThreeDModelBtn.Content = "Started";
                HelixViewport3D_Load();
            }

            if(hs_status == 'N' && mast_status == 'N')
            {
                fileobj = System.AppDomain.CurrentDomain.BaseDirectory + "/Assets/3D/Probe_Stowed.obj";
                HelixViewport3D_Load();
                mast_raised = false;
                hs_deployed = false;
            }

            if(hs_status == 'P' && !hs_deployed)
            {
                fileobj = System.AppDomain.CurrentDomain.BaseDirectory + "/Assets/3D/Probe_Deploy.obj";
                HelixViewport3D_Load();
                hs_deployed = true;
            }
            if (mast_status == 'M' && !mast_raised)
            {
                fileobj = System.AppDomain.CurrentDomain.BaseDirectory + "/Assets/3D/Probe_Upright.obj";
                HelixViewport3D_Load();
                mast_raised = true;
            }


            if (tilt_x == 0.00 && tilt_y == 0.00)
            {
                axis = new Vector3D((float)0.00000000000001, (float)0.00000000000001, (float)0.00000000000001);
            }
            else
            {
                if(tilt_x == 0.00)
                {
                    axis = new Vector3D((float)0.00000000000001, (float)tilt_y, (float)0.00000000000001);
                } else if(tilt_y == 0.00)
                {
                    axis = new Vector3D((float)tilt_x, (float)0.00000000000001, (float)0.00000000000001);
                }
                else
                {
                    axis = new Vector3D((float)tilt_x, (float)tilt_y, (float)0.0);
                }
            }

            Matrix3D matrix = model.Content.Transform.Value;

            matrix.Rotate(new System.Windows.Media.Media3D.Quaternion(axis, angle));
            model.Content.Transform = new MatrixTransform3D(matrix);

            var loadingAnimation = new DoubleAnimation(0.01, 1, new Duration(TimeSpan.FromSeconds(0.5)));

            Storyboard.SetTarget(loadingAnimation, model.Content);

            //Storyboard.SetTargetProperty(loadingAnimation, new PropertyPath(UIElement.OpacityProperty));

            //Storyboard.SetTarget(loadingAnimation, AssociatedObject);

            //var storyboard = new Storyboard();
            //storyboard.Children.Add(loadingAnimation);
            //storyboard.Children.Add(closingAnimation);
            //// Subscription to events must be done at this point, because the Storyboard object becomes frozen later on
            //storyboard.Completed += HandleOnCompleted;

            //string storyBoardName = "BeginNotificationStoryboard";

            // We define the BeginStoryBoard action for the EventTrigger
            //var beginStoryboard = new BeginStoryBoard();
            //beginStoryboard.Name = storyBoardName;
            //beginStoryboard.Storyboard = storyboard;

            //// We create the EventTrigger
            //var eventTrigger = new EventTrigger(Control.LoadedEvent);
            //eventTrigger.Actions.Add(beginStoryboard);

            //// Actions for the entering animation
            //var enterSeekStoryboard = new SeekStoryboard
            //{
            //    Offset = TimeSpan.FromSeconds(5),
            //    BeginStoryboardName = storyBoardName
            //};
            //var enterPauseStoryboard = new PauseStoryboard
            //{
            //    BeginStoryboardName = storyBoardName
            //};

            //// Actions for the exiting animation
            //var exitSeekStoryboard = new SeekStoryboard
            //{
            //    Offset = TimeSpan.FromSeconds(5),
            //    BeginStoryboardName = storyBoardName
            //};
            //var exitResumeStoryboard = new ResumeStoryboard
            //{
            //    BeginStoryboardName = storyBoardName
            //};

            //var trigger = new Trigger
            //{
            //    Property = UIElement.IsMouseOverProperty,
            //    Value = true
            //};

            //trigger.EnterActions.Add(enterSeekStoryboard);
            //trigger.EnterActions.Add(enterPauseStoryboard);
            //trigger.ExitActions.Add(exitSeekStoryboard);
            //trigger.ExitActions.Add(exitResumeStoryboard);

            //var style = new Style();
            //// The name of the Storyboard must be registered so the actions can find it
            //style.RegisterName(storyBoardName, beginStoryboard);
            //// Add both the EventTrigger and the regular Trigger
            //style.Triggers.Add(eventTrigger);
            //style.Triggers.Add(trigger);

            //AssociatedObject.Style = style;

            

            //#endregion

            try
            {
                // Graph Label
                SatellitCountPlot.Plot.XLabel("Sats: "+gps_sats_count);
                SatellitCountPlot.Plot.XAxis.Label(size: 12);
                PressurePlot.Plot.XLabel("Pressure: " + pressure + " kPa");
                PressurePlot.Plot.XAxis.Label(size: 12);
                AltitudePlot.Plot.XLabel("Payload: " + altitude + " m GPS: "+gps_altitude+" m");
                AltitudePlot.Plot.XAxis.Label(size: 12);
                TemperaturePlot.Plot.XLabel("Temp: " + temperature + " °C");
                TemperaturePlot.Plot.XAxis.Label(size: 12);
                VoltagePlot.Plot.XLabel("Volt: " + voltage + " volts");
                VoltagePlot.Plot.XAxis.Label(size: 12);
                LatLongPlot.Plot.XLabel("Lat: " + gps_latitude + " Long: "+ gps_longitude);
                LatLongPlot.Plot.XAxis.Label(size: 12);
                TiltPlot.Plot.XLabel("X: " + tilt_x + " Y: "+ tilt_y);
                TiltPlot.Plot.XAxis.Label(size: 12);


                // Checksum
                lblValidatedData.Text = validCount.ToString();
                lblCorruptedData.Text = corruptCount.ToString();
                lblTotalData.Text = totalPayloadData.ToString();
                //    #region Dashboard Item

                //    teamIDLabel.Content = String.Format("{0:0000}", containerTeamID);
                // GPS
                GPSTimeLabel.Text = String.Format("{00:00:00}", gps_time);
                GPSCountLabel.Text = String.Format("{0:00}", gps_sats_count);
                GPSLongitudeLabel.Text = String.Format("{0:0.0000}", gps_longitude);
                GPSLatitudeLabel.Text = String.Format("{0:0.0000}", gps_latitude);
                GPSAltitudeLabel.Text = String.Format("{0:0.0}", gps_altitude);

                // Payload
                MaxAltitudeLabel.Text = String.Format("{0:0.0}", max_max_altitude);
                MaxVoltageLabel.Text = String.Format("{0:0.0}", max_max_voltage);
                MaxTemperatureLabel.Text = String.Format("{0:0.0}", max_max_temperature);
                MaxPressureLabel.Text = String.Format("{0:0.0}", max_max_pressure);
                MaxPacketCountLabel.Text = String.Format("{0:0000}", max_max_packetCount);
                PacketCountLabel.Text = String.Format("{0:0000}", packetCount);
                AltitudeLabel.Text = String.Format("{0:0.0}", altitude);
                VoltageLabel.Text = String.Format("{0:0.0}", voltage);
                TemperatureLabel.Text = String.Format("{0:0.0}", temperature);
                PressureLabel.Text = String.Format("{0:0.0}", pressure);
                TiltXLabel.Text = String.Format("{0:0.00}", tilt_x);
                TiltYLabel.Text = String.Format("{0:0.00}", tilt_y);
                CMDEchoLabel.Text = String.Format("{0}", cmd_echo);
                //    #endregion
                
                SolidColorBrush red = (SolidColorBrush)new BrushConverter().ConvertFromString("#7D1C1C");
                SolidColorBrush white = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");
                SolidColorBrush green = (SolidColorBrush)new BrushConverter().ConvertFromString("#5BC12C");
                SolidColorBrush black = (SolidColorBrush)new BrushConverter().ConvertFromString("#000000");
                if (pc_status == 'N')
                {
                    ParachuteReleasedLabel.Background = red;
                    ParachuteReleasedLabel.Foreground = white;
                }
                else
                {
                    ParachuteReleasedLabel.Background = green;
                    ParachuteReleasedLabel.Foreground = black;
                }

                if (hs_status == 'N')
                {
                    HeatShieldReleasedLabel.Background = red;
                    HeatShieldReleasedLabel.Foreground = white;
                }
                else
                {
                    HeatShieldReleasedLabel.Background = green;
                    HeatShieldReleasedLabel.Foreground = black;
                }

                if (mast_status == 'N')
                {
                    MastRaisedLabel.Background = red;
                    MastRaisedLabel.Foreground = white;
                }
                else
                {
                    MastRaisedLabel.Background = green;
                    MastRaisedLabel.Foreground = black;
                }


                ParachuteReleasedLabel.Content = String.Format("{0}", pc_status);
                HeatShieldReleasedLabel.Content = String.Format("{0}", hs_status);
                MastRaisedLabel.Content = String.Format("{0}", mast_status);

                if (cmd_echo == "SIMENABLE" && mode == 'F')
                {
                    FlightOnOffSwitch.IsChecked = false;

                    SimulationStatus.Content = "ENABLE";
                    SimulationStatus.Background = System.Windows.Media.Brushes.ForestGreen;
                }
                else if (cmd_echo == "SIMACTIVATE" && mode == 'S')
                {
                    FlightOnOffSwitch.IsChecked = false;

                    SimulationStatus.Content = "ACTIVE";
                    SimulationStatus.Background = System.Windows.Media.Brushes.ForestGreen;
                }
                else if (cmd_echo == "SIMDISABLE" && mode == 'F')
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
            catch
            {
                //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void PayDataLog()
        {
            if (_serialPort.IsOpen == true)
            {
                try
                {
                    if (mode == 'S')
                    {
                        payloadFileLog = binAppPath + "\\LogData\\SIMULATION\\Flight_" + teamId + ".csv";
                    } else
                    {
                        payloadFileLog = binAppPath + "\\LogData\\FLIGHT\\Flight_" + teamId + ".csv";
                    }
                    if (System.IO.File.Exists(payloadFileLog))
                    {
                        string date = DateTime.Now.ToString("yyyy-MM-dd_mm-ss");
                        //string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        if (mode == 'S')
                        {
                            if (!Directory.Exists(binAppPath + "\\LogData\\SIMULATION\\" + date + "\\"))
                            {
                                Directory.CreateDirectory(binAppPath + "\\LogData\\SIMULATION\\"+date+"\\");
                            }
                            System.IO.File.Move(payloadFileLog, binAppPath + "\\LogData\\SIMULATION\\" + date + "\\Flight_" + teamId + ".csv");
                        }
                        else
                        {
                            if (!Directory.Exists(binAppPath + "\\LogData\\FLIGHT\\" + date + "\\"))
                            {
                                Directory.CreateDirectory(binAppPath + "\\LogData\\FLIGHT\\" + date + "\\");
                            }
                            System.IO.File.Move(payloadFileLog, binAppPath + "\\LogData\\FLIGHT\\" + date + "\\Flight_" + teamId + ".csv");
                        }
                    } 
                    BackgroundWorker worker = new();
                    worker.DoWork += delegate (object s, DoWorkEventArgs args)
                    {
                        
                        
                        var fileCon = System.IO.File.Open(payloadFileLog, (FileMode)FileIOPermissionAccess.Append, FileAccess.Write, FileShare.Read);
                        writePay = new StreamWriter(fileCon, Encoding.GetEncoding(1252))
                        {
                            AutoFlush = true
                        };
                        writePay.Write("TEAM_ID,MISSION_TIME,PACKET_COUNT,MODE,STATE,ALTITUDE,HS_DEPLOYED," +
                                        "PC_DEPLOYED,MAST_RAISED,TEMPERATURE,PRESSURE,VOLTAGE,GPS_TIME, GPS_ALTITUDE,GPS_LATITUDE,GPS_LONGITUDE,GPS_SATS," +
                                        "TILT_X,TILT_Y,CMD_ECHO \n");
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
                return;
            }
        }

        private void WriteLogPayload()
        {
            try
                {
                    writePay.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}\n"
                                    , teamId, missionTime, packetCount, mode
                                    , state, altitude, hs_status, pc_status, mast_status,
                                    temperature, pressure, voltage, gps_time, gps_altitude, gps_latitude, gps_longitude,
                                    gps_sats_count, tilt_x, tilt_y, cmd_echo);
                    writePay.Flush();
                    DataPayload dtp = new()
                    {
                        Pteamid = teamId,
                        Pmissiontime = missionTime,
                        Ppacketcount = packetCount,
                        Pmode = mode,
                        Pstate = state,
                        Paltitude = altitude,
                        Phs_status = hs_status,
                        Ppc_status = pc_status,
                        Pmast_status = mast_status,
                        Ptemperature = temperature,
                        Ppressure = pressure,
                        Pvoltage = voltage,
                        Pgps_time = gps_time,
                        Pgps_altitude = gps_altitude,
                        Pgps_latitude = gps_latitude,
                        Pgps_longitude = gps_longitude,
                        Pgps_sats_count = gps_sats_count,
                        Ptilt_x = tilt_x,
                        Ptilt_y = tilt_y,
                        Pcmd_echo = cmd_echo
                    };
                    if (checkSumHasil)
                        {
                            dtp.Pchecksum = "Valid";
                        } else
                        {
                            dtp.Pchecksum = "Corrupt";
                        }

                    Dispatcher.BeginInvoke((Action)(() =>
                        {
                            PayloadDataCsv.Items.Add(dtp);
                            if (auto_scroll)
                            {
                                PayloadDataCsv.ScrollIntoView(PayloadDataCsv.Items[PayloadDataCsv.Items.Count - 1]);
                            }
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

        public class DataPayload
        {
            public Double Pteamid { get; set; }
            public String Pmissiontime { get; set; }
            public Double Ppacketcount { get; set; }
            public Char Pmode { get; set; }
            public String Pstate { get; set; }
            public Double Paltitude { get; set; }
            public Char Phs_status { get; set; }
            public Char Ppc_status { get; set; }
            public Char Pmast_status { get; set; }
            public Double Ptemperature { get; set; }
            public Double Ppressure { get; set; }
            public Double Pvoltage { get; set; }
            public String Pgps_time { get; set; }
            public Double Pgps_altitude { get; set; }
            public Double Pgps_latitude { get; set; }
            public Double Pgps_longitude { get; set; }
            public uint Pgps_sats_count { get; set; }
            public Double Ptilt_x { get; set; }
            public Double Ptilt_y { get; set; }
            public String Pcmd_echo { get; set; }

            public String Pchecksum { get; set; }
        }

        private void Watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e) //watcher status map
        {
            if (e.Status == GeoPositionStatus.Ready)
            {
                if (watcher.Position.Location.IsUnknown)
                {
                    GCSStatusText.Content = "Out Of Service";
                    GCSStatusIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
                else
                {
                    GCSStatusText.Content = "In Service";
                    GCSCoordinateText.Content = String.Format("{0:00.0000}, {0:00.0000}", watcher.Position.Location.Latitude, watcher.Position.Location.Longitude);
                    GCSStatusIcon.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0));
                }
            }
        }

        private void ShutdownBtnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =  System.Windows.MessageBox.Show("Are you sure to shutdown the GCS?", "", MessageBoxButton.OKCancel);
            switch (result)
            {
                case MessageBoxResult.OK:
                    SoundPlayer player = new(binAppPath + "/Audio/GCSSTOP.wav");
                    player.Play();
                    Thread.Sleep(3000);
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        private void RestartBtnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure to restart the GCS?", "", MessageBoxButton.OKCancel);
            switch (result)
            {
                case MessageBoxResult.OK:
                    SoundPlayer player = new(binAppPath + "/Audio/GCSRESTART.wav");
                    player.Play();
                    Thread.Sleep(3000);
                    Process.Start(System.Windows.Application.ResourceAssembly.Location);
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        private void GetBatteryPercent()
        {
            ManagementClass wmi = new("Win32_Battery");
            var allBatteries =  wmi.GetInstances();
            int estimatedTimeRemaining, estimatedChargeRemaining;
            string final;
            foreach(var battery in allBatteries)
            {
                estimatedChargeRemaining = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
                estimatedTimeRemaining = Convert.ToInt32(battery["EstimatedRunTime"]);
                x = estimatedChargeRemaining;
                y = estimatedTimeRemaining;

                batteryPercentage.Width = Convert.ToDouble(estimatedChargeRemaining);
                lblBatteryStatus.Content = Convert.ToString(estimatedChargeRemaining) + "%" + " ";
            }
            if ((int)hasilRegLin > 715827)
            {
                jumlahDataRegLin = 0;
                sumX = 0;
                sumX2 = 0;
                sumY = 0;
                sumXY = 0;
                hasilRegLin = 0;
            }
            jumlahDataRegLin+=2;
            sumX += (x*2);
            sumX2 += (x * 2) * (x * 2);
            sumY += (y*2);
            sumXY += (x * 2) * (y * 2);
            m = (jumlahDataRegLin * sumXY - sumX * sumY) / (jumlahDataRegLin*sumX2-sumX*sumX);
            c = (sumY - m*sumX) / jumlahDataRegLin;
            hasilRegLin = c + (m * x);
            final = Convert.ToString((int)hasilRegLin) + " Minutes Remaining";


            if (y == 71582788)
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (System.Windows.Media.Brush)converter.ConvertFromString("#00FF00");
                batteryPercentage.Background = brush;
                final = "On Charging";
            }
            else if (y < 30)
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (System.Windows.Media.Brush)converter.ConvertFromString("#FFFF00");
                batteryPercentage.Background = brush;
            }
            else if (y < 10)
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (System.Windows.Media.Brush)converter.ConvertFromString("#FF0000");
                batteryPercentage.Background = brush;
            }
            lblBatteryStatus.Content += final;
            if (jumlahDataRegLin >= 1000)
            {
                jumlahDataRegLin = 0;
                sumX = 0;
                sumX2 = 0;
                sumY = 0;
                sumXY = 0;
            }
            GC.Collect();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            GetBatteryPercent();
            GetPerformanceIndicator();
        }

        private void SerialControlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SerialControlTextBox.ScrollToEnd();
        }

        private async void GetPerformanceIndicator()
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(2000);
            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime; 
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds; 
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            Process currentProc = Process.GetCurrentProcess();
            long memoryUsed = currentProc.PrivateMemorySize64;
            lblCPU.Content = (int)(cpuUsageTotal * 100) + " %";
            lblRAM.Content = (memoryUsed / 1000000) + " MB";
            GC.Collect();
        }

        private void SimToggleBtn_checked(object sender, RoutedEventArgs e)
        {
            // T1.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void SimToggleBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            //T2.Foreground = new SolidColorBrush(Colors.Blue);
        }

        private void SimToggleBtn_click(object sender, RoutedEventArgs e)
        {
            //T2.Foreground = new SolidColorBrush(Colors.Blue);
        }

        private void HomeNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 0;
        }

        private void GraphNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 1;
        }

        private void MapNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 2;
        }

        private void DataCsvNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 3;
        }

        private void MainPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "CSV Files (*.csv)|*.csv|TXT Files(*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (openFileDialog.ShowDialog() == true)
            {
                csvTextBox.Text = openFileDialog.FileName;
                this.Dispatcher.Invoke(() =>
                {
                    BindData(csvTextBox.Text);
                });

            }
        }
        
        private void BindData(string filePath) //Pengambilan Data
        {
            try
            {
                string ext = Path.GetExtension(filePath);
                DataTable dt = new();
                if(ext == ".csv")
                {
                    string[] data = System.IO.File.ReadAllLines(filePath);
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
                            kolom[1] = teamId.ToString();
                            dt.Rows.Add(kolom);
                        }
                    }
                } else if(ext == ".txt")
                {
                    using StreamReader file = new(filePath);

                    string ln;
                    string[] kolom = null;
                    string[] header = { "Command", "Team ID", "Simp", "Pressure" };
                    int x = 0;
                    while ((ln = file.ReadLine()) != null)
                    {
                        if (ln.StartsWith("#")) continue;
                        if (ln == "") continue;
                        kolom = ln.Split(',');
                        kolom[1] = teamId.ToString();
                        if (x == 0)
                        {
                            for (int i = 0; i <= kolom.Count() - 1; i++)
                            {
                                dt.Columns.Add(header[i]);
                            }
                            x++;
                        }
                        if (x != 0)
                        {
                            dt.Rows.Add(kolom);
                        }
                    }
                    file.Close();
                } else
                {
                    System.Windows.Forms.MessageBox.Show("File yang anda pilih bukan file CSV atau TXT");
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
        
        private void ConnectPortBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort.IsOpen == false)
            {
                try
                {
                    //ConnectPortBtn. = Properties.Resources.icon_Disconnect;
                    if (ComportDropdown.SelectedItem == null)
                    {
                        //bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                        //TBContent.Text = "Warning!";
                        //TBContent1.Text = "Comport Can't be Empty!";
                        //MessageOK.Visibility = Visibility.Hidden;
                        //MessageNO.Visibility = Visibility.Hidden;
                        //MessageOK1.Visibility = Visibility.Visible;
                        //winhost.Visibility = Visibility.Hidden;
                        //MessageOK1.Focus();
                        //CMBox.Visibility = Visibility.Visible;
                    }

                    else if (BaudrateDropdown.SelectedItem == null)
                    {
                        //bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                        //TBContent.Text = "Warning!";
                        //TBContent1.Text = "Serial Can't be Empty!";
                        //MessageOK.Visibility = Visibility.Hidden;
                        //MessageNO.Visibility = Visibility.Hidden;
                        //MessageOK1.Visibility = Visibility.Visible;
                        //winhost.Visibility = Visibility.Hidden;
                        //MessageOK1.Focus();
                        //CMBox.Visibility = Visibility.Visible;
                    }

                    else
                    {
                        _serialPort.PortName = ComportDropdown.SelectedItem.ToString();
                        _serialPort.BaudRate = Convert.ToInt32(BaudrateDropdown.SelectedItem);
                        _serialPort.NewLine = "\r";
                        _serialPort.Close();
                        _serialPort.Open();
                        PortStatus.Content = "Connected To Serial Port";
                        SerialPortStatus.Content = "Connected";
                        PortStatusPane.Background = System.Windows.Media.Brushes.ForestGreen;
                        ConnectPortBtn.Visibility = Visibility.Hidden;
                        DisconnectPortBtn.Visibility = Visibility.Visible;
                        CMDTextBox1.IsReadOnly = false;

                        timergraph.Start();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
                    PortStatus.Content = "Can't Connect To Serial Port, Try Again!";
                    PortStatusPane.Background = System.Windows.Media.Brushes.Firebrick;
                    return;
                }
            }
        }
        private void DisconnectPortBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort.IsOpen == true)
            {
                try
                {
                    _serialPort.Close();
                    PortStatus.Content = "Disconnected From Serial Port";
                    SerialPortStatus.Content = "Disconnected";
                    SerialDataStatus.Content = "Idle";
                    PortStatusPane.Background = System.Windows.Media.Brushes.Firebrick;
                    ConnectPortBtn.Visibility = Visibility.Visible;
                    DisconnectPortBtn.Visibility = Visibility.Hidden;
                    CMDTextBox1.IsReadOnly = true;
                    timergraph.Stop();
                }
                catch
                {
                    #region messageBox
                    //bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                    //TBContent.Text = "Warning!";
                    //TBContent1.Text = "Serial Port Error!";
                    //MessageOK.Visibility = Visibility.Hidden;
                    //MessageNO.Visibility = Visibility.Hidden;
                    //MessageOK1.Visibility = Visibility.Visible;
                    //winhost.Visibility = Visibility.Hidden;
                    //MessageOK1.Focus();
                    //CMBox.Visibility = Visibility.Visible;
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
                _serialPort.Close();
                PortStatus.Content = "Disconnected From Serial Port";
                PortStatusPane.Background = System.Windows.Media.Brushes.Firebrick;

                ComportDropdown.SelectedValue = null;
                BaudrateDropdown.SelectedValue = null;

                ConnectPortBtn.Visibility = Visibility.Visible;
                DisconnectPortBtn.Visibility = Visibility.Hidden;

                string[] CompPorts = SerialPort.GetPortNames();
                ComportDropdown.Items.Clear();
                foreach (string ComPort in CompPorts)
                {
                    ComportDropdown.Items.Add(ComPort);
                }
            }
            catch
            {
                return;
            }
        }

        private void HelixViewport3D_Load()
        {
            ModelImporter import = new();
            Model3DGroup model1 = import.Load(fileobj);

           
            model.Content = model1;
        }

        private void SettingNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 4;
        }
        
        private void SimDataClick(object sender, RoutedEventArgs e)
        {
            DataPage.SelectedIndex = 0;
        }
        
        private void PayloadDataClick(object sender, RoutedEventArgs e)
        {
            DataPage.SelectedIndex = 1;
        }

        
        private void CommandToPayload()
        {
            if (CMDTextBox1.GetLineText(0) == "" && lineCommand == 0)
            {
                CMDTextBox2.Text = Emoji.Use.Robot_Face + "Enter a command first my lord....";
            }
            else
            {
                string line = CMDTextBox1.GetLineText(0);
                if (line == "CMD,1085,CAL")
                {
                    try
                    {
                        string cmd = "CMD,1085,CAL";

                        _serialPort.WriteLine(cmd);

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Information_Source + "Payload is calibrating...." + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SerialDataStatus.Content = "CAL";

                        SoftwareStateLabel.Text = "BOOT";


                        MaxAltitudeLabel.Text = String.Format("{0:0.0}", 0);
                        MaxVoltageLabel.Text = String.Format("{0:0.0}", 0);
                        MaxTemperatureLabel.Text = String.Format("{0:0.0}", 0);
                        MaxPressureLabel.Text = String.Format("{0:0.0}", 0);
                        MaxPacketCountLabel.Text = String.Format("{0:0000}", 0);

                        SolidColorBrush red = (SolidColorBrush)new BrushConverter().ConvertFromString("#7D1C1C");
                        SolidColorBrush white = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");

                        ParachuteReleasedLabel.Background = red;
                        ParachuteReleasedLabel.Foreground = white;

                        HeatShieldReleasedLabel.Background = red;
                        HeatShieldReleasedLabel.Foreground = white;

                        MastRaisedLabel.Background = red;
                        MastRaisedLabel.Foreground = white;


                        SoundPlayer player = new(binAppPath + "/Audio/CAL.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }


                }
                else if (line == "CMD,1085,CX,ON")
                {
                    try
                    {
                        string cmd = "CMD,1085,CX,ON";
                        _serialPort.WriteLine(cmd);
                        

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Information_Source + "Activating payload telemetry......" + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SerialDataStatus.Content = "CXON";
                        SoundPlayer player = new(binAppPath + "/Audio/PXON.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (line == "CMD,1085,CX,OFF")
                {
                    try
                    {
                        string cmd = "CMD,1085,CX,OFF";

                        _serialPort.WriteLine(cmd);
                        

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Information_Source + "Deactivating  payload telemetry" + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SerialDataStatus.Content = "CXOFF";
                        SoundPlayer player = new(binAppPath + "/Audio/PXOFF.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (line.Contains("CMD,1085,ST,"))
                {
                    try
                    {
                        string lineSplit = line.Split(',')[3];
                        string pattern = @"^([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$";
                        if (lineSplit == "GPS")
                        {
                            try
                            {
                                string cmd = "CMD,1085,ST,GPS";

                                _serialPort.WriteLine(cmd);
                                

                                CMDTextBox2.Text += "\r\n" + Emoji.Use.Clock1 + "Setting payload time by GPS....." + "\r\n";
                                CMDTextBox2.ScrollToEnd();

                                SerialDataStatus.Content = "STGPS";
                                SoundPlayer player = new(binAppPath + "/Audio/STGPS.wav");
                                player.Play();
                                CMDTextBox1.Clear();
                                CMDShortcut.SelectedIndex = -1;
                            }
                            catch
                            {
                                return;
                            }

                        }
                        else if (Regex.IsMatch(lineSplit, pattern))
                        {
                            try
                            {
                                string cmd = "CMD,1085,ST," + lineSplit;

                                _serialPort.WriteLine(cmd);
                                

                                CMDTextBox2.Text += "\r\n" + Emoji.Use.Clock1 + "Set time payload by UTC...." + "\r\n";
                                CMDTextBox2.ScrollToEnd();

                                SerialDataStatus.Content = "ST" + lineSplit;
                                SoundPlayer player = new(binAppPath + "/Audio/STUTC.wav");
                                player.Play();
                                CMDTextBox1.Clear();
                                CMDShortcut.SelectedIndex = -1;
                            }
                            catch
                            {
                                return;
                            }
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (line == "CMD,1085,SIM,ENABLE")
                {
                    try
                    {
                        string cmd = "CMD,1085,SIM,ENABLE";

                        _serialPort.WriteLine(cmd);
                        

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Rotating_Light + "Enable payload simulation mode...." + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SerialDataStatus.Content = "SIMENABLE";
                        SoundPlayer player = new(binAppPath + "/Audio/SIMENABLE.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (line == "CMD,1085,SIM,DISABLE")
                {
                    try
                    {
                        string cmd = "CMD,1085,SIM,DISABLE";

                        _serialPort.WriteLine(cmd);
                        

                        SerialDataStatus.Content = "SIMDISABLE";
                        CMDTextBox2.Text += "\r\n" + Emoji.Use.No_Entry + "Disable payload simulation mode...." + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SimulationStatus.Content = "DISABLE";
                        SimulationStatusPane.Background = System.Windows.Media.Brushes.Firebrick;

                        FlightOnOffSwitch.IsChecked = true;

                        timerSimulation.Stop();
                        timerCSV = 0;

                        SoundPlayer player = new(binAppPath + "/Audio/SIMDISABLE.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (line == "CMD,1085,SIM,ACTIVATE")
                {
                    try
                    {
                        string cmd = "CMD,1085,SIM,ACTIVATE";

                        _serialPort.WriteLine(cmd);
                        

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Construction + "Activating payload simulation......" + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        FlightOnOffSwitch.IsChecked = false;
                        SimulationStatus.Content = "ACTIVATE";
                        SimulationStatusPane.Background = System.Windows.Media.Brushes.ForestGreen;
                        SerialDataStatus.Content = "SIMACTIVATE";
                        SoundPlayer player = new(binAppPath + "/Audio/SIMACTIVATE.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else if (line.Contains("CMD,1085,SIMP,"))
                {
                    string lineSplit = line.Split(',')[3];
                    if ((string)SimulationStatus.Content != "ACTIVATE" && FlightOnOffSwitch.IsChecked == true)
                    {
                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Loudspeaker + "Enable and Activate simulation mode to use this command!" + "\r\n";
                        CMDTextBox2.ScrollToEnd();
                    }
                    if (lineSplit == "PRESSURE")
                    {

                        try
                        {

                            if (openFileDialog == null)
                            {
                                CMDTextBox2.Text += "\r\n" + Emoji.Use.Loudspeaker + "Please import simulation data....." + "\r\n";
                                CMDTextBox2.ScrollToEnd();
                            }
                            else
                            {
                                CMDTextBox2.Text += "\r\n" + Emoji.Use.Open_File_Folder + "Pressure Simulation Activated....." + "\r\n";
                                CMDTextBox2.ScrollToEnd();

                                SendCSV();

                                SoundPlayer player = new(binAppPath + "/Audio/SIMP.wav");
                                player.Play();
                                CMDTextBox1.Clear();
                                CMDShortcut.SelectedIndex = -1;
                            }
                        }
                        catch
                        {
                            return;
                        }
                    }
                    else if (int.TryParse(lineSplit, out _))
                    {
                        string cmd = "CMD,1085," + lineSplit;

                        _serialPort.WriteLine(cmd);
                        

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Pencil + "PRESSURE " + lineSplit + " is being sended....." + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SerialDataStatus.Content = "SIMP" + lineSplit;
                        SoundPlayer player = new(binAppPath + "/Audio/SIMP.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    else
                    {
                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Loudspeaker + "Please enter the correct SIMP command!" + "\r\n";
                        CMDTextBox2.ScrollToEnd();
                    }
                }
                else if (line.Contains("CMD,1085,"))
                {
                    string lineSplit = line.Split(',')[2];
                    try
                    {
                        string words;
                        string words_status;
                        if (lineSplit == "LCK")
                        {
                            words = "Lock";
                            words_status = "LCK_MECHANISM";

                        }
                        else if (lineSplit == "HSR")
                        {
                            words = "Heat Shield";
                            words_status = "HS_RELEASED";
                        }
                        else if (lineSplit == "UPR")
                        {
                            words = "Uprighting";
                            words_status = "HS_MECHANISM";
                        }
                        else
                        {
                            return;
                        }
                        string cmd = "CMD,1085," + lineSplit;

                        _serialPort.WriteLine(cmd);
                        

                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Wrench + " " + words + " Mechanism Activated" + "\r\n";
                        CMDTextBox2.ScrollToEnd();

                        SerialDataStatus.Content = words_status;
                        SoundPlayer player = new(binAppPath + "/Audio/MECHANISM.wav");
                        player.Play();
                        CMDTextBox1.Clear();
                        CMDShortcut.SelectedIndex = -1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    CMDTextBox2.Text += "\r\n" + Emoji.Use.Information_Source + "----+The Command You entered Is Not Recognized.+----" + "\r\n";
                    CMDTextBox2.ScrollToEnd();

                    SoundPlayer player = new(binAppPath + "/Audio/TRY.wav");
                    player.Play();
                    CMDTextBox1.Clear();
                }
            }
        }

        public void CMDTextBox_KeyPress(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if (_serialPort.IsOpen == true)
            {

                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                try
                {
                    if (e.Key == Key.Return)
                    {
                        e.Handled = true;
                        CommandToPayload();
                    }
                } catch
                {
                    return;
                }
            } else
            {
                try
                {
                    if(e.Key == Key.Return)
                    {
                        e.Handled = true;
                        SoundPlayer player = new(binAppPath + "/Audio/CONNECTTO.wav");
                        CMDTextBox2.Text += "\r\n" + Emoji.Use.Information_Source + "Connect To Serial Port First!" + "\r\n";
                        CMDTextBox1.Clear();
                    }
                } catch
                {
                    return;
                }
            }
        }

        public void CMDSendButtonClick(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen == true)
            {

                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                try
                {
                    CommandToPayload();
                }
                catch
                {
                    return;
                }
            }
            else
            {
                try
                {
                    SoundPlayer player = new(binAppPath + "/Audio/CONNECTTO.wav");
                    CMDTextBox2.Text += "\r\n" + Emoji.Use.Information_Source + "Connect To Serial Port First!" + "\r\n";
                    CMDTextBox1.Clear();
                }
                catch
                {
                    return;
                }
            }
        }

        private void TimerSimulation_Tick(object sender, EventArgs e)
        {
            try
            {
                string cmd = "CMD,1085,SIMP," + Col4[timerCSV] + "\r";
                _serialPort.Write(cmd);
                SerialDataStatus.Content = "SIMP"+ Col4[timerCSV];
                SerialControlTextBox.Text += Emoji.Use.Pencil + " CMD,1085,SIMP," + Col4[timerCSV] + "\n";
            }
            catch (Exception)
            {
                return;
            }
            timerCSV++;
        }

        private void SendCSV()
        {
            try
            {
                var filePath = openFileDialog.FileName;

                using StreamReader reader = new(filePath);
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
                    GC.Collect();
                }
            } catch
            {
                return; 
            }
        }

        private void BtnCsv_Click(object sender, EventArgs e)
        {
            if (PayloadDataCsv.Items.Count > 0)
            {
                System.Windows.Forms.SaveFileDialog sfd = new()
                {
                    Filter = "CSV (*.csv)|*.csv",
                    FileName = "Flight_" + teamId + ".csv"
                };
                bool fileError = false;
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    if (System.IO.File.Exists(sfd.FileName))
                    {

                        try
                        {
                            System.IO.File.Delete(sfd.FileName);
                        }
                        catch (IOException ex)
                        {
                            fileError = true;
                            System.Windows.Forms.MessageBox.Show("It wasn't possible to write the data to the disk." + ex.Message);
                        }
                    }
                    if (!fileError)
                    {

                        try
                        {
                            int columnCount = PayloadDataCsv.Columns.Count;
                            string columnNames = "";
                            string[] outputCsv = new string[10000];
                            for (int i = 0; i < columnCount; i++)
                            {
                                columnNames += PayloadDataCsv.Columns[i].Header.ToString() + ",";
                                GC.Collect();
                            }
                            outputCsv[0] += columnNames;
                            
                            var cellInfos = PayloadDataCsv.SelectedCells;
                            System.Diagnostics.Debug.WriteLine("Count - " + PayloadDataCsv.Items.Count);

                            
                            for (int i = 0; i < PayloadDataCsv.Items.Count; i++)
                            {
                                for (int j = 0; j < PayloadDataCsv.Columns.Count; j++)
                                {
                                    System.Diagnostics.Debug.WriteLine("Count - " + PayloadDataCsv.Items.Count + " " + i + " " + j);
                                    var cellContent = PayloadDataCsv.Columns[j].GetCellContent(PayloadDataCsv.Items[i]);
                                    if (cellContent != null && cellContent is TextBlock)
                                    {
                                        var konten = (cellContent as TextBlock).Text;
                                        System.Diagnostics.Debug.WriteLine("Test - " + konten);
                                        outputCsv[i + 1] += konten + ",";
                                    }
                                }
                                GC.Collect();
                            }


                            System.IO.File.WriteAllLines(sfd.FileName, outputCsv, Encoding.UTF8);
                            System.Windows.Forms.MessageBox.Show("Data Exported Successfully !!!", "Info");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show("Error :" + ex.Message);
                        }
                    }
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("No Record To Export !!!", "Info");
            }
        }

        private void MapTerrainClick(object sender, RoutedEventArgs e) {
            if (GmapViewHome.MapProvider == null)
            {
                GmapViewHome.MapProvider = GoogleTerrainMapProvider.Instance;
            }
            else
            {
                if (GmapViewHome.MapProvider != GoogleTerrainMapProvider.Instance)
                {
                    GmapViewHome.MapProvider = GoogleTerrainMapProvider.Instance;
                }
            }   
        }
        private void MapStreetClick(object sender, RoutedEventArgs e) {
            if (GmapViewHome.MapProvider == null)
            {
                GmapViewHome.MapProvider = GoogleMapProvider.Instance;
            }
            else
            {
                if (GmapViewHome.MapProvider != GoogleMapProvider.Instance)
                {
                    GmapViewHome.MapProvider = GoogleMapProvider.Instance;
                }
            }
        }
        private void MapSatellitClick(object sender, RoutedEventArgs e) {
            if (GmapViewHome.MapProvider == null)
            {
                GmapViewHome.MapProvider = GoogleSatelliteMapProvider.Instance;
            }
            else
            {
                if (GmapViewHome.MapProvider != GoogleSatelliteMapProvider.Instance)
                {
                    GmapViewHome.MapProvider = GoogleSatelliteMapProvider.Instance;
                }
            }
        }

        private void ThreeDModelActivate(object sender, RoutedEventArgs e)
        {
            if((string)ThreeDModelBtn.Content == "Started")
            {
                model.Content = null;
                ThreeDModelStatus.Content = "Not Started";
                ThreeDModelBtn.Content = "Start";
            } else
            {
                fileobj = System.AppDomain.CurrentDomain.BaseDirectory + "/Assets/3D/Probe_Stowed.obj";
                ThreeDModelStatus.Content = "Startsed";
                ThreeDModelBtn.Content = "Started";
                HelixViewport3D_Load();
            }
        }

        private void MiniMapActivate(object sender, RoutedEventArgs e)
        {
            if ((string)MiniMapBtn.Content == "Started")
            {
                GmapView.Visible = false;
                MiniMapStatus.Content = "Not Started";
                MiniMapBtn.Content = "Start";
            }
            else
            {
                MiniMapStatus.Content = "Started";
                MiniMapBtn.Content = "Started";
                GmapView_Load();
            }
        }

        private void CMDShortcutChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CMDShortcut.SelectedItem != null)
            {
                string command = CMDShortcut.SelectedItem.ToString();
                CMDShortcut.SelectedItem = null;
                command = command.Split(' ')[1];
                if (command == "CAL")
                {
                    CMDTextBox1.Text = "CMD,1085,CAL";
                }
                else if (command == "CXON")
                {
                    CMDTextBox1.Text = "CMD,1085,CX,ON";
                }
                else if (command == "CXOFF")
                {
                    CMDTextBox1.Text = "CMD,1085,CX,OFF";
                }
                else if (command == "ST")
                {
                    CMDTextBox1.Text = "CMD,1085,ST,";
                }
                else if (command == "SIMENABLE")
                {
                    CMDTextBox1.Text = "CMD,1085,SIM,ENABLE";
                }
                else if (command == "SIMDISABLE")
                {
                    CMDTextBox1.Text = "CMD,1085,SIM,DISABLE";
                }
                else if (command == "SIMACTIVATE")
                {
                    CMDTextBox1.Text = "CMD,1085,SIM,ACTIVATE";
                }
                else if (command == "SIMP")
                {
                    CMDTextBox1.Text = "CMD,1085,SIMP,PRESSURE";
                }
                else if (command == "MECHANIC")
                {
                    CMDTextBox1.Text = "CMD,1085,LCK/HSR/UPR";
                }
                else if (command == "GCSRESET")
                {
                    CMDTextBox1.Text = "CMD,GCS,RESET";
                }
                else if (command == "GCSTEST")
                {
                    CMDTextBox1.Text = "CMD,GCS,TESTMODE";
                }

            }
        }

        private void AutoScrollActivate(object sender, RoutedEventArgs e)
        {
            if (AutoScrollBtn.IsChecked == true)
            {
                auto_scroll = true;
            }
            else
            {
                auto_scroll = false;
            }
        }


    }
}