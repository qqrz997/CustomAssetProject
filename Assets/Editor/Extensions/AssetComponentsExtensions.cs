using AssetComponents.Components;
using AssetComponents.Models;

namespace Editor.Extensions
{
    internal static class AssetComponentsExtensions
    {
        public static void MirrorColorType(this MaterialColorer instance)
        {
            instance.colorSchemeType = instance.colorSchemeType switch
            {
                ColorSchemeType.LeftSaber => ColorSchemeType.RightSaber,
                ColorSchemeType.RightSaber => ColorSchemeType.LeftSaber,
                ColorSchemeType.EnvironmentColor0 => ColorSchemeType.EnvironmentColor1,
                ColorSchemeType.EnvironmentColor1 => ColorSchemeType.EnvironmentColor0,
                ColorSchemeType.EnvironmentColor0Boost => ColorSchemeType.EnvironmentColor1Boost,
                ColorSchemeType.EnvironmentColor1Boost => ColorSchemeType.EnvironmentColor0Boost,
                _ => instance.colorSchemeType
            };
        }
    }
}