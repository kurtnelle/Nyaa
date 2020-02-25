using LogManagement;
using System;
using System.Web;
using Flurl.Http;
using System.IO;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

namespace Nyaa
{
    class Program
    {
        static DateTime startedTime = DateTime.Now;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Log.LogFileName = Log.DefaultLogFileName;
            Log.LogEvent += Log_LogEvent;
            Log.Informational($"Started at {startedTime}");
            if (args.Length == 1)
            {
                DoWork(args[0].Trim());
            }
            else
            {
                Console.Write("Anime Title? ");
                var _title = Console.ReadLine().Trim();
                if (!string.IsNullOrEmpty(_title))
                {
                    DoWork(_title);
                }
            }
        }

        public static void DoWork(string animeName)
        {
            var _page = $"https://nyaa.si/?f=0&c=1_2&q={HttpUtility.UrlEncode(animeName)}".GetStringAsync().Result;

            var doc = new HtmlDocument();
            doc.LoadHtml(_page);
            var _downloadNodes =  doc.DocumentNode.SelectNodes("//a[starts-with(@href,'/download/')]");
            var _folderName = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}Torrents";
            if (!Directory.Exists(_folderName))
            {
                Directory.CreateDirectory(_folderName);
            }
            var webClient = new WebClient();
            Regex _fileNameExpression = new Regex("filename=\"([^\"]+)\"", RegexOptions.Compiled);

            foreach (HtmlNode node in _downloadNodes)
            {
                var data = webClient.DownloadData($"https://nyaa.si{node.GetAttributeValue("href", string.Empty)}");
                string _fileName = "";
                if (!String.IsNullOrEmpty(webClient.ResponseHeaders["Content-Disposition"]))
                {
                    var _contentDisposition = HttpUtility.UrlDecode(webClient.ResponseHeaders["Content-Disposition"]);

                    if (_fileNameExpression.IsMatch(_contentDisposition))
                    {
                        _fileName = _fileNameExpression.Match(_contentDisposition).Groups[1].Value;
                    }
                }
                if (!string.IsNullOrEmpty(_fileName))
                {
                    _fileName = $"{_folderName}{Path.DirectorySeparatorChar}{_fileName}";
                    File.WriteAllBytes(_fileName, data);
                    Log.Informational(_fileName);
                }
            }
            //using var _webDriver = new WebDriver();
            //var _searchBox = _webDriver.Go($"https://nyaa.si/?f=0&c=1_2&q={HttpUtility.UrlEncode(animeName)}")
            //    .WaitForTitle(animeName);
            //Log.Informational($"Searching for {animeName}");
        }

        private static void Log_LogEvent(string message, string threadName, LogManagement.LogLevel level)
        {
            switch (level)
            {
                case LogManagement.LogLevel.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogManagement.LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogManagement.LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    break;
            }
            Console.WriteLine(message);
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Informational($"Terminating with error at {DateTime.Now}, with a duration of {(DateTime.Now - startedTime).TotalMinutes}");
            Log.Exception((Exception)e.ExceptionObject);
            Log.LogFileName = string.Empty;
            Environment.Exit(-1);
        }
    }
}
