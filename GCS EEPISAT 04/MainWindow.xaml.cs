using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Device;
using System.Device.Location;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Media;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Management;
using System.Timers;
// using OPCWPFDashboard;

// Map
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.ObjectModel;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.WindowsForms.ToolTips;

// Graph
using LiveCharts;
using LiveCharts.Wpf;
using System.Security.Permissions;
using System.Data.Entity;
//using AutoFixture.Kernel;

namespace GCS_EEPISAT_04
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort _serialPort = new SerialPort();
        enum Sequencer { readSensor };
        Sequencer stepData = Sequencer.readSensor;
        string dataSensor;

        public delegate void uiupdater();

        int dataSum = 0;
        bool isAscii = false;
        string[] splitData;

        public delegate void AddDataDelegate(String myString);

        public AddDataDelegate WriteTelemetryData;

        StreamWriter writePay;
        string payloadFileLog = "";
        int logDataCountTP = 0;
        ushort clickTP = 0;

        // bin App path
        string binAppPath = System.AppDomain.CurrentDomain.BaseDirectory;


        // Variabel Probe Log
        double teamId;
        string state;
        string missionTime;

        double packetCount;
        double max_packetCount;
        double max_max_packetCount;
        string mode; // F & S

        double altitude;
        double max_altitude;
        double max_max_altitude;
        string hs_status;
        string pc_status;
        string mast_status;
        double temperature;
        double max_temperature;
        double max_max_temperature;
        double voltage;
        double max_voltage;
        double max_max_voltage;
        string gps_time;
        double gps_altitude;
        double gps_latitude;
        double gps_longitude;
        uint gps_sats_count;
        double tilt_x;
        double tilt_y;
        string cmd_echo;

        // battery
        static int jumlahDataRegLin;
        static float sumX, sumY, sumX2, sumXY;
        static float x, y, m, c;
        float hasilRegLin;



        // TimerSimulation
        DispatcherTimer timerSimulation = new DispatcherTimer();
        DispatcherTimer timergraph = new DispatcherTimer();

        DispatcherTimer aTimer;

        private GeoCoordinateWatcher watcher = null;

        public delegate void MethodInvoker();

        // Serial COM PORT
        ManagementEventWatcher detector;
        string SerialPortNumber;
        private static string MustHavePort = "COM3";


        System.Windows.Threading.DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();


            _serialPort.DataReceived += new SerialDataReceivedEventHandler(serialport_datareceive);


            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        public void USBChangedEvent(object sender, EventArrivedEventArgs e)
        {
            (sender as ManagementEventWatcher).Stop();

            Dispatcher.Invoke((MethodInvoker) delegate
            {
                ManagementObjectSearcher deviceList = new ManagementObjectSearcher("Select Name, Description, Description, DeviceID from Win32_SerialPort");

                // List to store available USB serial devices plugged in 
                List<String> CompPortList = new List<String>();

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
                        CompPortList.Add(SerialPortNumber);
                        ComportDropdown.Items.Add(SerialPortNumber);
                    }
                }
                else
                {
                    ComportDropdown.Items.Add("NO SerialPorts AVAILABLE!");
                    // Inform the user about the disconnection of the arduino ON PORT 3 etc...
                }

                if (!CompPortList.Contains(MustHavePort))
                {
                    // Inform the user about the disconnection of the arduino ON PORT 3 etc...
                }
            });
            (sender as ManagementEventWatcher).Start();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            watcher = new GeoCoordinateWatcher();
            watcher.StatusChanged += watcher_StatusChanged;
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
            }
            catch (NullReferenceException)
            {

            }
        
        }

        private void WindowClosed(object sender, RoutedEventArgs e)
        {

        }

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

                        //Pengecekan 
                        //  Panjang Data               Team ID          Team ID            Mission Time       Packet Count         
                    if (splitData.Length == 19 && splitData[0] == "1085" && splitData[0].Length == 4 && splitData[1].Length == 11 && splitData[2].Length <= 4 &&
                             //  Packet Type         // Altitude             
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
                if (splitData.Length == 19)
                {
                    //
                    //payloadTeamID = Convert.ToDouble(splitData[16]);
                    if (splitData[16].Length > 0 && splitData[16].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        teamId = Convert.ToDouble(splitData[16]);
                    }
                    else
                    {
                        teamId = 0;
                    }

                    missionTime = splitData[17];

                    //payloadPacketCount = Convert.ToDouble(splitData[18]);
                    if (splitData[18].Length > 0 && splitData[18].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        packetCount = Convert.ToDouble(splitData[18]);
                    }
                    else
                    {
                        packetCount = 0;
                    }

                    //payloadAltitude = Convert.ToDouble(splitData[20]);
                    if (splitData[20].Length > 0 && splitData[20].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        altitude = Convert.ToDouble(splitData[20]);
                    }
                    else
                    {
                        altitude = 0;
                    }

                    //payloadTemperature = Convert.ToDouble(splitData[21]);
                    if (splitData[21].Length > 0 && splitData[21].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        temperature = Convert.ToDouble(splitData[21]);
                    }
                    else
                    {
                        temperature = 0;
                    }

                    //payloadVoltage = Convert.ToDouble(splitData[22]);
                    if (splitData[22].Length > 0 && splitData[22].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        voltage = Convert.ToDouble(splitData[22]);
                    }
                    else
                    {
                        voltage = 0;
                    }


                    state = splitData[33];
                    //if (splitData[33].Length > 0 && splitData[33].All(Char.IsLetter))
                    //{
                    //    payloadSoftwareState = splitData[33];
                    //}
                    //else
                    //{
                    //    payloadSoftwareState = "";
                    //}


                    //Pengecekan Telemetry Container
                } if (splitData.Length == 0)
                {
                    //payloadTeamID = Convert.ToDouble(splitData[0]);
                    if (splitData[0].Length > 0 && splitData[0].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        teamId = Convert.ToDouble(splitData[0]);
                    }
                    else
                    {
                        teamId = 0;
                    }

                    missionTime = splitData[1];

                    //payloadPacketCount = Convert.ToDouble(splitData[2]);
                    if (splitData[2].Length > 0 && splitData[2].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        packetCount = Convert.ToDouble(splitData[2]);
                    }
                    else
                    {
                        packetCount = 0;
                    }


                    //payloadAltitude = Convert.ToDouble(splitData[4]);
                    if (splitData[4].Length > 0 && splitData[4].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        altitude = Convert.ToDouble(splitData[4]);
                    }
                    else
                    {
                        altitude = 0;
                    }

                    //payloadTemperature = Convert.ToDouble(splitData[5]);
                    if (splitData[5].Length > 0 && splitData[5].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        temperature = Convert.ToDouble(splitData[5]);
                    }
                    else
                    {
                        temperature = 0;
                    }

                    //payloadVoltage = Convert.ToDouble(splitData[6]);
                    if (splitData[6].Length > 0 && splitData[6].All(c => Char.IsNumber(c) || c == '.'))
                    {
                        voltage = Convert.ToDouble(splitData[6]);
                    }
                    else
                    {
                        voltage = 0;
                    }

                    state = splitData[17];
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
                    cmd_echo = splitData[18];
                }


                // checkSum = Convert.ToDouble(splitData[34]);

                //for 3d Modelling
                //foreach (HelixToolkit.Wpf.Polygon polygon in polygons)
                //{
                //    //polygon.Transformation.RotateX = (float)payloadGyro_R;
                //    //polygon.Transformation.RotateY = (float)payloadGyro_P;
                //    //polygon.Transformation.RotateZ = (float)payloadGyro_Y;
                //}

                max_voltage = voltage;
                if (Math.Abs(max_voltage) > this.max_max_voltage)
                {
                    max_max_voltage = Math.Abs(max_voltage);
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
                }

                max_packetCount = packetCount;
                if (Math.Abs(max_packetCount) > this.max_max_packetCount)
                {
                    max_max_packetCount = Math.Abs(max_packetCount);
                }

                this.Dispatcher.BeginInvoke(new uiupdater(ShowTelemetryData));
                //this.Dispatcher.Invoke(new uiupdater(GraphCurve));
                //GraphCurve();
                //this.Dispatcher.BeginInvoke(new uiupdater(GmapView_Refresh));
                //this.Dispatcher.BeginInvoke(new uiupdater(GmapView_Region));
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
            //missionTimeLabel.Content = String.Format("{00:00:00.00}", containerMissionTime);
            //softwareStateLabel.Content = String.Format("{0}", softwareState);

            //#region 3D Item

            //String payloadGyro_R_formated = String.Format("{0:0.00}", payloadGyro_R);
            //String payloadGyro_P_formated = String.Format("{0:0.00}", payloadGyro_P);
            //String payloadGyro_Y_formated = String.Format("{0:0.00}", payloadGyro_Y);

            ////Magnetometer
            //RollValue.Content = payloadGyro_R_formated;
            //PitchValue.Content = payloadGyro_P_formated;
            //YawValue.Content = payloadGyro_Y_formated;
            //RPY_TextBox.AppendText(payloadGyro_R_formated + "," + payloadGyro_P_formated + "," + payloadGyro_Y_formated + "\n");
            //RPY_TextBox.SelectionLength = RPY_TextBox.Text.Length;
            //RPY_TextBox.ScrollToEnd();

            ////Vector3D axis;

            ////if (payloadGyro_R != 0 && payloadGyro_P != 0 && payloadGyro_Y != 0)
            ////{
            ////    axis = new Vector3D((float)payloadGyro_R, (float)payloadGyro_P, (float)payloadGyro_Y);
            ////}
            ////else
            ////{
            ////    axis = new Vector3D(0, 0, 0);
            ////}
            ////var angle = 50;

            ////Matrix3D matrix = model.Content.Transform.Value;

            ////matrix.Rotate(new Quaternion(axis, angle));

            ////model.Content.Transform = new MatrixTransform3D(matrix);

            //#endregion

            //#region Graph Item

            ////
            //PayloadPacketCountValue.Content = String.Format("{0:0000}", payloadPacketCount);
            //PayloadAltitudeValue.Content = String.Format("{0:000.0}", payloadAltitude);
            //PayloadTemperatureValue.Content = String.Format("{0:00.0}", payloadTemperature);
            //PayloadVoltageValue.Content = String.Format("{0:0.0}", payloadVoltage);

            ////Container
            //ContainerAltitudeGraph.Content = String.Format("{0:000.0}", altitude);
            //PayloadAltitudeGraph.Content = String.Format("{0:000.0}", payloadAltitude);
            //GPSAltitudeGraph.Content = String.Format("{0:000.0}", gpsAltitude);
            //ContainerTemperatureGraph.Content = String.Format("{0:00.0}", temperature);
            //PayloadTemperatureGraph.Content = String.Format("{0:00.0}", payloadTemperature);

            ////Voltage
            //ContainerVoltageGraph.Content = String.Format("{0:0.0}", voltage);

            ////Accelerometer
            //PayloadAccelerometer_R.Content = String.Format("{0:0.00}", payloadAccel_R);
            //PayloadAccelerometer_P.Content = String.Format("{0:0.00}", payloadAccel_P);
            //PayloadAccelerometer_Y.Content = String.Format("{0:0.00}", payloadAccel_Y);

            ////Gyrometer
            //PayloadGyrometer_R.Content = String.Format("{0:0.00}", payloadGyro_R);
            //PayloadGyrometer_P.Content = String.Format("{0:0.00}", payloadGyro_P);
            //PayloadGyrometer_Y.Content = String.Format("{0:0.00}", payloadGyro_Y);

            ////Magnetometer
            //PayloadMagnetometer_R.Content = String.Format("{0:0.00}", payloadMag_R);
            //PayloadMagnetometer_P.Content = String.Format("{0:0.00}", payloadMag_P);
            //PayloadMagnetometer_Y.Content = String.Format("{0:0.00}", payloadMag_Y);

            //#endregion

            //try
            //{
            //    #region Dashboard Item

            //    teamIDLabel.Content = String.Format("{0:0000}", containerTeamID);
            //    //missionTimeLabel.Text = DateTime.Now.ToString("hh:mm:ss.ss");
            //    //missionTimeLabel.Text = String.Format("{00:00:00.00}", containerMissionTime);
            //    altitudeValue.Content = String.Format("{0:000.0}", altitude);
            //    altitudeMaxValue.Content = String.Format("{0:000.0}", maxMaxAltitude);
            //    voltageValue.Content = String.Format("{0:0.00}", voltage);
            //    temperatureValue.Content = String.Format("{0:00.0}", temperature);
            //    containerPacketCountValue.Content = String.Format("{0:0000}", containerPacketCount);
            //    GPSLatitudeValue.Content = String.Format("{0:0.0000}", gpsLatitude);
            //    GPSLongitudeValue.Content = String.Format("{0:000.0000}", gpsLongitude);
            //    GPSAltitudeValue.Content = String.Format("{0:000.0}", gpsAltitude);
            //    GPSSatsValue.Content = String.Format("{0:00}", gpsSatelite);
            //    GPSTimeValue.Content = String.Format("{0:00:00.00}", gpsTime);
            //    //softwareStateLabel.Text = String.Format("{0}", softwareState);
            //    CMDEchoLabel.Content = String.Format("{0}", cmdEcho);

            //    //
            //    PayloadPacketCountValue.Content = String.Format("{0:0000}", payloadPacketCount);
            //    PayloadTemperatureValue.Content = String.Format("{0:00.0}", payloadTemperature);
            //    PayloadAltitudeValue.Content = String.Format("{0:000.0}", payloadAltitude);
            //    PayloadVoltageValue.Content = String.Format("{0:0.00}", payloadVoltage);
            //    payloadSoftwareStateLabel.Content = String.Format("{0}", payloadSoftwareState);

            //    //accelerometer
            //    ACCEL_RValue.Content = String.Format("{0:0.000}", payloadAccel_R);
            //    ACCEL_PValue.Content = String.Format("{0:0.000}", payloadAccel_P);
            //    ACCEL_YValue.Content = String.Format("{0:0.000}", payloadAccel_Y);


            //    //gyro
            //    GYRO_RValue.Content = String.Format("{0:0.000}", payloadGyro_R);
            //    GYRO_PValue.Content = String.Format("{0:0.000}", payloadGyro_P);
            //    GYRO_YValue.Content = String.Format("{0:0.000}", payloadGyro_Y);

            //    //magnetometer
            //    MAG_RValue.Content = String.Format("{0:0.000}", payloadMag_R);
            //    MAG_PValue.Content = String.Format("{0:0.000}", payloadMag_P);
            //    MAG_YValue.Content = String.Format("{0:0.000}", payloadMag_Y);

            //    //pointing error
            //    pointingErrorValue.Content = String.Format("{0:0.0}", payloadPointingError);
            //    #endregion

            //    #region Map Item

            //    #endregion

            //    PayloadReleased.Content = String.Format("{0}", tpReleased);
            //    if (tpReleased == "R")
            //    {
            //        PayloadReleased.Content = "R";
            //        PayloadReleasedPane.Background = System.Windows.Media.Brushes.ForestGreen;
            //    }
            //    else if (tpReleased == "N")
            //    {
            //        PayloadReleased.Content = "N";
            //        PayloadReleasedPane.Background = System.Windows.Media.Brushes.Firebrick;
            //    }

            //    if (cmdEcho == "SIMENABLE" && mode == "F")
            //    {
            //        FlightOnOffSwitch.IsChecked = false;

            //        SimulationStatus.Content = "ENABLE";
            //        SimulationStatus.Background = System.Windows.Media.Brushes.ForestGreen;
            //    }
            //    else if (cmdEcho == "SIMACTIVATE" && mode == "S")
            //    {
            //        FlightOnOffSwitch.IsChecked = false;

            //        SimulationStatus.Content = "ACTIVE";
            //        SimulationStatus.Background = System.Windows.Media.Brushes.ForestGreen;
            //    }
            //    else if (cmdEcho == "SIMDISABLE" && mode == "F")
            //    {
            //        FlightOnOffSwitch.IsChecked = false;

            //        SimulationStatus.Content = "DISABLE";
            //        SimulationStatus.Background = System.Windows.Media.Brushes.Firebrick;
            //    }
            //}
            //catch (NullReferenceException)
            //{
            //    return;
            //}
            //catch (IndexOutOfRangeException)
            //{
            //    return;
            //}
            //catch (FormatException)
            //{
            //    return;
            //}
            //catch (Exception ex)
            //{
            //    //System.Diagnostics.Debug.WriteLine("Error - " + ex.Message);
            //    //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
        }

        private void PayDataLog()
        {
            if (_serialPort.IsOpen == true)
            {
                try
                {
                    try
                        {
                            BackgroundWorker worker = new BackgroundWorker();
                            worker.DoWork += delegate (object s, DoWorkEventArgs args)
                            {
                                logDataCountTP = 0;
                                //LogDataPayload1Indicator.BackColor = Color.FromArgb(52, 168, 83);
                                clickTP++;
                                payloadFileLog = binAppPath + "\\LogData\\FLIGHT_1010.csv";
                                var filePayload = File.Open(payloadFileLog, (FileMode)FileIOPermissionAccess.Append, FileAccess.Write, FileShare.Read);
                                writePay = new StreamWriter(filePayload, Encoding.GetEncoding(1252));
                                writePay.AutoFlush = true;
                                writePay.Write("Team ID, Mission Time, Packet Count, Mode, State, Altitude, HS Deployed," +
                                               "PC Deployed, Mast Raised, Temperature, Voltage, GPS Time, GPS Altitude, GPS Latitude, GPS Longitude, GPS Sats," +
                                               "Tilt X, Tilt Y, CMD Echo \n");
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
                    try
                        {
                            string dataBuffer = String.Format("{0:0000}", teamId);
                            writePay.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}\n"
                                         , teamId, missionTime, packetCount, mode
                                         , state, altitude, hs_status, pc_status, mast_status,
                                         temperature, voltage, gps_time, gps_altitude, gps_latitude, gps_longitude, 
                                         gps_sats_count, tilt_x, tilt_y, cmd_echo);
                            writePay.Flush();
                            DataTable PayloadDataCsv;
                            DataPayload dtp = new DataPayload();
                            dtp.Pteamid = teamId;
                            dtp.Pmissiontime = missionTime;
                            dtp.Ppacketcount = packetCount;
                            dtp.Pmode = mode;
                            dtp.Pstate = state;
                            dtp.Paltitude = altitude;
                            dtp.Pmast_status = mast_status;
                            dtp.Phs_status = hs_status;
                            dtp.Ppc_status = pc_status;
                            dtp.Pmast_status = mast_status;
                            dtp.Ptemperature = temperature;
                            dtp.Pvoltage = voltage;
                            dtp.Pgps_time = gps_time;
                            dtp.Pgps_altitude = gps_altitude;
                            dtp.Pgps_latitude = gps_latitude;
                            dtp.Pgps_longitude = gps_longitude;
                            dtp.Pgps_sats_count = gps_sats_count;
                            dtp.Ptilt_x = tilt_x;
                            dtp.Ptilt_y = tilt_y;
                            dtp.Pcmd_echo = cmd_echo;

                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                //PayloadDataCSv.Items.Add(dtp);
                                //PayloadDataCSv.ScrollIntoView(PayloadDataCSv.Items[PayloadDataCSv.Items.Count - 1]);
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
            public String Pmode { get; set; }
            public String Pstate { get; set; }
            public Double Paltitude { get; set; }
            public String Phs_status { get; set; }
            public String Ppc_status { get; set; }
            public String Pmast_status { get; set; }
            public Double Ptemperature { get; set; }
            public Double Pvoltage { get; set; }
            public String Pgps_time { get; set; }
            public Double Pgps_altitude { get; set; }
            public Double Pgps_latitude { get; set; }
            public Double Pgps_longitude { get; set; }
            public uint Pgps_sats_count { get; set; }
            public Double Ptilt_x { get; set; }
            public Double Ptilt_y { get; set; }
            public String Pcmd_echo { get; set; }
        }

        private void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e) //watcher status map
        {
            if (e.Status == GeoPositionStatus.Ready)
            {
                if (watcher.Position.Location.IsUnknown)
                {
                    //GCSLocationStatus.Content = "Can't get GCS location";
                    //GCSLocationStatusPane.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 67, 53));
                }
                else
                {
                    //GCSLocationStatus.Content = "GCS Location Founded";
                    //GCSLatitudeValue.Content = String.Format("{0:00.0000}", watcher.Position.Location.Latitude);
                    //GCSLongitudeValue.Content = String.Format("{0:000.0000}", watcher.Position.Location.Longitude);
                    //GCSLocationStatusPane.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 168, 83));
                }
            }
        }

        private void shutdownBtnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =  MessageBox.Show("Are you sure to shutdown the GCS?", "", MessageBoxButton.OKCancel);
            switch (result)
            {
                case MessageBoxResult.OK:
                    Application.Current.Shutdown();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }

        private void restartBtnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure to restart the GCS?", "", MessageBoxButton.OKCancel);
            switch (result)
            {
                case MessageBoxResult.OK:
                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                    Application.Current.Shutdown();
                    break;
                case MessageBoxResult.Cancel:
                    break;
            }
        }
        Dictionary<UInt16, string> StatusCodes;

        private void GetBatteryPercent()
        {
            ManagementClass wmi = new ManagementClass("Win32_Battery");
            var allBatteries =  wmi.GetInstances();
            int estimatedTimeRemaining, estimatedChargeRemaining;
            string final = "";
            foreach(var battery in allBatteries)
            {
                estimatedChargeRemaining = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
                estimatedTimeRemaining = Convert.ToInt32(battery["EstimatedRunTime"]);
                x = estimatedChargeRemaining;
                y = estimatedTimeRemaining;

                batteryPercentage.Height = Convert.ToDouble(estimatedChargeRemaining);
                lblBatteryStatus.Content = Convert.ToString(estimatedChargeRemaining) + "%" + " ";
                jumlahDataRegLin++;
            }

            sumX += x;
            sumX2 += x*x;
            sumY += y;
            sumXY += x*y;
            m = (jumlahDataRegLin * sumXY - sumX * sumY) / (jumlahDataRegLin*sumX2-sumX*sumX);
            c = (sumY - m*sumX) / jumlahDataRegLin;
            hasilRegLin = c + (m * x);
            final = Convert.ToString((int)hasilRegLin) + " Minutes Remaining";
            if (y == 71582788)
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (Brush)converter.ConvertFromString("#00FF00");
                batteryPercentage.Background = brush;
                final = "On Charging";
            }
            else if (y < 30)
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (Brush)converter.ConvertFromString("#FFFF00");
                batteryPercentage.Background = brush;
            }
            else if (y < 10)
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (Brush)converter.ConvertFromString("#FF0000");
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
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            GetBatteryPercent();
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

        private void homeNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 0;
        }

        private void graphNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 1;
        }

        private void mapNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 2;
        }

        private void dataCsvNavClick(object sender, RoutedEventArgs e)
        {
            MainPage.SelectedIndex = 3;
        }

        private void MainPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ConnectPortBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort.IsOpen == false)
            {
                try
                {
                    //ConnectPort.Image = Properties.Resources.icon_Disconnect;
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
                        //screenfilter.Visibility = Visibility.Visible;
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
                        //screenfilter.Visibility = Visibility.Visible;
                    }

                    else
                    {
                        _serialPort.PortName = ComportDropdown.SelectedItem.ToString();
                        _serialPort.BaudRate = Convert.ToInt32(BaudrateDropdown.SelectedItem);
                        _serialPort.NewLine = "\n";
                        _serialPort.Close();
                        _serialPort.Open();
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
                    //bgmessagebox.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD93D");
                    //TBContent.Text = "Error!";
                    //TBContent1.Text = ex.Message;
                    //MessageOK.Visibility = Visibility.Hidden;
                    //MessageNO.Visibility = Visibility.Hidden;
                    //MessageOK1.Visibility = Visibility.Visible;
                    //winhost.Visibility = Visibility.Hidden;
                    //MessageOK1.Focus();
                    //CMBox.Visibility = Visibility.Visible;
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

        }
        private void RestartPortBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
