using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public MainWindow()
        {
            InitializeComponent();

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

            if (File.Exists(filename))
            {
                String data = File.ReadAllText(filename);
                display.Text = data;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            String contents = textBox.Text;
            File.WriteAllText(filename, contents);
        }
    }
}
