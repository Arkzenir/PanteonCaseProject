namespace CaseGame.UI.Settings
{
    /// <summary>Plain data: one selectable width/height option for the Settings screen.</summary>
    public readonly struct ResolutionOption
    {
        public ResolutionOption(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }

        public string Label => $"{Width} x {Height}";
    }
}
