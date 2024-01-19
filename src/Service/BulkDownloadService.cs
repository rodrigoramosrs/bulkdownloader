using BulkDownloader.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BulkDownloader.Service
{
    internal static class BulkDownloadService
    {


        private static object ThreadLocker = new object();
        private static SemaphoreSlim semaphore = new SemaphoreSlim(GlobalSettings.MaxDegreeOfParallelism); // Inicia com um slot disponível



        static BulkDownloadService()
        {
            if (!Directory.Exists(GlobalSettings.OutputPath))
            {
                Directory.CreateDirectory(GlobalSettings.OutputPath);
            }
        }

        internal static async Task BulkDownload(string FilePath)
        {
            double DownloadedUrls = 0;
            double TotalUrlsToDownload = 0;
            double runningThreads = 0;

            IEnumerable<string> enumerable = File.ReadLines(FilePath);
            TotalUrlsToDownload = enumerable.Count();
            
            Console.WriteLine($"[i] Downloading {TotalUrlsToDownload} files, please wait...");
            Console.WriteLine("");

            var tasks = new List<Task>();


            Parallel.ForEach(enumerable, new ParallelOptions
            {
                MaxDegreeOfParallelism = GlobalSettings.MaxDegreeOfParallelism
            }, (url, state, index) =>
            {

                tasks.Add(Task.Run(async () =>
                {
                    
                    try
                    {
                        semaphore.Wait();

                        lock (ThreadLocker)
                        {
                            runningThreads++;
                        }

                        string outputFilename = FileUtils.ConvertUriToLocalPath(new Uri(url), GlobalSettings.OutputPath);
                        var ConsoleOutput = string.Empty;
                        if (File.Exists(outputFilename))
                        {
                            ConsoleOutput = $"Skipped '{url}' because it already exists";
                        }
                        else
                        {
                            FileUtils.CreateFolderIfNotExists(outputFilename);
                            ConsoleOutput = await ExecuteRequest(url, outputFilename);
                        }

                        lock (ThreadLocker)
                        {
                            DownloadedUrls++;
                            runningThreads--;
                            double currentProgress = Math.Round((double)(DownloadedUrls / TotalUrlsToDownload) * 100, 2);
                            // Reportar progresso para o bloco de conclusão
                            string progressBar = BuildProgressbar(DownloadedUrls, TotalUrlsToDownload);

                            Console.WriteLine($"|{progressBar}| [i] {ConsoleOutput}");
                            Console.WriteLine();
                            ConsoleOutput = string.Empty;
                        }
                    }
                    finally
                    {


                        semaphore.Release();
                    }

                }));
            });

            await Task.WhenAll(tasks);

        }

        private static string BuildProgressbar(double DownloadedUrls, double TotalUrlsToDownload)
        {
            string progressBar = new string('█', (int)Math.Round((double)(DownloadedUrls / TotalUrlsToDownload) * 10, 2, MidpointRounding.ToZero));
            progressBar = progressBar.PadRight(10, '-');
            return progressBar;
        }

        private static async Task<string> ExecuteRequest(string UrlToDownload, string OutputFilename)
        {
            string output = string.Empty;
            try
            {
                bool tryAgain = false;
                int numberOfTries = 0;
                int maximumTries = 5;
                Exception lastException = null;
                do
                {
                    numberOfTries++;
                    try
                    {
                        await HttpService.GetAndSaveAsFile(new Uri(UrlToDownload), OutputFilename);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        tryAgain = numberOfTries <= maximumTries;
                        Thread.Sleep(500);
                    }
                }
                while (tryAgain);
                if (lastException != null)
                {
                    throw lastException;
                }

                output = $"Downloaded: {UrlToDownload}";
            }
            catch (Exception ex2)
            {
                output = $"Url: {UrlToDownload} - {ex2.Message.ToString()}";
            }
            return output;
        }

    }
}
