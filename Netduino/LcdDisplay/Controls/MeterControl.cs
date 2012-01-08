namespace Bewarro.Netduino.LcdDisplay.Controls
{
    using System;

    public class MeterControl : Control
    {
        private const string ErrorMessage = "*ERROR*";

        private char _character;
        private int _minValue;
        private int _maxValue;
        private int _currentValue;

        private int _currentCharacterCountToRender;

        public char Character
        {
            get { return _character; }
            set 
            { 
                _character = value;
                _isDirty = true;
            }
        }

        public int MinValue
        {
            get { return _minValue; }
            set 
            { 
                _minValue = value;
                _isDirty = true;
            }
        }

        public int MaxValue
        {
            get { return _maxValue; }
            set 
            { 
                _maxValue = value;
                _isDirty = true;
            }
        }

        public int CurrentValue
        {
            get { return _currentValue; }
            set 
            {
                if (value > _maxValue || value <= _minValue)
                {
                    throw new ArgumentOutOfRangeException("CurrentValue is out of range");
                }

                int newCharacterCountToRender = CalculateCharacterCountToRender(_minValue, _maxValue, value, _width);
                if (newCharacterCountToRender != _currentCharacterCountToRender)
                {
                    _currentCharacterCountToRender = newCharacterCountToRender;
                    _isDirty = true;
                }

                _currentValue = value;
            }
        }

        public override string Render()
        {
            string characters;
            string padding;

            if (_currentCharacterCountToRender >= 0)
            {
                characters = new string(_character, _currentCharacterCountToRender);
                padding = new string(' ', _width - _currentCharacterCountToRender);
            }
            else
            {
                characters = ErrorMessage;
                padding = new string(' ', _width - ErrorMessage.Length);
            }

            return characters + padding;
        }

        private static int CalculateCharacterCountToRender(int minValue, int maxValue, int currentValue, int width)
        {
            int range = maxValue - minValue;
            int value = currentValue - minValue;

            return (int)(((float)value / (float)range) * width);
        }
    }
}
