using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using OmronPLCTemperatureReader.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using System.IO;
using System.Windows;
using System.Threading;
using OmronPLCTemperatureReader.Properties;

namespace OmronPLCTemperatureReader.Services
{
    public class TerminalService
    {
        public TerminalService(IPAddress ip, ushort port, string login, string password)
        {
            BaseAddress = new Uri($"http://{ip}:{port}");
            CredentialsMethod = "Basic";
            CredentialsBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login}:{password}"));
            BaseAddress = new Uri($"http://{ip}:{port}");
        }
        
        public Uri BaseAddress { get; set; }
        public string CredentialsMethod { get; set; }
        public string CredentialsBase64 { get; set; }

        private TerminalFile terminalFileRoot { get; set; }

        private CancellationTokenSource cancellationToken { get; set; }

        
        public void Cancel()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
            }
        }


        public async Task<TerminalFile> GetTerminalFilesTreeAsync()
        {
            terminalFileRoot = new TerminalFile("/disk/usb1", BaseAddress, "/disk/usb1", true);
            cancellationToken = new CancellationTokenSource();
            var result = await this.FillTerminalFilesTreeAsync(terminalFileRoot);
            if (result && !cancellationToken.IsCancellationRequested)
            {
                return terminalFileRoot;
            }
            else
            {
                return null;
            }
        }

        public async Task<string> GetFileContentAsync(Uri path)
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add(HttpRequestHeader.Authorization, $"{ this.CredentialsMethod} {this.CredentialsBase64}");
                return await webClient.DownloadStringTaskAsync(path);
            }
        }

        private async Task<bool> FillTerminalFilesTreeAsync(TerminalFile terminalFileParent)
        {
            if (terminalFileParent.IsDirectory)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    AuthenticationHeaderValue authenticationHeaderValue = new AuthenticationHeaderValue(this.CredentialsMethod, this.CredentialsBase64);
                    httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
                    httpClient.Timeout = new TimeSpan(0, 0, Settings.Default.TerminalConnectionTimeoutSeconds);
                    HttpResponseMessage result = null;

                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            result = await httpClient.GetAsync(terminalFileParent.FullPath, HttpCompletionOption.ResponseContentRead, this.cancellationToken.Token);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (HttpRequestException exception)
                    {

                        MessageBox.Show($"Nie udało się pobrać listy plików. Wyjątek: \n{GetAllMessages(exception)}", "Terminal", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    catch (TaskCanceledException exception)
                    {
                        if (!exception.CancellationToken.IsCancellationRequested)
                        {
                            MessageBox.Show($"Upłynął limit czasu żądania. Wyjątek: \n{GetAllMessages(exception)}", "Terminal", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return false;
                    }

                    if (result != null && result.IsSuccessStatusCode)
                    {
                        string responseBody = await result.Content.ReadAsStringAsync();
                        List<TerminalFile> terminalFiles = this.ParseHtmlForUsbFiles(responseBody);

                        terminalFileParent.Children = terminalFiles;

                        foreach (TerminalFile terminalFile in terminalFiles)
                        {
                            await FillTerminalFilesTreeAsync(terminalFile);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Nie udało się pobrać listy plików. Http status: {result.StatusCode} {result.ReasonPhrase}", "Terminal", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            return true;
        }

        private List<TerminalFile> ParseHtmlForUsbFiles(string html)
        {
            List<TerminalFile> terminalFiles = new List<TerminalFile>();

            var document = new HtmlDocument();
            document.LoadHtml(html);
            var table = document.DocumentNode.SelectSingleNode(".//table");
            var rows = table.SelectNodes(".//tr");

            for (int i = 2; i < rows.Count; i++)
            {
                var row = rows[i];
                var columns = row.SelectNodes(".//td");

                string relativePath = columns[0].SelectSingleNode(".//a").Attributes["href"].Value;
                string name = Path.GetFileName(columns[0].SelectSingleNode(".//a").InnerText);
                bool isDirectory = columns[1].InnerText.Contains("dir");

                terminalFiles.Add(new TerminalFile(name, this.BaseAddress, relativePath, isDirectory));
            }

            return terminalFiles;
        }

        private string GetAllMessages(Exception exception)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(exception.Message);

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                sb.AppendLine(exception.Message);
            }

            return sb.ToString();
        }

        
    }
}
