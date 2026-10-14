// Консольная проверка ядра без Unity (необязательно; основной способ — меню Biskas в Unity).
// Сборка: dotnet build Tools/CoreCheck ; запуск: dotnet run --project Tools/CoreCheck
using System;
using System.IO;
using System.Text.Json;
using Biskas.Core;

public static class Program
{
    public static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : FindRoot();
        string json = File.ReadAllText(Path.Combine(root, "Assets/_Game/Data/episode1_ru.json"));
        var content = JsonSerializer.Deserialize<EpisodeContent>(json, new JsonSerializerOptions { IncludeFields = true });
        string tmp = Path.Combine(Path.GetTempPath(), "biskas_corecheck");
        Directory.CreateDirectory(tmp);
        var report = EpisodeSelfTest.Run(content, tmp);
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine(report.ToString());
        return report.Ok ? 0 : 1;
    }

    private static string FindRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets"))) d = d.Parent;
        return d?.FullName ?? Directory.GetCurrentDirectory();
    }
}
