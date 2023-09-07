using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Hourglass.Managers;

using Microsoft.Web.WebView2.Core;

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
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            await webView.EnsureCoreWebView2Async();
            webView.WebMessageReceived += WebView_WebMessageReceived;

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("www.webview", "wwwroot",
                CoreWebView2HostResourceAccessKind.Deny);
            webView.CoreWebView2.Navigate("https://www.webview/index.html");
        }

        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var jsonOptions = new JsonSerializerOptions() {
                Converters = {
                    new JsonStringEnumConverter(),
                }
            };

            DateTime date = DateTime.TryParseExact(
                e.TryGetWebMessageAsString(), "yyyy'-'MM'-'dd", null, default, out date)
                ? date : DateTime.Today;
            var dayData = (await TimerLogManager.Instance.GetRawTimerLogForDay(date)).ToList();
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(dayData, jsonOptions));
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
