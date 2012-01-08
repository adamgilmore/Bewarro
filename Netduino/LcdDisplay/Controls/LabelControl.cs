namespace Bewarro.Netduino.LcdDisplay.Controls
{
    public class LabelControl : Control
    {
        private string _text;

        public string Text
        {
            get { return _text; }
            set 
            { 
                _text = value;
                _isDirty = true;
            }
        }

        public override string Render()
        {
            return _text;
        }
    }
}
