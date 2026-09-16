using System;
using SaberComponents.Models;
using UnityEngine;

namespace Editor.Models
{
    [Serializable]
    public class MockColorScheme
    {
        public Color saberAColor = new(0.784f, 0.078f, 0.078f);
        public Color saberBColor = new(0.157f, 0.557f, 0.824f);

        [Space]
        public Color obstaclesColor = new(1f, 0.188f, 0.188f);

        [Space]
        public Color environmentColor0 = new(0.85f, 0.085f, 0.085f);

        public Color environmentColor1 = new(0.019f, 0.675f, 1f);
        public Color environmentColorW = Color.white;
        
        [Space]
        public Color environmentColor0Boost = new(0.85f, 0f, 0.85f);
        public Color environmentColor1Boost = new(0f, 0.85f, 0.85f);
        public Color environmentColorWBoost = Color.white;
        
        public static MockColorScheme Default { get; } = new();

        public Color ColorForType(ColorSchemeType type) => type switch
        {
            ColorSchemeType.LeftSaber => saberAColor,
            ColorSchemeType.RightSaber => saberBColor,
            ColorSchemeType.ObstaclesColor => obstaclesColor,
            ColorSchemeType.EnvironmentColor0 => environmentColor0,
            ColorSchemeType.EnvironmentColor1 => environmentColor1,
            ColorSchemeType.EnvironmentColorW => environmentColorW,
            ColorSchemeType.EnvironmentColor0Boost => environmentColor0Boost,
            ColorSchemeType.EnvironmentColor1Boost => environmentColor1Boost,
            ColorSchemeType.EnvironmentColorWBoost => environmentColorWBoost,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}