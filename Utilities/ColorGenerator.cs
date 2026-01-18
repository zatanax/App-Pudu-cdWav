namespace App.Utilities
{
    public static class ColorGenerator
    {
        private static readonly Color[] PredefinedColors = new[]
        {
            Color.FromArgb(255, 99, 132),   // Red
            Color.FromArgb(54, 162, 235),   // Blue
            Color.FromArgb(255, 205, 86),   // Yellow
            Color.FromArgb(75, 192, 192),   // Teal
            Color.FromArgb(153, 102, 255),  // Purple
            Color.FromArgb(255, 159, 64),   // Orange
            Color.FromArgb(201, 203, 207),  // Gray
            Color.FromArgb(255, 99, 255),   // Pink
            Color.FromArgb(54, 235, 162),   // Green
            Color.FromArgb(205, 86, 255),   // Lavender
            Color.FromArgb(192, 75, 192),   // Magenta
            Color.FromArgb(102, 153, 255),  // Light Blue
            Color.FromArgb(255, 205, 159),  // Peach
            Color.FromArgb(99, 255, 132),   // Lime
            Color.FromArgb(162, 54, 235),   // Violet
            Color.FromArgb(86, 255, 205)    // Aqua
        };

        public static Color GetColorForIndex(int index)
        {
            return PredefinedColors[index % PredefinedColors.Length];
        }
    }
}
