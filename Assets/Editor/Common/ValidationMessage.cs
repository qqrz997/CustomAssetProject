using UnityEngine;

namespace Editor.Common
{
    public class ValidationMessage
    {
        public static Color SuccessColor { get; } = new(0.25f, 0.65f, 0.25f);
        public static Color WarningColor { get; } = new(0.7f, 0.46f, 0f);
        public static Color ErrorColor { get; } = new(0.7f, 0f, 0f);
    
        private ValidationMessage(string message, Color color)
        {
            Message = message;
            Color = color;
        }
    
        public string Message { get; }
        public Color Color { get; }

        public static ValidationMessage Warning(string message) =>
            new(message, WarningColor);

        public static ValidationMessage Error(string message) =>
            new(message, ErrorColor);

        public void Deconstruct(out string message, out Color color)
        {
            message = Message;
            color = Color;
        }
    }
}