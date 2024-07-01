using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

using NodaTime;
using NodaTime.Text;

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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    new JsonStringEnumConverter(),
                }
            };

            var message = JsonSerializer.Deserialize<JsonObject>(e.WebMessageAsJson, jsonOptions);
            string msg = message["msg"].GetValue<string>();

            if (msg == "LoadDay")
            {
                LocalDate date = parseDate(message["date"].GetValue<string>())
                    ?? LocalDate.FromDateTime(DateTime.Today);

                DateTimeZone zone = DateTimeZoneProviders.Bcl.GetSystemDefault();
                ZonedDateTime from = date.AtStartOfDayInZone(zone);
                ZonedDateTime to = date.PlusDays(1).AtStartOfDayInZone(zone)
                    .PlusHours(3); // the UI sometimes shows a few hours of the next day

                var rawTasks = (await TimerLogManager.Instance.GetTimerLogEntries(from, to)).ToList();
                var editedTasks = (await TimerLogManager.Instance.GetTasks(from, to)).ToList();
                var response = new { rawTasks, editedTasks };
                webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(response, jsonOptions));
            }
            else if (msg == "UpsertTask")
            {
                Task task = message["task"].Deserialize<Task>(jsonOptions);
                if (task.IsDeleted is true)
                {
                    await TimerLogManager.Instance.DeleteTask(task.Id);
                }
                else
                {
                    await TimerLogManager.Instance.UpsertTask(task);
                }
            }

            LocalDate? parseDate(string dateStr)
            {
                var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy'-'MM'-'dd");
                var parseResult = pattern.Parse(dateStr);
                return parseResult.Success ? parseResult.Value : (LocalDate?)null;
            }
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
