using System;
using System.Threading.Tasks;
using System.Windows;

namespace DataScraping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public event EventHandler<bool> ButtonDisabledChanged;
        public event EventHandler SearchClicked;

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
            webView.Source = new Uri("https://termene.ro/profil#/");
            webView.WebMessageReceived += webView_WebMessageReceived;
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
            SearchClicked += SearchClickedEvent;
        }

        private void SearchClickedEvent(object sender, EventArgs e)
        {
        }

        private void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e) { }

        private async void webView_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }

        private void webView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            // Search button clicked
            if (message == "buttonClicked")
            {
                SearchClicked?.Invoke(this, EventArgs.Empty);
            }
            else if (message == "acceptCookie")
            {
                webView.Source = new Uri("https://termene.ro/autentificare");
            }
        }

        private async void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            await Task.Delay(500);
            string currentUrl = await webView.ExecuteScriptAsync("window.location.href");
            if (currentUrl == "\"https://termene.ro/#/\"")
            {
                await webView.CoreWebView2.ExecuteScriptAsync(@"
                    var button = document.querySelector('#c-right > a');
                    if (button) {
                        button.click();
                    } 
                    window.chrome.webview.postMessage('acceptCookie');
                ");
                return;
            }
            else if (currentUrl == "\"https://termene.ro/autentificare\"")
            {
                await webView.CoreWebView2.ExecuteScriptAsync(@"
                    var element = document.querySelector('#emailOrPhone');
                    if (element) {
                        var topPos = element.getBoundingClientRect().top + window.scrollY - 200;
                        window.scrollTo({ top: topPos, behavior: 'smooth' });
                    }
                ");

                await webView.ExecuteScriptAsync("document.querySelector('#emailOrPhone').value = 'office@it-setup.ro'");
                await webView.ExecuteScriptAsync("document.querySelector('#emailOrPhone').dispatchEvent(new Event('input', { bubbles: true }))");
                await webView.ExecuteScriptAsync("document.querySelector('#password').value = '1Ts3tup'");
                await webView.ExecuteScriptAsync("document.querySelector('#password').dispatchEvent(new Event('input', { bubbles: true }))");
                await Task.Delay(300);
                await webView.ExecuteScriptAsync("document.querySelector('#loginBtn').click()");
            }
            else if (currentUrl == "\"https://termene.ro/profil#/\"")
            {
                webView.Source = new Uri("https://termene.ro/ai360/pasp#/");
            }
            else
            {
                await webView.ExecuteScriptAsync(@"document.evaluate('//*[@id=""app""]/main/div/div[2]/div[1]/div[2]', document, null, 0, null).iterateNext().click()");
                await Task.Delay(5000);
                await webView.ExecuteScriptAsync(@"
                document.evaluate(""/html/body/div[2]/main/div/div[1]/div/div[3]/div[2]/button[2]"", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.addEventListener('click', function() {
                    window.chrome.webview.postMessage('buttonClicked');
                });");
                webView.NavigationCompleted -= webView_NavigationCompleted;
            }
        }
    }
}
