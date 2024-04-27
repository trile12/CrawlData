﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DataScraping
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region PROPERTIES

        private readonly string passWord = ConfigurationManager.AppSettings["password"].ToString();
        private readonly string userName = ConfigurationManager.AppSettings["username"].ToString();
        private bool _isButtonSearchClicked = false;

        private DataInfo currentInfo = new DataInfo();

        private List<DataInfo> listInfo = new List<DataInfo>();

        private List<SearchTableModel> searchTables = new List<SearchTableModel>();

        private TaskCompletionSource<bool> taskCompletionGetExten;

        private TaskCompletionSource<bool> taskCompletionGetOwner;

        private TaskCompletionSource<bool> taskCompletionGetPage;

        private TaskCompletionSource<bool> taskCompletionSource;

        private event EventHandler SearchClicked;
        #endregion PROPERTIES

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task<string> ExecuteScriptAndGetContent(string funtionName, string path)
        {
            string script = @" function " + funtionName + @"() {
            return document.querySelector('" + path + "').textContent;}";
            await webView.ExecuteScriptAsync(script);
            string content = await webView.ExecuteScriptAsync(funtionName + "();");

            content = content.Replace("\\n", "").Replace("\n", "").Trim('"').Trim();
            content = Regex.Replace(content, @"\s+", " ").Trim();
            return content;
        }

        private async Task ProcessCrawlData()
        {
            Log($"ProcessCrawlData - ({searchTables.Count})");
            //Check and delete existing entries
            var cuis = searchTables.Select(x => x.Cui).ToList();
            List<string> cuisString = cuis.Select(i => i.ToString()).ToList();
            var existCUIS = await GetExistingCUISAsync(cuisString);

            foreach (var item in searchTables)
            {
                if (string.IsNullOrEmpty(item.Name)) continue;
                if (existCUIS.Any() && existCUIS.Contains(item.Cui.ToString())) continue;
                taskCompletionSource = new TaskCompletionSource<bool>();
                taskCompletionGetOwner = new TaskCompletionSource<bool>();
                taskCompletionGetExten = new TaskCompletionSource<bool>();
                currentInfo = new DataInfo();
                webView.Source = new Uri(item.Url);
                await taskCompletionSource.Task;
            }

            Log($"================= DONE =================)");
        }
        #region EVENTS

        private void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            if (e.Request.Uri == "https://termene.ro/resources/processes/Pasp.prc.php?type=search" && _isButtonSearchClicked)
            {
                _isButtonSearchClicked = false;
                var result = e.Response.GetContentAsync().GetAwaiter();
                result.OnCompleted(async () =>
                {
                    try
                    {
                        var res = result.GetResult();
                        using (StreamReader reader = new StreamReader(res))
                        {
                            string jsonString = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(jsonString)) return;
                            var jsonObject = JsonDocument.Parse(jsonString);
                            var dataArray = jsonObject.RootElement.GetProperty("data").GetProperty("data").EnumerateArray();
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
                        }
                    }
                    catch { }

                    taskCompletionGetPage.SetResult(true);
                });
            }
            else if (e.Request.Uri.Contains("https://termene.ro/resources/requests/pageDetaliiFirma/contactInfoExtins.php?cui="))
            {
                var result = e.Response.GetContentAsync().GetAwaiter();
                result.OnCompleted(() =>
                {
                    try
                    {
                        var res = result.GetResult();
                        using (StreamReader reader = new StreamReader(res))
                        {
                            string jsonString = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(jsonString)) return;
                            var jsonObject = JsonDocument.Parse(jsonString);
                            var phones = jsonObject.RootElement.GetProperty("telefon").EnumerateArray();
                            //var sums = jsonObject.RootElement.GetProperty("sumarizare").EnumerateObject();
                            var emails = jsonObject.RootElement.GetProperty("email").EnumerateArray();
                            var webs = jsonObject.RootElement.GetProperty("adrese_web").EnumerateArray();
                            List<string> results = new List<string>();
                            foreach (JsonElement phone in phones)
                            {
                                results.Add(phone.GetString());
                            }
                            foreach (JsonElement email in emails)
                            {
                                results.Add(email.GetString());
                            }
                            foreach (JsonElement web in webs)
                            {
                                results.Add(web.GetString());
                            }
                            if (results.Any())
                                currentInfo.ExtendedData = string.Join(",", results.ToArray());

                            Log("get ExtendedData");
                            //UpdateRecord(currentInfo);
                        }
                    }
                    catch
                    {
                        Log("get ExtendedData failed");
                    }
                    finally
                    {
                        taskCompletionGetExten.SetResult(true);
                    }
                });
            }
            else if (e.Request.Uri.Contains("https://termene.ro/resources/requests/pageDetaliiFirma/associatesAdministrators.php?cui"))
            {
                var result = e.Response.GetContentAsync().GetAwaiter();
                result.OnCompleted(async () =>
                {
                    try
                    {
                        var res = result.GetResult();
                        using (StreamReader reader = new StreamReader(res))
                        {
                            string jsonString = reader.ReadToEnd();
                            if (string.IsNullOrEmpty(jsonString)) return;
                            var jsonObject = JsonDocument.Parse(jsonString);

                            var dataArray = jsonObject.RootElement.GetProperty("asociatiAdministratori").GetProperty("asociati");

                            List<string> numeList = new List<string>();
                            foreach (JsonElement asociat in dataArray.EnumerateArray())
                            {
                                string nume = asociat.GetProperty("nume").GetString();
                                numeList.Add(nume);
                            }
                            currentInfo.Owners = string.Join(",", numeList);
                            Log("get Owners");
                        }
                    }
                    catch { Log("get Owners failed"); }
                    finally { taskCompletionGetOwner.SetResult(true); }
                });
            }
        }

        private async void SearchClickedEvent(object sender, EventArgs e)
        {
            searchTables.Clear();
            LogTextBox.Text = "";
            taskCompletionGetPage = new TaskCompletionSource<bool>();
            await taskCompletionGetPage.Task;
            string script = @"
                        var paginationNav = document.querySelector('#app > main > div > div.col-10.col-sm-9 > div:nth-child(3) > div.mt-3.text-end > nav');
                        var buttons = paginationNav.querySelectorAll('button');
                        var selectors = [];
                        buttons.forEach(function(button) {
                            var selector = '';
                            selector = button.nodeName.toLowerCase();
                            if (button.id) {
                                selector += '#' + button.id;
                            } else if (button.classList.length > 0) {
                                selector += '.' + button.classList[0];
                            }
                            selectors.push(selector);
                        });
                        selectors.join(',');";
            string querySelectors = await webView.ExecuteScriptAsync(script);

            string[] selectorArray = querySelectors.Split(',');
            for (int i = 1; i < selectorArray.Length; i++)
            {
                taskCompletionGetPage = new TaskCompletionSource<bool>();
                _isButtonSearchClicked = true;
                string scriptButton = @"
                             var button = document.querySelector('#app > main > div > div.col-10.col-sm-9 > div:nth-child(3) > div.mt-3.text-end > nav > ul > li:nth-child(" + $"{i + 1}" + ") > button'); if (button) { button.click(); }";
                await webView.ExecuteScriptAsync(scriptButton);
                await taskCompletionGetPage.Task;
            }

            ProcessCrawlData();
        }

        private async void webView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            webView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
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

                await webView.ExecuteScriptAsync("document.querySelector('#emailOrPhone').value = '" + $"{userName}'");
                await webView.ExecuteScriptAsync("document.querySelector('#emailOrPhone').dispatchEvent(new Event('input', { bubbles: true }))");
                await webView.ExecuteScriptAsync("document.querySelector('#password').value = '" + $"{passWord}'");
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
                do
                {
                    currentInfo.CompanyName = await ExecuteScriptAndGetContent("getCompanyName", "#tailwind h1 div:nth-child(1) div:nth-child(2) span");
                } while (string.IsNullOrEmpty(currentInfo.CompanyName) || currentInfo.CompanyName == "null");

                do
                {
                    currentInfo.CUI = await ExecuteScriptAndGetContent("getCUI", "#tailwind > div.col-12.col-sm-7 > div:nth-child(2) > div:nth-child(2) > div");
                } while (string.IsNullOrEmpty(currentInfo.CUI) || currentInfo.CUI == "null");

                if (CheckCUIIsExist(currentInfo.CUI))
                {
                    Log("Already Exist !!");
                    taskCompletionSource.SetResult(true);
                    return;
                }

                currentInfo.RegistDate = await ExecuteScriptAndGetContent("getRegistDate", "#tailwind > div.col-12.col-sm-7 > div:nth-child(2) > div:nth-child(4) > div");
                currentInfo.MFINANCE = await ExecuteScriptAndGetContent("getMFINANCE", "#tailwind > div.col-12.col-sm-7 > div:nth-child(2) > div:nth-child(6) > div");
                currentInfo.Localitate = await ExecuteScriptAndGetContent("getLocalitate", "#tailwind > div.col-12.col-sm-7 > div:nth-child(4) > div:nth-child(2) > div");
                currentInfo.District = await ExecuteScriptAndGetContent("getDistrict", "#tailwind > div.col-12.col-sm-7 > div:nth-child(4) > div:nth-child(4) > div");
                currentInfo.CodPostal = await ExecuteScriptAndGetContent("getCodPostal", "#tailwind > div.col-12.col-sm-7 > div:nth-child(4) > div:nth-child(6) > div");
                currentInfo.SediuSocial = await ExecuteScriptAndGetContent("getSediuSocial", "#tailwind > div.col-12.col-sm-7 > div:nth-child(4) > div:nth-child(8) > div");
                currentInfo.CompanyStatus = await ExecuteScriptAndGetContent("getCompanyStatus", "#company-profile > div > div > div:nth-child(2) > div.col-8 > div > span");
                currentInfo.SocialCapital = await ExecuteScriptAndGetContent("getSocialCapital", "#company-profile > div > div > div:nth-child(3) > div.col-8 > div");
                currentInfo.Phone = await ExecuteScriptAndGetContent("getPhone", "#go-contact-info > div > div:nth-child(2) > div > div.col-12.col-sm-7 > div:nth-child(1) > div.col-8");
                currentInfo.Email = await ExecuteScriptAndGetContent("getEmail", "#go-contact-info > div > div:nth-child(2) > div > div.col-12.col-sm-7 > div:nth-child(2) > div.col-8");
                currentInfo.Web = await ExecuteScriptAndGetContent("getWeb", "#go-contact-info > div > div:nth-child(2) > div > div.col-12.col-sm-7 > div:nth-child(3) > div.col-8");
                currentInfo.NrOfBranches = await ExecuteScriptAndGetContent("getNrOfBranches", "#tailwind > h2 > span:nth-child(2)");
                currentInfo.Url = currentUrl;

                await taskCompletionGetOwner.Task;
                await taskCompletionGetExten.Task;

                listInfo.Add(currentInfo);
                AddRecord(currentInfo);
                Log("Add Record !!");
                taskCompletionSource.SetResult(true);
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

        private void webView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
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

        private void Window_Closed(object sender, EventArgs e)
        {
            webView?.Dispose();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (var dbContext = new AppDbContext())
            {
                dbContext.Database.EnsureCreated();
            }
            string dbFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DS.db");
            Log("Loading page!!");
            webView.Source = new Uri("https://termene.ro/profil#/");
            //webView.Source = new Uri("https://termene.ro/firma/33787105-ECOREC-ALBA-SRL");
            webView.WebMessageReceived += webView_WebMessageReceived;
            webView.NavigationCompleted += webView_NavigationCompleted;
            webView.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
            SearchClicked += SearchClickedEvent;
        }
        #endregion EVENTS

        #region AppDbContext

        private void AddRecord(DataInfo dataInfo)
        {
            using (var dbContext = new AppDbContext())
            {
                dataInfo.Id = Guid.NewGuid();
                dbContext.DataInfos.Add(dataInfo);
                dbContext.SaveChanges();
            }
        }

        private bool CheckCUIIsExist(string cui)
        {
            using (var dbContext = new AppDbContext())
            {
                return dbContext.DataInfos.AsNoTracking().Any(d => d.CUI == cui);
            }
        }

        private async Task<List<string>> GetExistingCUISAsync(List<string> cuis)
        {
            using (var dbContext = new AppDbContext())
            {
                var existingCUIS = await dbContext.DataInfos
                    .AsNoTracking()
                    .Where(x => cuis.Contains(x.CUI))
                    .Select(x => x.CUI)
                    .ToListAsync();

                return existingCUIS;
            }
        }

        private void UpdateRecord(DataInfo dataInfo)
        {
            using (var dbContext = new AppDbContext())
            {
                var existingRecord = dbContext.DataInfos.FirstOrDefault(d => d.CUI == dataInfo.CUI);
                if (existingRecord != null)
                {
                    existingRecord.ExtendedData = dataInfo.ExtendedData;
                    dbContext.SaveChanges();
                }
            }
        }
        #endregion AppDbContext

        #region LOG

        private void Log(string logInfo)
        {
            LogTextBox.Text += logInfo + Environment.NewLine;
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogTextBox.ScrollToEnd();
        }
        #endregion LOG
    }
}