using NationalInstruments.Analysis;
using NationalInstruments.Analysis.Conversion;
using NationalInstruments.Analysis.Dsp;
using NationalInstruments.Analysis.Dsp.Filters;
using NationalInstruments.Analysis.Math;
using NationalInstruments.Analysis.Monitoring;
using NationalInstruments.Analysis.SignalGeneration;
using NationalInstruments.Analysis.SpectralMeasurements;
using NationalInstruments;
using NationalInstruments.UI;
using NationalInstruments.DAQmx;
using NationalInstruments.NetworkVariable;
using NationalInstruments.NetworkVariable.WindowsForms;
using NationalInstruments.Tdms;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.Controls;
using NationalInstruments.Controls.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Drawing.Text;
using MySql.Data.MySqlClient;
using MColor = System.Windows.Media.Color;
using DColor = System.Drawing.Color;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;

using System.Data;
using System.IO;

namespace WpfApplication6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private AnalogMultiChannelReader analogInReader;
        private NationalInstruments.DAQmx.Task myTask;

        private NationalInstruments.DAQmx.Task runningTask;
        private AsyncCallback analogCallback;

        private AnalogWaveform<double>[] data;
        private DataColumn[] dataColumn = null;
        private DataTable dataTable = null;
        private DataTable refDataTable = null;
        private DataColumn[] refDataColumn = null;

        string[] colNames = new string[7];

        private static string dbID;
        private static string dbPwd;
        private static string dbName;
        private static bool isLogin = false;
        
        private static bool startTrigger = false;
        private static bool endTrigger = false;

        private double duration = Properties.Settings.Default.jobDuration * 1000;
        private int numOfdata;
        private int countRows;
        private int triggerSensitivity = 1;

        private int current_stitch;
        private string fault_stitch;
        private bool iswelding;
        
        private int measure_index;
        private int measured_id;

        //private int samplingGraphNum;
        private int samplingVariable;
        private int samplingTime;
        private int training_samplingGraphNum;
        private int training_samplingVariable;

        //private bool isGraphs = false;

        private bool isfirst = false;
        private bool holding = false;

        private static int fault_num = 0;
        private int fault_count;

        private int current_replay_rowNum = 0;
        private int replayRow = 0;
        private DataTable replaydt = new DataTable();
        private List<string> pathForReplay = new List<string>();
        private bool replayType = false;
        private static volatile bool stopReplay = false;
        private int replayCount = 0;

        private Thread addDataRunner;
        public delegate void AddDataDelegate();
        public AddDataDelegate addDataDel;
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
                this.WindowState = System.Windows.WindowState.Maximized;
            else if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                addDataRunner.Abort();
            }
            catch (NullReferenceException eThread)
            {

            }
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mainButtonGridCol1.Width.Value != 0)
            {
                this.mainButtonGridCol1.Width = new GridLength(0);
                this.slideBtn.Margin = new Thickness(10, 0, -15, 0);
                this.mainGridCol1.Width = new GridLength(40);
            }
            else
            {
                this.mainButtonGridCol1.Width = new GridLength(180);
                this.slideBtn.Margin = new Thickness(1, 0, -5, 0);
                this.mainGridCol1.Width = new GridLength(210);
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
                this.WindowState = System.Windows.WindowState.Maximized;
            else if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (startBtn.Content == "재 생")
            {
                stopReplay = true;
                replayCount++;
                replay(Properties.Settings.Default.replayFilePath);
            }
            else
                firstMainPerformance(isLogin);
        }

        private void firstMainPerformance(bool isref)
        {
            this.label_projectName.Content = Properties.Settings.Default.projectName;
            if (runningTask == null)
            {
                if (isref == true)
                {
                    jobNumGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                    jobNumGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    try
                    {
                        Properties.Settings.Default.jobNum = 1;
                        duration = Properties.Settings.Default.jobDuration * 1000;
                        try
                        {
                            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
                            MySqlConnection con = new MySqlConnection(conStr);

                            MySqlCommand comm = con.CreateCommand();
                            MySqlDataReader Reader;
                            con.Open();
                            comm.CommandText = "SELECT MAX(Measure_index) FROM `" + Properties.Settings.Default.databaseName + "`.`measured_data`";
                            Reader = comm.ExecuteReader();
                            Reader.Read();
                            measure_index = Convert.ToInt32(Reader[0]) + 1;
                            con.Close();

                        }
                        catch (Exception exc)
                        {
                            measure_index = 0;
                        }

                        try
                        {
                            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
                            MySqlConnection con = new MySqlConnection(conStr);

                            MySqlCommand comm = con.CreateCommand();
                            MySqlDataReader Reader;

                            con.Open();
                            comm.CommandText = "SELECT COUNT(*) FROM `" + Properties.Settings.Default.databaseName + "`.`measured_data`";
                            Reader = comm.ExecuteReader();
                            Reader.Read();
                            measured_id = Convert.ToInt32(Reader[0]) + 1;
                            con.Close();
                        }
                        catch (Exception exc)
                        {
                            measured_id = 0;
                        }

                        try
                        {
                            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
                            MySqlConnection con = new MySqlConnection(conStr);

                            MySqlCommand comm = con.CreateCommand();
                            MySqlDataReader Reader;

                            con.Open();
                            comm.CommandText = "SELECT COUNT(*) FROM `" + Properties.Settings.Default.databaseName + "`.`test_reference` WHERE Reference_id=" + Properties.Settings.Default.reference_id;
                            Reader = comm.ExecuteReader();
                            Reader.Read();
                            numOfdata = Convert.ToInt32(Reader[0]);
                            con.Close();

                            con.Open();

                            string commtext = "SELECT * FROM `" + Properties.Settings.Default.databaseName + "`.`test_reference` WHERE Reference_id=" + Properties.Settings.Default.reference_id;
                            MySqlDataAdapter mda = new MySqlDataAdapter(commtext, con);

                            DataSet ds = new DataSet();

                            mda.Fill(ds);
                            refDataTable = ds.Tables[0];
                            con.Close();
                        }
                        catch (Exception exc)
                        {

                        }

                        int txtFilePathIndex = Properties.Settings.Default.textFilePathSetting.LastIndexOf("\\");
                        string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + "SF LWM\\" + Properties.Settings.Default.projectName + "\\" + DateTime.Now.ToString("yyyy MM dd") + "_" + DateTime.Now.ToString("HH mm ss");
                        Properties.Settings.Default.txtFilePath = path;
                        DirectoryInfo f = new DirectoryInfo(path);
                        if (f.Exists == false)
                            f.Create();

                        countRows = 0;
                        fault_count = 0;
                        fault_stitch = "";
                        iswelding = false;
                        current_stitch = 0;
                        samplingVariable = Convert.ToInt32(1 / ((Properties.Settings.Default.samplesPerChannelNumeric * 100) / Properties.Settings.Default.rateNumeric));

                        this.label3.Content = Convert.ToString(Properties.Settings.Default.jobNum - 1);

                        dataTable = new DataTable();

                        chart1.Series[0].Points.Clear();
                        chart2.Series[0].Points.Clear();

                        int numofGraphs = Properties.Settings.Default.physicalChannelCheckedIndex.Length;

                        switch (numofGraphs)
                        {
                            case 0:
                                break;
                            case 1:
                                chart1.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(numOfdata);
                                chart1.ChartAreas[0].AxisX.Minimum = 0;
                                break;
                            case 2:
                                chart1.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(numOfdata);
                                chart1.ChartAreas[0].AxisX.Minimum = 0;
                                chart2.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(numOfdata);
                                chart2.ChartAreas[0].AxisX.Minimum = 0;
                                break;
                        }
                        
                        // Create a new task
                        myTask = new NationalInstruments.DAQmx.Task();

                        // Create a virtual channel
                        myTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.physicalChannel, "",
                            (AITerminalConfiguration)(-1), Convert.ToDouble(Properties.Settings.Default.minVoltage),
                            Convert.ToDouble(Properties.Settings.Default.maxVoltage), AIVoltageUnits.Volts);

                        // Configure the timing parameters
                        myTask.Timing.ConfigureSampleClock("", Properties.Settings.Default.rateNumeric,
                            SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);

                        // Verify the Task
                        myTask.Control(TaskAction.Verify);

                        // Prepare the table for Data
                        InitializeDataTable(myTask.AIChannels, ref dataTable);

                        runningTask = myTask;
                        analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                        analogCallback = new AsyncCallback(AnalogInCallback);

                        // Use SynchronizeCallbacks to specify that the object 
                        // marshals callbacks across threads appropriately.
                        analogInReader.SynchronizeCallbacks = true;
                        analogInReader.BeginReadWaveform(Convert.ToInt32(Properties.Settings.Default.samplesPerChannelNumeric),
                            analogCallback, myTask);
                    }
                    catch (DaqException exception)
                    {
                        // Display Errors
                        MessageBox.Show(exception.Message, "기기 연결을 확인해주세요");
                        runningTask = null;
                        myTask.Dispose();
                    }
                }
                else
                {
                    jobNumGrid.ColumnDefinitions[1].Width = new GridLength(0);
                    jobNumGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    try
                    {
                        Properties.Settings.Default.jobNum = 1;
                        duration = Properties.Settings.Default.jobDuration * 1000;
                        try
                        {
                            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
                            MySqlConnection con = new MySqlConnection(conStr);

                            MySqlCommand comm = con.CreateCommand();
                            MySqlDataReader Reader;
                            con.Open();
                            comm.CommandText = "SELECT MAX(Measure_index) FROM `" + Properties.Settings.Default.databaseName + "`.`measured_data`";
                            Reader = comm.ExecuteReader();
                            Reader.Read();
                            measure_index = Convert.ToInt32(Reader[0]) + 1;
                            con.Close();
                        }
                        catch (Exception exc)
                        {
                            measure_index = 0;
                        }

                        try
                        {
                            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
                            MySqlConnection con = new MySqlConnection(conStr);

                            MySqlCommand comm = con.CreateCommand();
                            MySqlDataReader Reader;

                            con.Open();
                            comm.CommandText = "SELECT COUNT(*) FROM `" + Properties.Settings.Default.databaseName + "`.`measured_data`";
                            Reader = comm.ExecuteReader();
                            Reader.Read();
                            measured_id = Convert.ToInt32(Reader[0]) + 1;
                            con.Close();
                        }
                        catch (Exception exc)
                        {
                            measured_id = 0;
                        }

                        numOfdata = Convert.ToInt32((Properties.Settings.Default.rateNumeric / Properties.Settings.Default.samplesPerChannelNumeric) * (duration / 1000));
                        int txtFilePathIndex = Properties.Settings.Default.textFilePathSetting.LastIndexOf("\\");
                        string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + "SF LWM\\" + Properties.Settings.Default.projectName + "\\" + DateTime.Now.ToString("yyyy MM dd") + "_" + DateTime.Now.ToString("HH mm ss");
                        Properties.Settings.Default.txtFilePath = path;
                        DirectoryInfo f = new DirectoryInfo(path);
                        if (f.Exists == false)
                            f.Create();

                        countRows = 0;
                        fault_count = 0;
                        fault_stitch = "";
                        iswelding = false;
                        current_stitch = 0;
                        samplingVariable = Convert.ToInt32(1 / ((Properties.Settings.Default.samplesPerChannelNumeric * 100) / Properties.Settings.Default.rateNumeric));

                        this.label3.Content = Convert.ToString(Properties.Settings.Default.jobNum - 1);

                        dataTable = new DataTable();

                        chart1.Series[0].Points.Clear();
                        chart2.Series[0].Points.Clear();

                        chart1.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(numOfdata);
                        chart1.ChartAreas[0].AxisX.Minimum = 0;
                        chart2.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(numOfdata);
                        chart2.ChartAreas[0].AxisX.Minimum = 0;

                        // Create a new task
                        myTask = new NationalInstruments.DAQmx.Task();

                        // Create a virtual channel
                        myTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.physicalChannel, "",
                            (AITerminalConfiguration)(-1), Convert.ToDouble(Properties.Settings.Default.minVoltage),
                            Convert.ToDouble(Properties.Settings.Default.maxVoltage), AIVoltageUnits.Volts);

                        // Configure the timing parameters
                        myTask.Timing.ConfigureSampleClock("", Properties.Settings.Default.rateNumeric,
                            SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);

                        // Verify the Task
                        myTask.Control(TaskAction.Verify);

                        // Prepare the table for Data
                        InitializeDataTable(myTask.AIChannels, ref dataTable);

                        runningTask = myTask;
                        analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                        analogCallback = new AsyncCallback(AnalogInCallback2);

                        // Use SynchronizeCallbacks to specify that the object 
                        // marshals callbacks across threads appropriately.
                        analogInReader.SynchronizeCallbacks = true;
                        analogInReader.BeginReadWaveform(Convert.ToInt32(Properties.Settings.Default.samplesPerChannelNumeric),
                            analogCallback, myTask);
                    }
                    catch (DaqException exception)
                    {
                        // Display Errors
                        MessageBox.Show(exception.Message, "기기 연결을 확인해주세요");
                        runningTask = null;
                        myTask.Dispose();
                    }
                }
            }
        }

        private void runMainPerformance()
        {
            if (runningTask == null)
            {
                try
                {
                    duration = Properties.Settings.Default.jobDuration * 1000;
                    countRows = 0;
                    fault_count = 0;
                    fault_stitch = "";
                    iswelding = false;
                    current_stitch = 0;
                    samplingVariable = Convert.ToInt32(1 / ((Properties.Settings.Default.samplesPerChannelNumeric * 100) / Properties.Settings.Default.rateNumeric));

                    //this.label3.Content = Convert.ToString(Properties.Settings.Default.jobNum - 1);
                    //this.label5.Content = Convert.ToString(fault_num);

                    dataTable = new DataTable();
                    chart1.Series[0].Points.Clear();
                    chart2.Series[0].Points.Clear();

                    // Create a new task
                    myTask = new NationalInstruments.DAQmx.Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.physicalChannel, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(Properties.Settings.Default.minVoltage),
                        Convert.ToDouble(Properties.Settings.Default.maxVoltage), AIVoltageUnits.Volts);

                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock("", Properties.Settings.Default.rateNumeric,
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);

                    // Verify the Task
                    myTask.Control(TaskAction.Verify);

                    // Prepare the table for Data
                    InitializeDataTable(myTask.AIChannels, ref dataTable);

                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback);

                    // Use SynchronizeCallbacks to specify that the object 
                    // marshals callbacks across threads appropriately.
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(Convert.ToInt32(Properties.Settings.Default.samplesPerChannelNumeric),
                        analogCallback, myTask);
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message, "기기 연결을 확인해주세요");
                    runningTask = null;
                    myTask.Dispose();
                }
            }
        }


        private void runMainPerformance2()
        {
            if (runningTask == null)
            {
                try
                {
                    duration = Properties.Settings.Default.jobDuration * 1000;
                    countRows = 0;
                    fault_count = 0;
                    fault_stitch = "";
                    iswelding = false;
                    current_stitch = 0;
                    samplingVariable = Convert.ToInt32(1 / ((Properties.Settings.Default.samplesPerChannelNumeric * 100) / Properties.Settings.Default.rateNumeric));

                    //this.label3.Content = Convert.ToString(Properties.Settings.Default.jobNum - 1);
                    //this.label5.Content = Convert.ToString(fault_num);

                    dataTable = new DataTable();

                    // Create a new task
                    myTask = new NationalInstruments.DAQmx.Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.physicalChannel, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(Properties.Settings.Default.minVoltage),
                        Convert.ToDouble(Properties.Settings.Default.maxVoltage), AIVoltageUnits.Volts);

                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock("", Properties.Settings.Default.rateNumeric,
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 100000);

                    // Verify the Task
                    myTask.Control(TaskAction.Verify);

                    // Prepare the table for Data
                    InitializeDataTable(myTask.AIChannels, ref dataTable);

                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback2);

                    // Use SynchronizeCallbacks to specify that the object 
                    // marshals callbacks across threads appropriately.
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(Convert.ToInt32(Properties.Settings.Default.samplesPerChannelNumeric),
                        analogCallback, myTask);
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message, "기기 연결을 확인해주세요");
                    runningTask = null;
                    myTask.Dispose();
                }
            }
        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);

                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(Properties.Settings.Default.samplesPerChannelNumeric),
                        analogCallback, myTask, data);

                    // Plot your data here
                    dataToDataTable(data, ref dataTable);

                }
            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
            }
        }

        private void AnalogInCallback2(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);

                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(Properties.Settings.Default.samplesPerChannelNumeric),
                        analogCallback, myTask, data);

                    // Plot your data here
                    dataToDataTable2(data, ref dataTable);

                }
            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
            }
        }

        private void dataToDataTable(AnalogWaveform<double>[] sourceArray, ref DataTable dataTable)
        {
            // Iterate over channels
            int currentLineIndex = 0;
            int i = 0;
            DataRow tmprow = dataTable.NewRow();
            int lengthOfSourceArray = sourceArray.GetLength(0);

            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                for (int sample = 0; sample < waveform.Samples.Count; sample++)
                {
                    if (waveform.Samples[i].Value > triggerSensitivity && countRows <= numOfdata)
                    {
                        startTrigger = true;
                        endTrigger = true;
                    }
                    else if (countRows >= numOfdata)
                        startTrigger = false;

                    if (sample == lengthOfSourceArray)
                        break;

                    tmprow[i] = waveform.Samples[i].Value;

                    if(countRows < refDataTable.Rows.Count && countRows > 0)
                    {
                        if ((double)refDataTable.Rows[countRows - 1][6] < 2)
                        {
                            if ((double)refDataTable.Rows[countRows][6] > 2)
                            {
                                current_stitch++;
                                iswelding = true;
                                fault_count = 0;
                            }

                        }
                        else
                        {
                            if ((double)refDataTable.Rows[countRows][6] < 2)
                            {
                                iswelding = false;
                                if (fault_count > 100)
                                {
                                    fault_num++;
                                    if (fault_stitch.Length > 0)
                                    {
                                        fault_stitch = fault_stitch + "," + current_stitch;
                                    }
                                    else
                                    {
                                        fault_stitch = current_stitch.ToString();
                                    }
                                    //uploadToDB_faultResult();
                                }
                                fault_count = 0;
                            }

                            if ((double)tmprow[i] > (double)refDataTable.Rows[countRows][i + 5] || (double)tmprow[i] < (double)refDataTable.Rows[countRows][i + 7])
                            {
                                fault_count++;
                            }
                        }
                    }
                }
                i++;
                currentLineIndex++;
            }
            if (lengthOfSourceArray == 1 && startTrigger == true && endTrigger == true)
            {
                countRows++;
                if (countRows % samplingVariable == 0)
                {
                    chart1.Series[0].Points.AddXY(countRows, tmprow[0]);
                }
            }
            else if (lengthOfSourceArray == 2 && startTrigger == true && endTrigger == true)
            {
                countRows++;
                if (countRows % samplingVariable == 0)
                {
                    chart1.Series[0].Points.AddXY(countRows, tmprow[0]);
                    chart2.Series[0].Points.AddXY(countRows, tmprow[1]);
                }
            }
            if (startTrigger == true)
            {
                dataTable.Rows.Add(tmprow);
            }
            else if (startTrigger == false && countRows >= numOfdata && endTrigger == true)
            {
                if (runningTask != null)
                {
                    runningTask = null;
                    dataTableToFile(ref dataTable);

                    if (fault_stitch.Length > 0)
                    {
                        uploadToDB_faultResult();
                    }

                    // Dispose of the task
                    myTask.Dispose();
                    countRows = 0;
                    endTrigger = false;
                    measure_index++;
                    Properties.Settings.Default.jobNum++;
                    this.label3.Content = Convert.ToString(Properties.Settings.Default.jobNum);
                    this.label5.Content = Convert.ToString(fault_num);
                    show_faultGrid();
                    runMainPerformance();
                }
            }
        }


        private void dataToDataTable2(AnalogWaveform<double>[] sourceArray, ref DataTable dataTable)
        {
            // Iterate over channels
            int currentLineIndex = 0;
            int i = 0;
            DataRow tmprow = dataTable.NewRow();
            int lengthOfSourceArray = sourceArray.GetLength(0);

            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                for (int sample = 0; sample < waveform.Samples.Count; sample++)
                {
                    if (waveform.Samples[i].Value > triggerSensitivity && countRows <= numOfdata)
                    {
                        if (holding == false)
                        {
                            chart1.Series[0].Points.Clear();
                            chart2.Series[0].Points.Clear();
                            holding = true;
                        }
                        startTrigger = true;
                        endTrigger = true;
                    }
                    else if (countRows >= numOfdata)
                        startTrigger = false;

                    if (sample == lengthOfSourceArray)
                        break;

                    tmprow[i] = waveform.Samples[i].Value;

                }
                i++;
                currentLineIndex++;
            }
            if (lengthOfSourceArray == 1 && startTrigger == true && endTrigger == true)
            {
                countRows++;
                if (countRows % samplingVariable == 0)
                {
                    chart1.Series[0].Points.AddXY(countRows, tmprow[0]);
                }
            }
            else if (lengthOfSourceArray == 2 && startTrigger == true && endTrigger == true)
            {
                countRows++;
                if (countRows % samplingVariable == 0)
                {
                    chart1.Series[0].Points.AddXY(countRows, tmprow[0]);
                    chart2.Series[0].Points.AddXY(countRows, tmprow[1]);
                }
            }
            if (startTrigger == true)
            {
                dataTable.Rows.Add(tmprow);
            }
            else if (startTrigger == false && countRows >= numOfdata && endTrigger == true)
            {
                if (runningTask != null)
                {
                    runningTask = null;
                    dataTableToFile(ref dataTable);

                    if (fault_stitch.Length > 0)
                    {
                        uploadToDB_faultResult();
                    }

                    // Dispose of the task
                    myTask.Dispose();
                    countRows = 0;
                    endTrigger = false;
                    holding = false;
                    measure_index++;
                    runMainPerformance2();
                    Properties.Settings.Default.jobNum++;
                    this.label3.Content = Convert.ToString(Properties.Settings.Default.jobNum);
                }
            }
        }

        public void InitializeDataTable(AIChannelCollection channelCollection, ref DataTable data)
        {
            int numOfChannels = channelCollection.Count;
            data.Rows.Clear();
            data.Columns.Clear();
            dataColumn = new DataColumn[numOfChannels];
            int numOfRows = 0;
            for (int currentChannelIndex = 0; currentChannelIndex < numOfChannels; currentChannelIndex++)
            {
                dataColumn[currentChannelIndex] = new DataColumn();
                dataColumn[currentChannelIndex].DataType = typeof(double);
                dataColumn[currentChannelIndex].ColumnName = channelCollection[currentChannelIndex].PhysicalName;
            }

            data.Columns.AddRange(dataColumn);

            for (int currentDataIndex = 0; currentDataIndex < numOfRows; currentDataIndex++)
            {
                object[] rowArr = new object[numOfChannels];
                data.Rows.Add(rowArr);
            }
        }


        private void dataTableToFile(ref DataTable dataTable)
        {
            int i = 0;
            StreamWriter sw = null;
            double timeUnit = 1 / Properties.Settings.Default.samplingSpeed;
            string project_index = Properties.Settings.Default.projectNum.ToString();

            sw = new StreamWriter(returnFileString(false), false);
            sw.Write("index;Measured_index;idProject;Time;Plsm;Temp;Refl");
            colNames[0] = "index";
            colNames[1] = "Measured_index";
            colNames[2] = "idProject";
            colNames[3] = "Time";
            colNames[4] = "Plsm";
            colNames[5] = "Temp";
            colNames[6] = "Refl";
            sw.WriteLine();
            int j = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                object[] array = row.ItemArray;
                //sw.Write(j + ";");
                sw.Write(measured_id + ";");
                sw.Write(measure_index + ";");
                sw.Write(project_index + ";");
                sw.Write(timeUnit * j + ";");

                for (i = 0; i < array.Length - 1; i++)
                {
                    sw.Write(array[i].ToString() + ";");
                }
                sw.Write(array[i].ToString());
                sw.WriteLine();
                measured_id++;
                j++;
            }
            sw.Close();
            uploadDB(returnFileString(true), Properties.Settings.Default.databaseName, Properties.Settings.Default.databaseID, Properties.Settings.Default.databasePwd);
        }
        
        private void uploadDB(string txtFileFullPath, string databaseName, string userID, string userPwd)
        {
            dbID = userID;
            dbPwd = userPwd;
            dbName = databaseName;

            string conStr = "Server=localhost;Database=" + dbName + ";Uid=" + dbID + ";Pwd=" + dbPwd;
            MySqlConnection con = new MySqlConnection(conStr);
            
            MySqlCommand comm = con.CreateCommand();
            try
            {
                MySqlBulkLoader bl = new MySqlBulkLoader(con);
                bl.TableName = "`" + dbName + "`.`measured_data`";
                bl.FieldTerminator = ";";
                bl.LineTerminator = "\n";
                bl.NumberOfLinesToSkip = 1;
                bl.FileName = txtFileFullPath;
                con.Open();
                int count = bl.Load();
                con.Close();
            }
            catch (MySqlException e)
            {
                MessageBox.Show("실행이 없거나, ID와 비밀번호가 틀립니다. 확인해주세요.");
            }
        }

        public void drawRefGraphBtn_Click()
        {
            /*
            if (isGraphs == false)
            {
                isGraphs = true;
                chart1.Series[1].Enabled = true;
                chart1.Series[2].Enabled = true;
                chart2.Series[1].Enabled = true;
                chart2.Series[2].Enabled = true;*/
                getDBT(Properties.Settings.Default.databaseName, Properties.Settings.Default.databaseID, Properties.Settings.Default.databasePwd);
            /*}
            else
            {
                isGraphs = false;
                chart1.Series[1].Enabled = false;
                chart1.Series[2].Enabled = false;
                chart2.Series[1].Enabled = false;
                chart2.Series[2].Enabled = false;
            }*/
        }

        private void drawLine(double x, double y, System.Windows.Forms.DataVisualization.Charting.Chart chart, int i)
        {
            chart.Series[i].Points.AddXY(x, y);
        }
        
        private void getDBT(string databaseName, string userID, string userPwd)
        {
            int numOfSelected = Properties.Settings.Default.physicalChannelCheckedIndex.Length;
            if (numOfSelected < 4)
            {
                isLogin = true;
                try
                {
                    string conStr = "Server=localhost;Database=" + databaseName + ";Uid=" + userID + ";Pwd=" + userPwd;
                    MySqlConnection con = new MySqlConnection(conStr);

                    con.Open();

                    string normalData = "test_reference";

                    if (con.State != ConnectionState.Open)
                    {
                        this.Close();
                    }
                    else
                    {
                        string tmp_ = Convert.ToString(con.Database.ElementAt(0));
                        MySqlDataAdapter mda = new MySqlDataAdapter("SELECT * FROM `"+ databaseName+"`.`" + normalData + "` WHERE reference_id="+Properties.Settings.Default.reference_id, con);

                        DataSet ds = new DataSet(normalData);

                        mda.Fill(ds);

                        refDataTable = ds.Tables[0]; DataRow row = refDataTable.Rows[0];

                        int i = 0;
                        int tmp_cur_row = 0;
                        training_samplingGraphNum = 0;
                        //speed problem solving
                        if (refDataTable.Rows.Count > 1000)
                            training_samplingVariable = (int)(refDataTable.Rows.Count / 1000);
                        else if (Properties.Settings.Default.samplingSpeed * Properties.Settings.Default.jobDuration > 1000)
                            training_samplingVariable = (int)(Properties.Settings.Default.samplingSpeed * Properties.Settings.Default.jobDuration / 1000);
                        else
                            training_samplingVariable = 1;
                        
                        chart1.Series[1].Points.Clear();
                        chart1.Series[2].Points.Clear();
                        while (i < refDataTable.Rows.Count)
                        {
                            if (training_samplingGraphNum % training_samplingVariable == 0)
                            {
                                drawLine(tmp_cur_row, Convert.ToDouble(refDataTable.Rows[training_samplingGraphNum][5].ToString()), chart1, 1);
                                drawLine(tmp_cur_row, Convert.ToDouble(refDataTable.Rows[training_samplingGraphNum][7].ToString()), chart1, 2);
                            }
                            i++;
                            tmp_cur_row++;
                            training_samplingGraphNum++;
                        }

                    }
                    con.Close();
                }
                catch (MySqlException e)
                {
                    isLogin = false;
                    MessageBox.Show(e.Message, "기준선이 없거나, ID와 비밀번호가 틀립니다. 확인해주세요.");
                }
            }
            else
                MessageBox.Show("3개 이하를 선택해 주세요.");
        }
        
        private string returnFileString(bool isupload)
        {
            int txtFilePathIndex = Properties.Settings.Default.textFilePathSetting.LastIndexOf("\\");
            int txtFilePathIndex2 = Properties.Settings.Default.textFilePathSetting.LastIndexOf(".");

            string txtFileName = Properties.Settings.Default.textFilePathSetting.Substring(txtFilePathIndex + 1, txtFilePathIndex2 - txtFilePathIndex - 1) + "_" + Properties.Settings.Default.projectName + "_" + Properties.Settings.Default.jobNum;
            string txtFilePath = Properties.Settings.Default.txtFilePath + "\\";

            if (isupload == false)
            {
                bool fileExist = File.Exists(txtFilePath + txtFileName + ".txt");

                while (fileExist == true)
                {
                    int tmp_find_ = txtFileName.LastIndexOf("_");
                    if (tmp_find_ > 0)
                    {
                        Properties.Settings.Default.jobNum++;
                        txtFileName = txtFileName.Substring(0, tmp_find_ + 1) + Convert.ToString(Properties.Settings.Default.jobNum);
                    }
                    else
                    {
                        txtFileName = txtFileName + "_" + Convert.ToString(Properties.Settings.Default.jobNum);
                    }
                    fileExist = File.Exists(txtFilePath + txtFileName + ".txt");
                }
                return txtFilePath + txtFileName + ".txt";
            }
            else
            {
                return txtFilePath + txtFileName + ".txt";
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (runningTask != null)
            {
                // Dispose of the task
                runningTask = null;
                myTask.Dispose();
                endTrigger = false;
                isfirst = false;
            }
            if(stopReplay == true)
            {
                stopReplay = false;
            }
            if(StopBtn.Content == "시작으로")
            {
                startBtn.Content = "시 작";
                StopBtn.Content = "정 지";
                startBtnGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            Window1 settings = new Window1(this);
            settings.ShowDialog();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            dataTableToFile(ref dataTable);
            uploadDB(returnFileString(true), Properties.Settings.Default.databaseName, Properties.Settings.Default.databaseID, Properties.Settings.Default.databasePwd);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Maximized;
            this.label_projectName.Content = Properties.Settings.Default.projectName;
            int numofGraphs = Properties.Settings.Default.physicalChannelCheckedIndex.Length;

            startBtnGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);

            switch (numofGraphs)
            {
                case 0:
                    break;
                case 1:
                    graphGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    graphGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
                    break;
                case 2:
                    graphGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    graphGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                    break;
            }
        }

        private void uploadToDB_faultResult()
        {
            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
            MySqlConnection con = new MySqlConnection(conStr);
            string[] fault = new string[4];
            int this_index;

            fault[0] = Properties.Settings.Default.projectName;
            
            MySqlCommand comm = con.CreateCommand();
            comm.CommandText = "SELECT Training_name FROM `"+Properties.Settings.Default.databaseName+"`.`offline_training` WHERE idTraining="+Properties.Settings.Default.reference_id;
            MySqlDataReader fault_reader;

            con.Open();
            fault_reader = comm.ExecuteReader();
            fault_reader.Read();
            fault[1] = (string)fault_reader[0];
            con.Close();

            con.Open();
            try
            {
                comm.CommandText = "SELECT MAX(`Index`) FROM `" + Properties.Settings.Default.databaseName + "`.`fault_result`";
                fault_reader = comm.ExecuteReader();
                fault_reader.Read();
                this_index = ((int)fault_reader[0])+1;
            }
            catch(Exception e)
            {
                this_index = 1;
            }
            con.Close();
            //fault_row["Training_name"] = training_name;

            string fileName = returnFileString(true);
            FileInfo fi = new FileInfo(fileName);
            DateTime time24 = Convert.ToDateTime(fi.LastWriteTime.ToString());
            //fault[2] = fi.LastWriteTime.ToString().Substring(0, 10) + " " + fi.LastWriteTime.ToString().Substring(fi.LastWriteTime.ToString().LastIndexOf(" ") + 1, fi.LastWriteTime.ToString().Length - fi.LastWriteTime.ToString().LastIndexOf(" ") - 1);
            fault[3] = Properties.Settings.Default.partName;
            //fault_row["Date"] = fi.LastWriteTime.ToString();
            //fault_row["Part"] = Properties.Settings.Default.partName;

            /*
            comm.CommandText = "SELECT Num_stitch FROM `" + Properties.Settings.Default.databaseName + "`.`project` WHERE idProject=" + Properties.Settings.Default.projectNum;
            con.Open();
            fault_reader = comm.ExecuteReader();
            fault_reader.Read();
            int fault_stitNum = (int)fault_reader[0];
            con.Close();
             */

            int fault_id = Properties.Settings.Default.reference_id;

            MySqlCommand cmd = new MySqlCommand("INSERT INTO fault_result (`Index`,Workspace_name,Training_name,Date,Part,Stitch,Training_id) VALUES (@index,@projectName,@trainingName,@date,@partName,@stitch,@idTraining)", con);
            //MySqlCommand cmd = new MySqlCommand("INSERT INTO fault_result (Workspace_name,Training_name,Part,Stitch,Training_id) VALUES (@projectName,@trainingName,@partName,@stitch,@idTraining)", con);
            cmd.Parameters.AddWithValue("@index", this_index);
            cmd.Parameters.AddWithValue("@projectName",fault[0]);
            cmd.Parameters.AddWithValue("@trainingName", fault[1]);
            //cmd.Parameters.AddWithValue("@date", fault[2]);
            cmd.Parameters.AddWithValue("@date", time24);
            cmd.Parameters.AddWithValue("@partName", fault[3]);
            //cmd.Parameters.AddWithValue("@stitch", fault_stitNum);
            cmd.Parameters.AddWithValue("@stitch", fault_stitch);
            cmd.Parameters.AddWithValue("@idTraining", fault_id);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

        }

        private void show_faultGrid()
        {
            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
            MySqlConnection con = new MySqlConnection(conStr);

            try
            {
                con.Open();
                MySqlDataAdapter mda = new MySqlDataAdapter("SELECT `Index`,Date,Part,Stitch FROM `" + Properties.Settings.Default.databaseName + "`.`fault_result`", con);

                DataSet ds = new DataSet();

                mda.Fill(ds, "LoadDataBinding5");
                dataGrid1.DataContext = ds;
                con.Close();
            }
            catch (MySqlException e)
            {
                MessageBox.Show("DB를 불러오는데 실패했습니다.");
            }
        }

        public void replay(string replayFilePath)
        {
            string[] replayColumns = null;
            var lines = File.ReadAllLines(replayFilePath);

            replaydt.Clear();
            replaydt.Columns.Clear();
            chart1.Invalidate();
            // assuming the first row contains the columns information
            if (lines.Count() > 0)
            {
                replayColumns = lines[0].Split(new char[] { ';' });

                foreach (var column in replayColumns)
                    replaydt.Columns.Add(column);
            }

            // reading rest of the data
            for (int i = 1; i < lines.Count(); i++)
            {
                DataRow dr = replaydt.NewRow();
                string[] values = lines[i].Split(new char[] { ';' });

                for (int j = 0; j < values.Count() && j < replayColumns.Count(); j++)
                {
                    dr[j] = values[j];
                }
                
                replaydt.Rows.Add(dr);
            }

            samplingVariable = Convert.ToInt32(1 / ((Properties.Settings.Default.samplesPerChannelNumeric * 100) * (Convert.ToDouble(replaydt.Rows[1][3]))));
            samplingTime = (int)(Convert.ToDouble(replaydt.Rows[1][3]) * 1000)*samplingVariable;

            current_replay_rowNum = replaydt.Rows.Count;
            chart1.ChartAreas[0].AxisX.Maximum = Convert.ToDouble(replaydt.Rows[replaydt.Rows.Count - 1][3]) / Convert.ToDouble(replaydt.Rows[1][3]);
            chart1.Series[0].Points.Clear();
            replayRow = 1;

            chart1.DataSource = replaydt;
            chart1.Series[0].XValueMember = "Time";
            chart1.Series[0].YValueMembers = "Plsm";
            chart1.DataBind();

            /*
            stopReplay = true;
            ThreadStart addDataThreadStart = new ThreadStart(AddDataThreadLoop);
            addDataRunner = new Thread(addDataThreadStart);

            addDataDel += new AddDataDelegate(AddData);
            addDataRunner.Priority = ThreadPriority.Highest;
            addDataRunner.Start();
            */
        }

        private void AddDataThreadLoop()
        {
            while (replayRow < current_replay_rowNum)
            {
                chart1.Invoke(addDataDel);
                if (stopReplay == false)
                    break;
            }
            stopReplay = false;
        }

        public void AddData()
        {
            AddNewPoint(chart1.Series[0]);
        }

        public void AddNewPoint(Series ptSeries)
        {
            Thread.Sleep(samplingTime);
            int i = 0;
            while(i < samplingVariable && replayRow < current_replay_rowNum)
            { 
                ptSeries.Points.AddXY(replayRow, replaydt.Rows[replayRow][4]);
                replayRow++;
                i++;
            }
            chart1.Invalidate();
        }

        public bool OptimizeOfLocalFormsOnly(System.Windows.Forms.Control chartControlForm)
        {
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                SetUpDoubleBuffer(chartControlForm);
                return true;
            }
            return false;
        }

        public static void SetUpDoubleBuffer(System.Windows.Forms.Control chartControlForm)
        {
            System.Reflection.PropertyInfo formProp =
            typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            formProp.SetValue(chartControlForm, true, null);
        }

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            if(stopReplay == true)
            {
                stopReplay = false;
            }
            int tmpNextReplayPathIndex = pathForReplay.FindIndex(delegate (string s) { return s == Properties.Settings.Default.replayFilePath; });
            if(pathForReplay[tmpNextReplayPathIndex] == pathForReplay.Last())
                Properties.Settings.Default.replayFilePath = pathForReplay[0];
            else
                Properties.Settings.Default.replayFilePath = pathForReplay[tmpNextReplayPathIndex + 1];
            replay(Properties.Settings.Default.replayFilePath);
        }

        public void setPathForReplay()
        {
            string rootPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + "SF LWM\\";
            DirectoryInfo di = new DirectoryInfo(rootPath);
            foreach (DirectoryInfo sub_d in di.GetDirectories())
            {
                foreach (DirectoryInfo sub_d2 in sub_d.GetDirectories())
                {
                    foreach (FileInfo fi in sub_d2.GetFiles())
                    {
                        pathForReplay.Add(fi.FullName);
                    }
                }
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            if (stopReplay == true)
            {
                stopReplay = false;
            }
            int tmpNextReplayPathIndex = pathForReplay.FindIndex(delegate (string s) { return s == Properties.Settings.Default.replayFilePath; });
            if (pathForReplay[tmpNextReplayPathIndex] == pathForReplay[0])
                Properties.Settings.Default.replayFilePath = pathForReplay.Last();
            else
                Properties.Settings.Default.replayFilePath = pathForReplay[tmpNextReplayPathIndex - 1];
            replay(Properties.Settings.Default.replayFilePath);
        }
        
    }
}
