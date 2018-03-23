using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace time_man
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NotifyIcon notifyIcon;
        String filename;
        ObservableCollection<ScheduleItem> items;
        SQLiteConnection db;

        public MainWindow()
        {
            InitializeComponent();

            string sql;
            SQLiteCommand command;

            if (!File.Exists("db.sqlite"))
            {
                initDatabase();
            }
            else
            {
                db = new SQLiteConnection("Data Source=db.sqlite;Version=3;");
                db.Open();
            }

            listView.Items.Clear();
            items = new ObservableCollection<ScheduleItem>();
            listView.ItemsSource = items;

            loadDatabase();

            filename = "Data/data.txt";
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = new System.Drawing.Icon("Resources/dolphin.ico");
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        private void initDatabase()
        {
            SQLiteConnection.CreateFile("db.sqlite");
            db = new SQLiteConnection("Data Source=db.sqlite;Version=3;");
            db.Open();
            String sql = "CREATE TABLE schedule (id integer primary key, active INT, time INT, label VARCHAR(20))";
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
        }

        private void addItem(ScheduleItem scheduleItem)
        {
            String sql = String.Format("INSERT INTO schedule (label, time, active) values ('{0}', '{1}', '{2}')", scheduleItem.Label, scheduleItem.Time, scheduleItem.Active ? 1 : 0);
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
            scheduleItem.Id = db.LastInsertRowId;
        }

        private void deleteItem(long id)
        {
            String sql = String.Format("DELETE FROM schedule WHERE id = {0}", id);
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
        }

        private void loadDatabase()
        {
            String sql = "SELECT * FROM schedule ORDER BY time ASC";
            SQLiteCommand command = new SQLiteCommand(sql, db);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(String.Format("{0} {1} {2}", reader["active"], reader["time"], reader["label"]));
                String label = reader["label"].ToString();
                int time = int.Parse(reader["time"].ToString());
                bool active = reader["active"].Equals("1") ? true : false;
                ScheduleItem scheduleItem = new ScheduleItem() { Label = label, Time = time, Active = true };

                items.Add(scheduleItem);
            }
        }

        private void readDatabase()
        {
            String sql = "SELECT * FROM schedule ORDER BY time ASC";
            SQLiteCommand command = new SQLiteCommand(sql, db);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(String.Format("{0} {1} {2} {3}", reader["id"], reader["active"], reader["time"], reader["label"]));
            }
        }

        private void showNotification(object sender, RoutedEventArgs e)
        {
            notifyIcon.ShowBalloonTip(1, "Hello World", "Description message", ToolTipIcon.Info);
            Console.WriteLine("Show a notification");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }

        private void buttonAddItem(object sender, RoutedEventArgs e)
        {
            String label = labelTextBox.Text.ToString();
            int time;
            if (int.TryParse(timeTextBox.Text, out time))
            {
                time = int.Parse(timeTextBox.Text);
            }
            else
            {
                time = 0;
            }
            ScheduleItem scheduleItem = new ScheduleItem() { Label = label, Time = time, Active = true };
            addItem(scheduleItem);
            items.Add(scheduleItem);
            Console.Write("add:");
            Console.WriteLine(scheduleItem.Id);
        }

        private void buttonReadSchedule(object sender, RoutedEventArgs e)
        {
            readDatabase();
        }

        private void deleteSelected(object sender, RoutedEventArgs e)
        {
            int index = listView.SelectedIndex;
            ScheduleItem scheduleItem = items.ElementAt(index);
            deleteItem(scheduleItem.Id);
            items.RemoveAt(index);
            Console.Write("delete:");
            Console.WriteLine(scheduleItem.Id);
        }
    }


    public class ScheduleItem
    {
        public long Id { get; set; }
        public string Label { get; set; }
        public int Time { get; set; }
        public bool Active{ get; set; }
    }
}
