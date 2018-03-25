using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

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
        DispatcherTimer dispatcherTimer;
        bool waitingForFirst = true;
        bool menuOpen = false;

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
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Open", openWindow);
            contextMenu.MenuItems.Add("Exit", closeApplication);
            notifyIcon.ContextMenu = contextMenu;


            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(notifyTasksForNow);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();

        }

        private void openWindow(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void closeApplication(object sender, EventArgs e)
        {
            notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Notifies for tasks with time matching now
        /// </summary>
        private void notifyTasksForNow(object sender, EventArgs e)
        {
            foreach (var item in items)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                if (getTime(item.Time).Equals(getTime(now)) && item.Active)
                {
                    showNotification(item.Label, "Task for " + getTime(item.Time));
                    if (waitingForFirst)
                    {
                        waitingForFirst = false;

                        dispatcherTimer.Stop();
                        dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
                        dispatcherTimer.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Create SQLite table
        /// </summary>
        private void initDatabase()
        {
            SQLiteConnection.CreateFile("db.sqlite");
            db = new SQLiteConnection("Data Source=db.sqlite;Version=3;");
            db.Open();
            String sql = "CREATE TABLE schedule (id integer primary key, active INT, time VARCHAR(20), label VARCHAR(20))";
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Add schedule item to database
        /// </summary>
        private void addItem(ScheduleItem scheduleItem)
        {
            String timeString = getTime(scheduleItem.Time);
            String sql = String.Format("INSERT INTO schedule (label, time, active) values ('{0}', '{1}', '{2}')", scheduleItem.Label, timeString, scheduleItem.Active ? 1 : 0);
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
            scheduleItem.Id = db.LastInsertRowId;
        }

        /// <summary>
        /// Delete schedule item from database
        /// </summary>
        private void deleteItem(long id)
        {
            String sql = String.Format("DELETE FROM schedule WHERE id = {0}", id);
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Update shcedule item in database
        /// </summary>
        private void updateItem(long id, ScheduleItem scheduleItem)
        {
            Console.WriteLine(id);
            Console.WriteLine(scheduleItem.Active);
            String sql = String.Format("UPDATE schedule SET active = {0} WHERE id = {1}", scheduleItem.Active ? 1 : 0, id);
            Console.WriteLine(sql);
            SQLiteCommand command = new SQLiteCommand(sql, db);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Load schedule items from database
        /// </summary>
        private void loadDatabase()
        {
            String sql = "SELECT * FROM schedule ORDER BY time ASC";
            SQLiteCommand command = new SQLiteCommand(sql, db);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(String.Format("{0} {1} {2}", reader["active"], reader["time"], reader["label"]));
                long id = long.Parse(reader["Id"].ToString());
                String label = reader["label"].ToString();

                // Time
                String[] stringTime = reader["time"].ToString().Split(':');
                int hour = int.Parse(stringTime[0]);
                int minute = int.Parse(stringTime[1]);
                TimeSpan time = new TimeSpan(hour, minute, 0);

                bool active = reader["active"].Equals(1) ? true : false;
                ScheduleItem scheduleItem = new ScheduleItem() { Id = id, Label = label, Time = time, Active = active };

                items.Add(scheduleItem);
            }
        }

        /// <summary>
        /// Refresh the schedule item table
        /// </summary>
        private void reloadView()
        {
            items = new ObservableCollection<ScheduleItem>();
            listView.ItemsSource = items;

            loadDatabase();
        }

        /// <summary>
        /// Print out the database
        /// </summary>
        private void readDatabase()
        {
            String sql = "SELECT * FROM schedule ORDER BY time ASC";
            SQLiteCommand command = new SQLiteCommand(sql, db);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine(String.Format("{0} {1} {2} {3}", reader["id"], reader["active"], reader["time"], reader["label"]));
            }

            foreach (var item in items)
            {
                Console.WriteLine(item.Active);
            }
        }

        /// <summary>
        /// Trigger notification
        /// </summary>
        private void buttonShowNotification(object sender, RoutedEventArgs e)
        {
            showNotification("Test Notification", "Test message");
        }

        /// <summary>
        /// String format of TimeSpan object
        /// </summary>
        private String getTime(TimeSpan timeSpan)
        {
            return string.Format("{0:00}:{1:00}", timeSpan.Hours, timeSpan.Minutes);
        }

        /// <summary>
        /// Display window notification
        /// </summary>
        private void showNotification(String title, String message)
        {
            notifyIcon.ShowBalloonTip(1, title, message, ToolTipIcon.Info);
        }

        /// <summary>
        /// Prevent application termination on close
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }

        /// <summary>
        /// Reset form validation state
        /// </summary>
        private void resetFormValidation()
        {
            hourTextBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFABADB3"));
            minuteTextBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFABADB3"));
            labelTextBox.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFABADB3"));
        }

        /// <summary>
        /// Validate form to add item
        /// </summary>
        private bool validateForm()
        {
            resetFormValidation();
            bool isValid = true;
            int hour;
            int minute;
            if (int.TryParse(hourTextBox.Text, out hour) && int.TryParse(minuteTextBox.Text, out minute))
            {
                hour = int.Parse(hourTextBox.Text);
                minute = int.Parse(minuteTextBox.Text);
                if (hour > 24 || hour < 0 || minute > 59 || minute < 0)
                {
                    hourTextBox.BorderBrush = Brushes.IndianRed;
                    minuteTextBox.BorderBrush = Brushes.IndianRed;
                    isValid = false;
                }
            }
            else
            {
                hourTextBox.BorderBrush = Brushes.IndianRed;
                minuteTextBox.BorderBrush = Brushes.IndianRed;
                isValid = false;

            }
            if (String.IsNullOrEmpty(labelTextBox.Text))
            {
                labelTextBox.BorderBrush = Brushes.IndianRed;
                isValid = false;
            }
            return isValid;
        }

        private void clearForm()
        {
            labelTextBox.Text = "";
            hourTextBox.Text = "12";
            minuteTextBox.Text = "00";
        }

        /// <summary>
        /// Add item button was clicked
        /// </summary>
        private void buttonAddItem(object sender, RoutedEventArgs e)
        {
            if (!validateForm())
            {
                return;
            }
            String label = labelTextBox.Text.ToString();
            int hour = int.Parse(hourTextBox.Text);
            int minute = int.Parse(minuteTextBox.Text);
            TimeSpan timeSpan = new TimeSpan(hour, minute, 0);
            ScheduleItem scheduleItem = new ScheduleItem() { Label = label, Time = timeSpan, Active = true };
            addItem(scheduleItem);
            items.Add(scheduleItem);
            clearForm();
            Console.Write("add:");
            Console.WriteLine(scheduleItem.Id);
        }

        /// <summary>
        /// Button for reading database was clicked
        /// </summary>
        private void buttonReadSchedule(object sender, RoutedEventArgs e)
        {
            readDatabase();
        }

        /// <summary>
        /// Delete button was clicked
        /// </summary>
        private void deleteSelected(object sender, RoutedEventArgs e)
        {
            int index = listView.SelectedIndex;
            if (index < 0)
            {
                return;
            }
            ScheduleItem scheduleItem = items.ElementAt(index);
            deleteItem(scheduleItem.Id);
            items.RemoveAt(index);
            Console.Write("delete:");
            Console.WriteLine(scheduleItem.Id);
        }

        /// <summary>
        /// Update item to toggle active
        /// </summary>
        private void toggleActiveSelected(object sender, RoutedEventArgs e)
        {
            int index = listView.SelectedIndex;
            if (index < 0)
            {
                return;
            }
            items.ElementAt(index).Active = !items.ElementAt(index).Active;
            updateItem(items.ElementAt(index).Id, items.ElementAt(index));
            reloadView();
        }
    }

    /// <summary>
    /// Store schedule item data
    /// </summary>
    public class ScheduleItem
    {
        public long Id { get; set; }
        public string Label { get; set; }
        public TimeSpan Time { get; set; }
        public bool Active { get; set; }
    }

    /// <summary>
    /// Converter for table to display active bool as a tick symbol
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public char Separator { get; set; } = ';';

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strings = ((string)parameter).Split(Separator);
            var trueString = strings[0];
            var falseString = strings[1];

            var boolValue = (bool)value;
            if (boolValue == true)
            {
                return trueString;
            }
            else
            {
                return falseString;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strings = ((string)parameter).Split(Separator);
            var trueString = strings[0];
            var falseString = strings[1];

            var stringValue = (string)value;
            if (stringValue == trueString)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
