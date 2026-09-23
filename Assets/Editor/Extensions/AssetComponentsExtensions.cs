using System;
using System.Reflection;
using AssetComponents.Components;
using AssetComponents.Models;

namespace Editor.Extensions
{
    internal static class AssetComponentsExtensions
    {
        public static void MirrorColorType(this MaterialColorer instance)
        {
            var mirrored = instance.ColorSchemeType switch
            {
                ColorSchemeType.LeftSaber => ColorSchemeType.RightSaber,
                ColorSchemeType.RightSaber => ColorSchemeType.LeftSaber,
                ColorSchemeType.EnvironmentColor0 => ColorSchemeType.EnvironmentColor1,
                ColorSchemeType.EnvironmentColor1 => ColorSchemeType.EnvironmentColor0,
                ColorSchemeType.EnvironmentColor0Boost => ColorSchemeType.EnvironmentColor1Boost,
                ColorSchemeType.EnvironmentColor1Boost => ColorSchemeType.EnvironmentColor0Boost,
                _ => instance.ColorSchemeType
            };
            var fieldInfo = instance.GetType()
                .GetField("colorSchemeType", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new NullReferenceException();
            fieldInfo.SetValue(instance, mirrored);
        }
    }
}