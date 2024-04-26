using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace DataScraping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private event EventHandler<bool> ButtonDisabledChanged;
        private event EventHandler SearchClicked;

        private List<SearchTableModel> searchTables = new List<SearchTableModel>();
        private bool _isButtonSearchClicked = false;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            webView?.Dispose();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log("Loading page!!");
            webView.Source = new Uri("https://termene.ro/profil#/");
            webView.WebMessageReceived += webView_WebMessageReceived;
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
            SearchClicked += SearchClickedEvent;
        }

        private void SearchClickedEvent(object sender, EventArgs e)
        {


        }

        private void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (e.Request.Uri == "https://termene.ro/resources/processes/Pasp.prc.php?type=search" && _isButtonSearchClicked)
            {
                _isButtonSearchClicked = false;
                var result = e.Response.GetContentAsync().GetAwaiter();
                result.OnCompleted(() =>
                {
                    try
                    {
                        var res = result.GetResult();
                        StreamReader reader = new StreamReader(res);
                        string jsonString = reader.ReadToEnd();
                        if (string.IsNullOrEmpty(jsonString)) return;

                        var jsonObject = JsonDocument.Parse(jsonString);
                        var dataArray = jsonObject.RootElement.GetProperty("data").GetProperty("data").EnumerateArray();
                        searchTables.Clear();
                        Log("===========Response Json Data===========");
                        foreach (var item in dataArray)
                        {
                            var searchTableModel = new SearchTableModel
                            {
                                Cui = item.GetProperty("firma.cui").GetInt32(),
                                Name = item.TryGetProperty("firma.nume", out var name) ? name.GetString() : null,
                                Url = item.GetProperty("url").GetString()
                            };
                            Log(searchTableModel.Url);
                            searchTables.Add(searchTableModel);
                        }
                        ProcessCrawlData();
                    }
                    catch { }
                });
            }
        }

        private void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e) { }

        private async void webView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
        }

        private void webView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            // Search button clicked
            if (message == "buttonClicked")
            {
                Log("Search Clicked!!");
                _isButtonSearchClicked = true;
                SearchClicked?.Invoke(this, EventArgs.Empty);
            }
            else if (message == "acceptCookie")
            {
                webView.Source = new Uri("https://termene.ro/autentificare");
            }
        }

        private async void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            string currentUrl = await webView.ExecuteScriptAsync("window.location.href");
            currentUrl = currentUrl.Trim('"');
            string pattern = @"^https:\/\/termene\.ro\/firma\/\d+-[A-Z0-9-]+$";

            Regex regex = new Regex(pattern);
            if (currentUrl == "https://termene.ro/#/")
            {
                await webView.CoreWebView2.ExecuteScriptAsync(@"
                    var button = document.querySelector('#c-right > a');
                    if (button) {
                        button.click();
                    } 
                    window.chrome.webview.postMessage('acceptCookie');
                ");
                Log("Accept Cookie");
                return;
            }
            else if (currentUrl == "https://termene.ro/autentificare")
            {
                await Task.Delay(500);

                await webView.CoreWebView2.ExecuteScriptAsync(@"
                    var element = document.querySelector('#emailOrPhone');
                    if (element) {
                        var topPos = element.getBoundingClientRect().top + window.scrollY - 200;
                        window.scrollTo({ top: topPos, behavior: 'smooth' });
                    }
                ");

                await webView.ExecuteScriptAsync("document.querySelector('#emailOrPhone').value = 'office@it-setup.ro'");
                await webView.ExecuteScriptAsync("document.querySelector('#emailOrPhone').dispatchEvent(new Event('input', { bubbles: true }))");
                await webView.ExecuteScriptAsync("document.querySelector('#password').value = 'Dukygeorge123'");
                await webView.ExecuteScriptAsync("document.querySelector('#password').dispatchEvent(new Event('input', { bubbles: true }))");
                await Task.Delay(300);
                await webView.ExecuteScriptAsync("document.querySelector('#loginBtn').click()");
                Log("Login!!");
            }
            else if (currentUrl == "https://termene.ro/profil#/" || currentUrl == "https://termene.ro/profil/" || currentUrl.Contains("/profil"))
            {
                await Task.Delay(500);
                webView.Source = new Uri("https://termene.ro/ai360/pasp#/");
                Log("Nav to pasp");
            }
            else if (regex.IsMatch(currentUrl))
            {
                Log($"=>>> nav to {currentUrl}");
                Log("Processing crawl data");
                Log("================= END =================");
            }
            else
            {
                await webView.ExecuteScriptAsync(@"document.evaluate('//*[@id=""app""]/main/div/div[2]/div[1]/div[2]', document, null, 0, null).iterateNext().click()");
                await Task.Delay(5000);
                await webView.ExecuteScriptAsync(@"
                document.evaluate(""/html/body/div[2]/main/div/div[1]/div/div[3]/div[2]/button[2]"", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.addEventListener('click', function() {
                    window.chrome.webview.postMessage('buttonClicked');
                });");

                Log("click Prospectare piață");
                Log("=>>> Please click on the search button");
            }
        }

        private async Task ProcessCrawlData()
        {

            foreach (var item in searchTables)
            {
                if (string.IsNullOrEmpty(item.Name)) continue;
                webView.Source = new Uri(item.Url);
                break;
            }
        }

        private void Log(string logInfo)
        {
            LogTextBox.Text += logInfo + Environment.NewLine;
        }
    }
}
