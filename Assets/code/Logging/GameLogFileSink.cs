using System;
using System.IO;

public static class GameLogFileSink
{
    private static string filePath = "Logs/game_log.txt";

    public static void Configure(string path)
    {
        filePath = string.IsNullOrEmpty(path) ? null : path;
    }

    public static void Write(string line)
    {
        if (filePath == null) return;

        string dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.AppendAllText(filePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + line + Environment.NewLine);
    }
}
