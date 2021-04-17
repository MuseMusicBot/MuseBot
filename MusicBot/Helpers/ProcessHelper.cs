using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MusicBot.Helpers
{
    public static class ProcessHelper
    {
        public static string GetJavaVersion()
        {
            Process p = Process.Start(new ProcessStartInfo
            {
                FileName = "java",
                CreateNoWindow = true,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            string s = p.StandardError.ReadLine();

            Regex r = new Regex(@"(?:(\d+)\.\d+\.\d+)");
            Match m;
            if ((m = r.Match(s)).Success)
            {
                return m.Groups[1].Value;
            }

            return string.Empty;
        }

        //TODO: Start lavalink on bot start
        //LavalinkTask = Task.Factory.StartNew(() =>
        //{
        //    var logging = services.GetRequiredService<LoggingService>();
        //    var logger = logging.CreateLogger("LavaLink");
        //    Process p = new Process
        //    {
        //        StartInfo = new ProcessStartInfo
        //        {
        //            FileName = "java.exe",
        //            WorkingDirectory = @"C:\\tools",
        //            Arguments = "-jar lavalink.jar",
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true
        //        }
        //    };

        //    if (!p.Start())
        //    {
        //        p.Kill();
        //        Console.WriteLine("Lavalink did not start properly, stopping execution.");
        //        Environment.Exit(255);
        //    }

        //    while (!p.HasExited)
        //    {
        //        logger.LogInformation(p.StandardOutput.ReadToEnd());
        //    }


        //    Task.Delay(-1);
        //});
    }
}
