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
using System.Windows.Shapes;

namespace Hourglass.Windows
{
    /// <summary>
    /// Interaction logic for TimelineWindow.xaml
    /// </summary>
    public partial class TimelineWindow : Window
    {
        private static TimelineWindow instance;

        public TimelineWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows or activates the <see cref="TimelineWindow"/>. Call this method instead of the constructor to prevent
        /// multiple instances of the dialog.
        /// </summary>
        public static void ShowOrActivate()
        {
            if (TimelineWindow.instance == null)
            {
                TimelineWindow.instance = new TimelineWindow();
                TimelineWindow.instance.Show();
            }
            else
            {
                TimelineWindow.instance.Activate();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TimelineWindow.instance = null;
        }
    }
}
