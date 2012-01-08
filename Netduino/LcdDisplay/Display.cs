namespace Bewarro.Netduino.LcdDisplay
{
    using System;
    using System.Collections;
    using System.IO.Ports;
    using System.Text;
    using Bewarro.Netduino.LcdDisplay.Controls;

    public class Display : IDisposable
    {
        #region Consts

        private const byte Command = 0xFE;
        private const byte Command_Backlight = 0x7C;

        private const byte Command_Clear = 0x01;
        private const byte Command_DisplayOn = 0x0C;
        private const byte Command_DisplayOff = 0x08;
        private const byte Command_ScrollLeft = 0x18;
        private const byte Command_ScrollRight = 0x1C;
        private const byte Command_SetCursorPositionBase = 0x80;
        private const byte Command_SetCursorBlinkingOn = 0x0D;
        private const byte Command_SetCursorBlinkingOff = 0x0C;

        private const int MaxBacklightBrightness = 30;

        private readonly int[] LineCursorOffsets = new int[] { 0, 64, 16, 80 };

        #endregion

        #region Fields

        private SerialPort _displayPort;

        private int _width;
        private int _height;

        ArrayList _pages = new ArrayList();
        int _currentPageIndex = 0;

        bool _showPageNumbers = false;

        IControl _focusControl;

        #endregion 

        #region Properties

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public ArrayList Pages
        {
            get { return _pages; }
            set { _pages = value; }
        }

        public int CurrentPageIndex
        {
            get { return _currentPageIndex; }
            set 
            {
                if (_currentPageIndex < 0 || _currentPageIndex > _pages.Count - 1)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _currentPageIndex = value;
                InitialisePage();
            }
        }

        public bool ShowPageNumbers
        {
            get { return _showPageNumbers; }
            set { _showPageNumbers = value; }
        }


        public int Brightness
        {
            set
            {
                if (value < 0 || value > MaxBacklightBrightness)
                {
                    throw new ArgumentOutOfRangeException("Brightness out of range");
                }

                byte[] bytes = new byte[] { Command_Backlight, (byte)(value + 128) };
                _displayPort.Write(bytes, 0, bytes.Length);
            }
        }

        private Page CurrentPage
        {
            get
            {
                return (Page)_pages[_currentPageIndex];
            }

        }

        #endregion

        #region Methods

        public Display(int width, int height, string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _width = width;
            _height = height;

            _displayPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _displayPort.Open();
        }

        public void InitialisePage()
        {
            if (_currentPageIndex < 0 || _currentPageIndex > _pages.Count - 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            SendCommand(Command_Clear);

            if (_showPageNumbers)
            {
                string pageNumbers = (_currentPageIndex + 1).ToString() + "/" + _pages.Count.ToString();

                DrawString(pageNumbers, _width - pageNumbers.Length);
            }

            DrawControls(false);

            SetInitialFocus();
        }

        public void DrawControls(bool dirtyOnly = true)
        {
            if (_currentPageIndex < 0 || _currentPageIndex > _pages.Count - 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Switch the blibking cursor off while we redraw controls
            SetCursorType(Command_SetCursorBlinkingOff);

            foreach (IControl control in CurrentPage.Controls)
            {
                if (control.IsDirty || dirtyOnly == false)
                {
                    string controlText = control.Render();

                    if (controlText.Length < control.Width)
                    {
                        controlText += new string(' ', control.Width - controlText.Length); // Right pad the string to the control width
                    }
                    else if (controlText.Length > control.Width)
                    {
                        controlText = controlText.Substring(0, control.Width); // Trim the string to the control width
                    }

                    DrawString(controlText, control.Position);

                    control.IsDirty = false;
                }
            }

            // If a control had focus, reset the blinking cursor at the correct position
            if (_focusControl != null)
            {
                SetFocusControl(_focusControl);
            }
        }

        public void NextPage()
        {
            CurrentPageIndex = (CurrentPageIndex == Pages.Count - 1) ? 0 : CurrentPageIndex + 1; 
        }

        public void PreviousPage()
        {
            CurrentPageIndex = (CurrentPageIndex == 0) ? Pages.Count - 1 : CurrentPageIndex - 1; 
        }

        public void SetNextControlFocus()
        {
            if (CurrentPage.Controls.Count > 0)
            {
                int i = 0;
                while (true)
                {
                    // Find the current focussed control
                    IControl control = CurrentPage.Controls[i] as IControl;
                    if (control == _focusControl)
                    {
                        while (true)
                        {
                            // Find the next focusable control
                            i = (i == CurrentPage.Controls.Count - 1) ? 0 : i + 1;

                            IControl nextControl = CurrentPage.Controls[i] as IControl;
                            if (nextControl.ReceivesFocus)
                            {
                                SetFocusControl(nextControl);
                                return;
                            }
                        }
                    }

                    ++i;
                }
            }
        }


        private void SendCommand(byte command)
        {
            byte[] bytes = new byte[] { Command, command };
            _displayPort.Write(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            if (_displayPort != null)
            {
                _displayPort.Dispose();
            }
        }

        private void DrawString(string message, int position)
        {
            SetCursorPosition(position);

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            _displayPort.Write(bytes, 0, bytes.Length);
        }

        private void SetCursorType(byte type)
        {
            SendCommand(type);
        }

        private void SetCursorPosition(int position)
        {
            int line = position / _width;
            int offset = position % _width;
            byte cursorPosition = (byte)(LineCursorOffsets[line] + offset + Command_SetCursorPositionBase);

            SendCommand(cursorPosition);
        }

        private void SetInitialFocus()
        {
            _focusControl = null;

            ArrayList controls = GetFocusableControls();
            if (controls.Count > 0)
            {
                SetFocusControl(controls[0] as IControl);
            }
        }

        private void SetFocusControl(IControl control)
        {
            _focusControl = control;

            SetCursorType(Command_SetCursorBlinkingOn);
            SetCursorPosition(_focusControl.Position);
        }

        private ArrayList GetFocusableControls()
        {
            ArrayList controls = new ArrayList();

            foreach (IControl control in CurrentPage.Controls)
            {
                if (control.ReceivesFocus == true)
                {
                    controls.Add(control);
                }
            }

            return controls;
        }
        
        #endregion
    }
}
