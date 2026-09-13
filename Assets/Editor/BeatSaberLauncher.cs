using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using Debug = UnityEngine.Debug;

public class BeatSaberLauncher
{
    private static SaberProjectSettings Settings => SaberProjectSettings.GetOrCreateSettings();

    [CanBeNull] private static Process console;
    [CanBeNull] private static Process beatSaber;
    
    public static bool TryStartBeatSaber(out string message)
    {
        message = string.Empty;
        try
        {
            if (console is { HasExited: false })
            {
                message = "BeatSaber is already running";
                return false;
            }
            
            if (string.IsNullOrEmpty(Settings.beatSaberPath))
            {
                message = "Please set the Beat Saber path";
                return false;
            }
            
            var beatSaberExe = new FileInfo(Path.Combine(Settings.beatSaberPath, "Beat Saber.exe"));
            if (!beatSaberExe.Exists)
            {
                message = "Beat Saber.exe not found at given path.";
                return false;
            }

            beatSaber = LaunchExe();
            if (beatSaber == null)
            {
                message = "Could not start BeatSaber, failed to create process";
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Could not start BeatSaber");
            Debug.LogException(e);
            message = $"Encountered a problem: {e.GetType().Name}, check console";
            return false;
        }
    }

    private static Process LaunchExe()
    {
        // Start the debug console first
        console = Process.Start("cmd.exe");
        if (console is null)
        {
            throw new("Failed to start console process");
        }
        
        var beatSaber = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Settings.beatSaberPath, "Beat Saber.exe"),
            WorkingDirectory = Settings.beatSaberPath,
            ArgumentList = { "--no-yeet", "--verbose", $"{console.Id}" ,"fpfc" },
            UseShellExecute = false,
            CreateNoWindow = false,
            Environment =
            {
                { "SteamAppId", "620980" },
                { "SteamGameId", "620980" },
                { "SteamOverlayGameId", "620980" }
            }
        });

        if (beatSaber is null)
        {
            console.Kill();
            console.Dispose();
            throw new("Failed to start Beat Saber process");
        }
        
        return beatSaber;
    }
}