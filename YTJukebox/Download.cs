using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace YTJukeboxMod
{
    static public class Download
    {

        private static string lastURL;
        private static readonly string ffmpegURL = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
        private static readonly string ytDlpURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

        static public async Task GetDependencies()
        {
            await DownloadYtDlp();
            await DownloadFfmpeg();
        }

        static private async Task DownloadYtDlp()
        {
            try
            {
                Log.Info("Downloading yt-dlp...");
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(ytDlpURL), ModPaths.yt_dlp);
                }
                Log.Info("yt-dlp downloaded successfully!");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to download yt-dlp: " + ex.Message);
            }
        }

        static private async Task DownloadFfmpeg()
        {
            try
            {
                string ffmpegTempZipPath = Path.Combine(ModPaths.dependencies, "ffmpeg.zip");
                Log.Info("Downloading ffmpeg...");
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(ffmpegURL), ffmpegTempZipPath);
                }
                Log.Info("ffmpeg downloaded successfully!");

                await Task.Run(() => ExtractFfmpeg(ffmpegTempZipPath, ModPaths.dependencies));

                File.Delete(ffmpegTempZipPath);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to download ffmpeg: " + ex.Message);
            }
        }

        static private void ExtractFfmpeg(string zipPath, string extractPath)
        {
            try
            {
                Log.Info("Extracting ffmpeg...");

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    var ffmpegEntry = archive.Entries.FirstOrDefault(entry => entry.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
                    if (ffmpegEntry != null)
                    {
                        string destinationPath = Path.Combine(extractPath, "ffmpeg.exe");

                        ffmpegEntry.ExtractToFile(destinationPath, true);
                        Log.Info("ffmpeg.exe extracted successfully!");
                    }
                    else
                    {
                        Log.Error("ffmpeg.exe not found in the zip archive!");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to extract ffmpeg: " + ex.Message);
            }
        }

        static public async Task<bool> GetCustomSong(string URLInput)
        {
            if (!URLInput.StartsWith("http"))
            {
                return false;
            }
            if (lastURL != null && lastURL == URLInput)
            {
                if (File.Exists(ModPaths.customSong))
                {
                    return true;
                }
            }

            Log.Info("Play button pressed with URL: " + URLInput);

            if (File.Exists(ModPaths.customSong))
            {
                File.Delete(ModPaths.customSong);
            }

            ProcessStartInfo ytDlpProcess = new ProcessStartInfo
            {
                FileName = ModPaths.yt_dlp,
                Arguments = $"--ffmpeg-location \"{ModPaths.ffmpeg}\" -f bestaudio -x --audio-format wav -o \"{ModPaths.customSong}\" {URLInput}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = ytDlpProcess;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                int exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    lastURL = URLInput;
                    Log.Info("yt-dlp finished successfully");
                    Log.Info("Output: " + output);
                    return true;
                }
                else
                {
                    Log.Error("yt-dlp encountered an error: " + error);
                    return false;
                }
            }
        }
    }
}
