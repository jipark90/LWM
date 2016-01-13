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
using System.Windows.Shapes;
using NationalInstruments.DAQmx;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using System.Reflection;
using MColor = System.Windows.Media.Color;
using DColor = System.Drawing.Color;

namespace WpfApplication6
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        DataTable dt = new DataTable();
        private MainWindow mainForm; 
        private static readonly int COLUMS = 5;

        public Window1()
        {
            InitializeComponent();

            this.numericTextBoxDouble5.Value = Properties.Settings.Default.maxVoltage;
            this.numericTextBoxDouble6.Value = Properties.Settings.Default.minVoltage;
            this.numericTextBoxDouble1.Value = Properties.Settings.Default.samplingSpeed;
            this.numericTextBoxDouble2.Value = Properties.Settings.Default.samplesPerChannelNumeric;
            this.numericTextBoxDouble3.Value = Properties.Settings.Default.rateNumeric;
            this.textBox5.Text = Properties.Settings.Default.txtFilePath;
            this.textBox7.Text = Properties.Settings.Default.databaseID;
            this.passwordBox1.Password = Properties.Settings.Default.databasePwd;
            this.textBox8.Text = Properties.Settings.Default.projectName;
            this.textBox10.Text = Convert.ToString(Properties.Settings.Default.jobNum);
            checkListBox1_Loaded();
        }

        public Window1(MainWindow mwf)
        {
            InitializeComponent();
            this.mainForm = mwf;
            comboColors.ItemsSource = typeof(Colors).GetProperties();
            comboColors2.ItemsSource = typeof(Colors).GetProperties();
            comboColorsGraph1.ItemsSource = typeof(Colors).GetProperties();
            comboColorsGraph2.ItemsSource = typeof(Colors).GetProperties();

            this.numericTextBoxDouble5.Value = Properties.Settings.Default.maxVoltage;
            this.numericTextBoxDouble6.Value = Properties.Settings.Default.minVoltage;
            this.numericTextBoxDouble1.Value = Properties.Settings.Default.samplingSpeed;
            this.numericTextBoxDouble2.Value = Properties.Settings.Default.samplesPerChannelNumeric;
            this.numericTextBoxDouble3.Value = Properties.Settings.Default.rateNumeric;
            this.numericTextBoxDouble4.Value = Properties.Settings.Default.jobDuration;
            this.textBox5.Text = Properties.Settings.Default.txtFilePath;
            this.textBox7.Text = Properties.Settings.Default.databaseID;
            this.passwordBox1.Password = Properties.Settings.Default.databasePwd;
            this.textBox8.Text = Properties.Settings.Default.projectName;
            this.textBox10.Text = Convert.ToString(Properties.Settings.Default.jobNum);
            checkListBox1_Loaded();

        }

        private void checkListBox1_Loaded()
        {
            this.checkBox1.Content = Properties.Settings.Default.sensor1;
            this.checkBox2.Content = Properties.Settings.Default.sensor2;
            this.checkBox3.Content = Properties.Settings.Default.sensor3;
            this.checkBox4.Content = Properties.Settings.Default.sensor4;
            checkListBox1_LoadedSelectedItems();
        }

        private void checkListBox1_LoadedSelectedItems()
        {
            int i = 0;
            string tmpIndecies = Convert.ToString(Properties.Settings.Default.physicalChannelCheckedIndex.Clone());
            Properties.Settings.Default.physicalChannelCheckedIndex = "";
            while (i < tmpIndecies.Length)
            {
                if (Convert.ToInt32(tmpIndecies.Substring(i, 1)) == 0)
                    this.checkBox1.IsChecked = true;
                else if (Convert.ToInt32(tmpIndecies.Substring(i, 1)) == 1)
                    this.checkBox2.IsChecked = true;
                else if (Convert.ToInt32(tmpIndecies.Substring(i, 1)) == 2)
                    this.checkBox3.IsChecked = true;
                else if (Convert.ToInt32(tmpIndecies.Substring(i, 1)) == 3)
                    this.checkBox4.IsChecked = true;
                i++;
            }
            if (checkBox1.IsChecked == true)
                Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "0";
            if (checkBox2.IsChecked == true)
                Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "1";
            if (checkBox3.IsChecked == true)
                Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "2";
            if (checkBox4.IsChecked == true)
                Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "3";
        }

        private void checkListBox1_saveToProperty()
        {
            try
            {
                string[] physicalChannelNames = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External);
                int deviceChannelTextLength = physicalChannelNames[0].Length;
                int deviceNameIndex = physicalChannelNames[0].LastIndexOf("/");
                string physicalChannelName = physicalChannelNames[0].Substring(0, deviceNameIndex + 1);

                int i = 0;
                Properties.Settings.Default.physicalChannel = "";
                Properties.Settings.Default.physicalChannelCheckedIndex = "";
                if (checkBox1.IsChecked == true)
                    Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "0";
                if (checkBox2.IsChecked == true)
                    Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "1";
                if (checkBox3.IsChecked == true)
                    Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "2";
                if (checkBox4.IsChecked == true)
                    Properties.Settings.Default.physicalChannelCheckedIndex = Properties.Settings.Default.physicalChannelCheckedIndex + "3";
                int checkedItemsCount = Properties.Settings.Default.physicalChannelCheckedIndex.Length;
                if (checkedItemsCount > 0)
                {
                    i = 0;
                    while (i < checkedItemsCount - 1)
                    {
                        if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "0")
                            Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai0,";
                        if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "1")
                            Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai1,";
                        if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "2")
                            Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai2,";
                        if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "3")
                            Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai3,";
                        i++;
                    }
                    if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "0")
                        Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai0";
                    if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "1")
                        Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai1";
                    if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "2")
                        Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai2";
                    if (Properties.Settings.Default.physicalChannelCheckedIndex.Substring(i, 1) == "3")
                        Properties.Settings.Default.physicalChannel = Properties.Settings.Default.physicalChannel + physicalChannelName + "ai3";
                }
                this.numericTextBoxDouble2.Value = Properties.Settings.Default.physicalChannelCheckedIndex.Length;
                this.numericTextBoxDouble3.Value = this.numericTextBoxDouble2.Value * this.numericTextBoxDouble1.Value;
                Properties.Settings.Default.samplesPerChannelNumeric = this.numericTextBoxDouble2.Value;
                Properties.Settings.Default.rateNumeric = this.numericTextBoxDouble3.Value;
            }
            catch (IndexOutOfRangeException e2)
            {
                MessageBox.Show("장비를 연결해 주세요.");
            }
        }

        private void numericTextBoxDouble1_ValueChanged(object sender, NationalInstruments.Controls.ValueChangedEventArgs<double> e)
        {
            this.numericTextBoxDouble3.Value = this.numericTextBoxDouble1.Value * this.numericTextBoxDouble2.Value;
        }

        private void numericTextBoxDouble2_ValueChanged(object sender, NationalInstruments.Controls.ValueChangedEventArgs<double> e)
        {
            this.numericTextBoxDouble3.Value = this.numericTextBoxDouble1.Value * this.numericTextBoxDouble2.Value;
        }

        private void numericTextBoxDouble3_ValueChanged(object sender, NationalInstruments.Controls.ValueChangedEventArgs<double> e)
        {
            //this.numericTextBoxDouble1.Value = this.numericTextBoxDouble3.Value / this.numericTextBoxDouble2.Value;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.maxVoltage = this.numericTextBoxDouble5.Value;
            Properties.Settings.Default.minVoltage = this.numericTextBoxDouble6.Value;
            Properties.Settings.Default.samplingSpeed = this.numericTextBoxDouble1.Value;
            Properties.Settings.Default.txtFilePath = this.textBox5.Text;
            Properties.Settings.Default.databaseID = this.textBox7.Text;
            Properties.Settings.Default.databasePwd = this.passwordBox1.Password;
            Properties.Settings.Default.projectName = this.textBox8.Text;
            Properties.Settings.Default.jobNum = Convert.ToDouble(this.textBox10.Text);
            Properties.Settings.Default.jobDuration = this.numericTextBoxDouble4.Value;
            checkListBox1_saveToProperty();
            Properties.Settings.Default.Save();

            if (Properties.Settings.Default.physicalChannelCheckedIndex != "")
            {
                this.Close();
                int numofGraphs = Properties.Settings.Default.physicalChannelCheckedIndex.Length;

                switch (numofGraphs)
                {
                    case 0:
                        break;
                    case 1:
                        mainForm.graphGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                        mainForm.graphGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
                        break;
                    case 2:
                        mainForm.graphGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                        mainForm.graphGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                        break;
                }
            }
            else
                MessageBox.Show("센서는 1개 이상 선택해 주세요.");

        }


        private void table1_Load()
        {
            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
            MySqlConnection con = new MySqlConnection(conStr);

            try
            {
                con.Open();
                MySqlDataAdapter mda = new MySqlDataAdapter("SELECT idProject,Project_name,Part_name,Production_date,Num_stitch FROM `" + Properties.Settings.Default.databaseName + "`.`project`" , con);

                DataSet ds = new DataSet();

                mda.Fill(ds, "LoadDataBinding3");
                dataGrid1.DataContext = ds;
                con.Close();
            }
            catch (MySqlException e)
            {
                MessageBox.Show("DB를 불러오는데 실패했습니다.");
            }
        }


        private void table2_Load(int selectedIndex)
        {
            string conStr = "Server=localhost;Database=" + Properties.Settings.Default.databaseName + ";Uid=" + Properties.Settings.Default.databaseID + ";Pwd=" + Properties.Settings.Default.databasePwd;
            MySqlConnection con = new MySqlConnection(conStr);

            try
            {
                con.Open();
                MySqlDataAdapter mda = new MySqlDataAdapter("SELECT Reference_id,Training_name FROM `" + Properties.Settings.Default.databaseName + "`.`offline_training` WHERE idProject=" + selectedIndex, con);

                DataSet ds = new DataSet();

                mda.Fill(ds, "LoadDataBinding4");
                dataGrid2.DataContext = ds;
                con.Close();
            }
            catch (MySqlException e)
            {
                MessageBox.Show("DB를 불러오는데 실패했습니다.");
            }
        }

        private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DataRowView tmpRow = (DataRowView)dataGrid1.SelectedItem;
                table2_Load((int)tmpRow.Row[0]);
                Properties.Settings.Default.projectName = (string)tmpRow.Row[1];
                mainForm.label_projectName.Content = Properties.Settings.Default.projectName;
                Properties.Settings.Default.partName = (string)tmpRow.Row[2];
                mainForm.partName_label.Content = "파트 명";
                mainForm.label_partName.Content = Properties.Settings.Default.partName;
                Properties.Settings.Default.projectNum = (int)tmpRow.Row[0];
                Properties.Settings.Default.Save();
            }
            catch (InvalidCastException ey)
            {

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            table1_Load();
            load_folders();
        }

        private void dataGrid2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DataRowView tmpRow = (DataRowView)dataGrid2.SelectedItem;
                try
                {
                    Properties.Settings.Default.reference_id = (int)tmpRow.Row[0];
                    mainForm.drawRefGraphBtn_Click();
                    Properties.Settings.Default.Save();
                }
                catch(NullReferenceException en)
                {

                }
            }
            catch (InvalidCastException ey)
            {

            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (numericTextBoxDouble2.IsEnabled == false)
            {
                numericTextBoxDouble2.IsEnabled = true;
                numericTextBoxDouble3.IsEnabled = true;
            }
            else
            {
                numericTextBoxDouble2.IsEnabled = false;
                numericTextBoxDouble3.IsEnabled = false;
            }
        }

        private void load_folders()
        {
            dt.Columns.Add("num");
            dt.Columns.Add("projectName");
            dt.Columns.Add("date");
            dt.Columns.Add("jobNum");

            int firstid = 1;
            string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + "SF LWM\\";
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (DirectoryInfo sub_d in di.GetDirectories())
            {
                foreach (DirectoryInfo sub_d2 in sub_d.GetDirectories())
                {
                    foreach (FileInfo fi in sub_d2.GetFiles())
                    {
                        DataRow dr = dt.NewRow();

                        dr[0] = firstid++;
                        dr[1] = sub_d.Name.ToString();
                        dr[2] = fi.LastWriteTime;
                        dr[3] = fi.Name.Substring(fi.Name.LastIndexOf('_')+1, fi.Name.Length - fi.Name.LastIndexOf('_') - 5);

                        dt.Rows.Add(dr);
                    }
                }
            }

            dataGrid3.DataContext = dt.DefaultView;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            mainForm.startBtn.Content = "재 생";
            mainForm.startBtnGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            mainForm.startBtnGrid.RowDefinitions[1].Height = new GridLength(50, GridUnitType.Pixel);
            mainForm.setPathForReplay();
            //mainForm.replay();
            this.Close();
        }

        private void dataGrid3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DataRowView tmpRow = (DataRowView)dataGrid3.SelectedItem;

                string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + "SF LWM\\" + tmpRow.Row[1] + "\\";
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (DirectoryInfo sub_di in di.GetDirectories())
                {
                    foreach(FileInfo fi in sub_di.GetFiles())
                    {
                        if (fi.LastWriteTime.ToString() == (string)tmpRow.Row[2])
                        {
                            Properties.Settings.Default.replayFilePath = fi.FullName.ToString();
                            StreamReader sr = new StreamReader(fi.FullName);
                            sr.ReadLine();
                            sr.ReadLine();
                            string fileThirdLine = sr.ReadLine();
                            int firstSemicolonId = fileThirdLine.IndexOf(';');
                            int secondSemicolonId = fileThirdLine.IndexOf(';', firstSemicolonId + 1);
                            int targetSemicolonId = fileThirdLine.IndexOf(';', secondSemicolonId + 1);
                            Properties.Settings.Default.replayTimeUnit = Convert.ToDouble(fileThirdLine.Substring(secondSemicolonId + 1, targetSemicolonId - secondSemicolonId - 1));
                        }
                    }
                }
            }
            catch (InvalidCastException ey2)
            {

            }
        }

        private void table_Loaded(object sender, RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            
            if (grid != null)
            {
                if (grid.RowDefinitions.Count == 0)
                {
                    for (int r = 0; r <= comboColors.Items.Count / COLUMS; r++)
                    {
                        grid.RowDefinitions.Add(new RowDefinition());
                    }
                }
                if (grid.ColumnDefinitions.Count == 0)
                {
                    for (int c = 0; c < Math.Min(comboColors.Items.Count, COLUMS); c++)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition());
                    }
                }
                for (int i = 0; i < grid.Children.Count; i++)
                {
                    Grid.SetColumn(grid.Children[i], i % COLUMS);
                    Grid.SetRow(grid.Children[i], i / COLUMS);
                }
            }
        }

        private void comboColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LinearGradientBrush test = mainForm.FindResource("backgraundColor") as LinearGradientBrush;
            var selectedItem = (PropertyInfo)comboColors.SelectedItem;
            var color = (Color)selectedItem.GetValue(null,null);
            test.GradientStops[0].Color = color;
        }

        private void comboColors2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LinearGradientBrush test = mainForm.FindResource("backgraundColor") as LinearGradientBrush;
            var selectedItem = (PropertyInfo)comboColors2.SelectedItem;
            var color = (Color)selectedItem.GetValue(null, null);
            test.GradientStops[1].Color = color;
        }

        private void comboColorsGraph1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (PropertyInfo)comboColorsGraph1.SelectedItem;
            var color = (Color)selectedItem.GetValue(null, null);
            mainForm.chart1.BackColor = ToMediaColor(color);
            mainForm.chart2.BackColor = ToMediaColor(color);
        }

        private void comboColorsGraph2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (PropertyInfo)comboColorsGraph2.SelectedItem;
            var color = (Color)selectedItem.GetValue(null, null);
            mainForm.chart1.Series[0].Color = ToMediaColor(color);
            mainForm.chart2.Series[0].Color = ToMediaColor(color);
        }

        private DColor ToMediaColor(MColor color)
        {
            return DColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void default_button1_Click(object sender, RoutedEventArgs e)
        {
            numericTextBoxDouble5.Value = 10;
            numericTextBoxDouble6.Value = -10;
        }

        private void default_button2_Click(object sender, RoutedEventArgs e)
        {
            numericTextBoxDouble1.Value = 100;
            numericTextBoxDouble1.Value = 1;
            numericTextBoxDouble1.Value = 100;
        }

        private void edit_button1_Click(object sender, RoutedEventArgs e)
        {
            textBox8.IsEnabled = true;
            textBox10.IsEnabled = true;
        }
        
    }
}
