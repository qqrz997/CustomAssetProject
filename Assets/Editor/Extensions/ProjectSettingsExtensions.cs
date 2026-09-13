using System.IO;

namespace Editor.Extensions
{
    internal static class ProjectSettingsExtensions
    {
        public static bool BeatSaberDirValid(this SaberProjectSettings settings) => 
            !string.IsNullOrWhiteSpace(settings.beatSaberPath) 
            && File.Exists(Path.Combine(settings.beatSaberPath, "Beat Saber.exe"));

        public static string GetExportFilename(this SaberProjectSettings settings, string saberName)
        {
            var exportName = Path.GetFileNameWithoutExtension(settings.exportFilename);
            exportName = exportName.Replace("{ModelName}", saberName);
            exportName = exportName.Replace("{AuthorName}", settings.author);
            return $"{exportName}.saber2";
        }
    }
}