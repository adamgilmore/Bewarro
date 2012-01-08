namespace Bewarro.Netduino.LcdDisplay.Controls
{
    public class Control : IControl
    {
        protected int _position;
        protected int _width = 0;
        protected bool _isDirty = true;
        protected bool _receivesFocus = false;

        public int Position
        {
            get { return _position; }
            set 
            { 
                _position = value;
                _isDirty = true;
            }
        }

        public int Width
        {
            get { return _width; }
            set 
            { 
                _width = value;
                _isDirty = true;
            }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }

        public bool ReceivesFocus
        {
            get { return _receivesFocus; }
        }

        public virtual string Render()
        {
            return string.Empty;
        }
    }
}
