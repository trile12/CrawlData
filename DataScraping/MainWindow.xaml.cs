using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace DataScraping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public event EventHandler<bool> ButtonDisabledChanged;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            wb?.Dispose();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            wb.Source = new Uri("https://termene.ro/profil#/");
            wb.WebMessageReceived += Wb_WebMessageReceived;
            wb.NavigationCompleted += Wb_NavigationCompleted;
            wb.CoreWebView2InitializationCompleted += Wb_CoreWebView2InitializationCompleted;
        }

        private async void Wb_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            //await wb.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            //     @"window.external = {
            //        notify: function(message) {
            //            window.chrome.webview.postMessage(message);
            //        }
            //    };");
        }

        private void Wb_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            // Check if the message indicates the button is clicked
            if (message == "buttonClicked")
            {
                // Raise an event or perform any action in response to the button click
                //ButtonClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void Wb_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            await Task.Delay(500);
            string currentUrl = await wb.ExecuteScriptAsync("window.location.href");
            if (currentUrl == "\"https://termene.ro/#/\"")
            {
                wb.Source = new Uri("https://termene.ro/autentificare");
                return;
            }
            else if (currentUrl == "\"https://termene.ro/autentificare\"")
            {
                var check = await wb.ExecuteScriptAsync("document.querySelector('#loginBtn')");
                await wb.ExecuteScriptAsync("document.querySelector('#emailOrPhone').value = '1office@it-setup.ro'");
                await wb.ExecuteScriptAsync("document.querySelector('#emailOrPhone').dispatchEvent(new Event('input', { bubbles: true }))");
                await wb.ExecuteScriptAsync("document.querySelector('#password').value = '1Ts3tup'");
                await wb.ExecuteScriptAsync("document.querySelector('#password').dispatchEvent(new Event('input', { bubbles: true }))");
                await Task.Delay(300);

                check = await wb.ExecuteScriptAsync("document.querySelector('#loginBtn')");
                await wb.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
        document.querySelector('#loginBtn').addEventListener('click', function() {
            // Send a message to C# indicating that the button is clicked
            window.chrome.webview.postMessage('loginButtonClicked');
        });
    ");


                //await wb.ExecuteScriptAsync("document.querySelector('#loginBtn').click()");
            }
            else if (currentUrl == "\"https://termene.ro/profil#/\"")
            {
                wb.Source = new Uri("https://termene.ro/ai360/pasp#/");
            }
            else
            {
                await wb.ExecuteScriptAsync(@"document.evaluate('//*[@id=""app""]/main/div/div[2]/div[1]/div[2]', document, null, 0, null).iterateNext().click()");

                await Task.Delay(5000);
                await wb.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
        document.evaluate(""/html/body/div[2]/main/div/div[1]/div/div[3]/div[2]/button[2]"", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.addEventListener('click', function() {
            // Send a message to C# indicating that the button is clicked
            window.chrome.webview.postMessage('buttonClicked');
        });
    ");
                wb.NavigationCompleted -= Wb_NavigationCompleted;
            }
        }
    }
    public class NetworkInterceptor
    {
        public void ClientInterceptRequests()
        {
            // Interception logic to capture network requests and their responses
            // After getting the response, call ProcessNetworkResponse method with the response text
        }

        // Method called from JavaScript to send the parsed response data to C#
        public void ProcessNetworkResponse(string responseData)
        {
            // Process the responseData in C# code
            Console.WriteLine("Received response data from JavaScript: " + responseData);
        }
    }
}
