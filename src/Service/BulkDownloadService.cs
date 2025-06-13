using BulkDownloader.Utils;
using System;
using System.Collections.Concurrent;
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
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private static readonly object _consoleLock = new();
        private static int _completedDownloads;
        private static int _runningDownloads;
        private static long _totalBytesDownloaded;

        internal static async Task BulkDownloadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Arquivo de URLs não encontrado", filePath);

            var urls = await File.ReadAllLinesAsync(filePath, cancellationToken);
            urls = urls.Where(url => !string.IsNullOrWhiteSpace(url)).ToArray();
            var totalUrls = urls.Length;

            if (totalUrls == 0)
            {
                Console.WriteLine("Nenhuma URL para baixar.");
                return;
            }

            Console.WriteLine($"[i] Baixando {totalUrls} arquivos, por favor aguarde...\n");

            // Inicializa o relatório de progresso
            var progressTimer = new Timer(ReportProgress, (totalUrls, urls),
                dueTime: 1000,
                period: 500);

            try
            {
                var downloadTasks = new List<Task>();
                var downloadQueue = new ConcurrentQueue<string>(urls);

                // Cria workers de download
                for (int i = 0; i < GlobalSettings.MaxDegreeOfParallelism; i++)
                {
                    downloadTasks.Add(Task.Run(() =>
                        DownloadWorkerAsync(downloadQueue, totalUrls, cancellationToken), cancellationToken));
                }

                await Task.WhenAll(downloadTasks);
            }
            finally
            {
                await progressTimer.DisposeAsync();
                ReportProgress((totalUrls, urls)); // Relatório final
            }
        }

        private static async Task DownloadWorkerAsync(
            ConcurrentQueue<string> queue,
            int totalCount,
            CancellationToken cancellationToken)
        {
            while (queue.TryDequeue(out string url))
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    Interlocked.Increment(ref _runningDownloads);

                    string outputPath = FileUtils.ConvertUrlToLocalPath(url, GlobalSettings.OutputPath);

                    if (File.Exists(outputPath))
                    {
                        LogMessage($"Pulado: '{url}' (arquivo existente)", ConsoleColor.DarkYellow);
                        continue;
                    }

                    FileUtils.EnsureDirectoryExists(outputPath);
                    var(success, bytesDownloaded) = await HttpService.GetAndSaveAsFile(url, outputPath, cancellationToken);
                    //var (success, bytesDownloaded) = await ExecuteRequestAsync(url, outputPath, cancellationToken);

                    if (success)
                    {
                        Interlocked.Add(ref _totalBytesDownloaded, bytesDownloaded);
                        LogMessage($"Baixado: {FormatFileSize(bytesDownloaded)} - {url}", ConsoleColor.Green);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Erro: {url} - {ex.Message}", ConsoleColor.Red);
                }
                finally
                {
                    Interlocked.Decrement(ref _runningDownloads);
                    Interlocked.Increment(ref _completedDownloads);
                }
            }
        }

        private static void ReportProgress(object state)
        {
            (int totalCount, string[] urls) = ((int, string[]))state;
            int completed = _completedDownloads;
            int running = _runningDownloads;
            double progressPercent = totalCount > 0 ? (double)completed / totalCount * 100 : 0;

            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, Math.Max(Console.CursorTop - 1, 0));
                string progressBar = BuildProgressBar(progressPercent, 50);
                string status = $"[{completed}/{totalCount}] {progressPercent:0.0}% | ";
                string threads = $"Threads: {running}/{GlobalSettings.MaxDegreeOfParallelism} | ";
                string bytes = $"Total: {FormatFileSize(_totalBytesDownloaded)}";

                Console.WriteLine($"{status}{threads}{bytes}");
                Console.WriteLine(progressBar);
            }
        }

        private static string BuildProgressBar(double percent, int barLength)
        {
            int completedLength = (int)(percent / 100 * barLength);
            return new string('█', completedLength).PadRight(barLength, '░');
        }

        private static void LogMessage(string message, ConsoleColor color)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB"];
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
