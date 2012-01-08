namespace Bewarro.Netduino.LcdDisplay.Controls
{
    public class EditControl : Control
    {
        private const char PlaceholderCharacter = '_';

        public EditControl()
        {
            _receivesFocus = true;
        }

        public override string Render()
        {
            return new string(PlaceholderCharacter, _width);
        }
    }
}
