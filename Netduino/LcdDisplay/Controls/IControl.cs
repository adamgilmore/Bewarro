namespace Bewarro.Netduino.LcdDisplay.Controls
{
    public interface IControl
    {
        int Position { get; set; }
        int Width { get; set; }
        bool IsDirty { get; set; }
        bool ReceivesFocus { get; }

        string Render();
    }
}
