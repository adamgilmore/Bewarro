namespace Bewarro.Netduino.GPS.NMEA
{
    using System;

    public interface IGPSMessage
    {
    }

    public class FixedDataMessage : IGPSMessage
    {
        public TimeSpan Timestamp;
        public string Latitude;
        public char NSIndicator;
        public string Longitude;
        public char EWIndicator;
        public PositionFixIndicator PositionFix;
        public string SatallitesUsed;
        public double HDOP;
        public bool IsHDOPValid;
        public double MSLAltitude;
        public bool IsMSLAltitudeValid;
        public char MSLAltitudeUnits;
        public bool IsMSLAltitudeUnitsValid;
        public double GeoidSeperation;
        public bool IsGeoidSeperationValid;
        public char GeoidSeperationUnits;
        public bool IsGeoidSeperationUnitsValid;
        public int AgeOfDifferentialCorrection;
        public bool IsAgeOfDifferentialCorrectionValid;
        public string DifferentialReferenceStationId;
    }

    public enum PositionFixIndicator
    {
        Invalid = 0,
        SPS = 1,
        DifferentialSPS = 2,
        PPS = 3,
        RealTimeKinematic = 4,
        FloatRTK = 5,
        Estimated = 6,
        ManualInput = 7,
        Simulation = 8
    }

    internal static class GPSMessageParser
    {
        private static readonly PositionFixIndicator[] _postitionFixIndicatorValues;

        static GPSMessageParser()
        {
            _postitionFixIndicatorValues = new PositionFixIndicator[] 
                { 
                    PositionFixIndicator.Invalid,  
                    PositionFixIndicator.SPS,
                    PositionFixIndicator.DifferentialSPS,
                    PositionFixIndicator.PPS,
                    PositionFixIndicator.RealTimeKinematic,
                    PositionFixIndicator.FloatRTK,
                    PositionFixIndicator.Estimated,
                    PositionFixIndicator.ManualInput,
                    PositionFixIndicator.Simulation
                };
        }

        public static IGPSMessage Parse(string sentence)
        {
            IGPSMessage message = null;

            int checksumIndex = sentence.IndexOf("*");
            if (checksumIndex == -1)
            {
                throw new Exception("No checksum found");
            }

            if (checksumIndex == sentence.Length - 1)
            {
                throw new Exception("No checksum after marker");
            }

            string checksum = sentence.Substring(checksumIndex + 1, sentence.Length - checksumIndex - 1);
            sentence = sentence.Substring(0, checksumIndex);

            string[] messageFields = sentence.Split(new char[] { ',' });
            if (messageFields == null || messageFields.Length == 0)
            {
                throw new Exception("No fields in the message");
            }

            switch (messageFields[0])
            {
                case "$GPGGA":
                {
                    FixedDataMessage fdMessage = new FixedDataMessage();

                    fdMessage.Timestamp = ParseTime(messageFields[1]);
                    fdMessage.Latitude = messageFields[2];
                    fdMessage.NSIndicator = ParseChar(messageFields[3]);
                    fdMessage.Longitude = messageFields[4];
                    fdMessage.EWIndicator = ParseChar(messageFields[5]);
                    fdMessage.PositionFix = ParsePositionFixIndicator(messageFields[6]);
                    fdMessage.SatallitesUsed = messageFields[7];
                    ParseDouble(messageFields[8], out fdMessage.HDOP, out fdMessage.IsHDOPValid);
                    ParseDouble(messageFields[9], out fdMessage.MSLAltitude, out fdMessage.IsMSLAltitudeValid);
                    fdMessage.MSLAltitudeUnits = ParseChar(messageFields[10], out fdMessage.IsMSLAltitudeUnitsValid);
                    ParseDouble(messageFields[11], out fdMessage.GeoidSeperation, out fdMessage.IsGeoidSeperationValid);
                    fdMessage.GeoidSeperationUnits = ParseChar(messageFields[12], out fdMessage.IsGeoidSeperationUnitsValid);
                    ParseInt(messageFields[13], out fdMessage.AgeOfDifferentialCorrection, out fdMessage.IsAgeOfDifferentialCorrectionValid);
                    fdMessage.DifferentialReferenceStationId = messageFields[14];

                    message = fdMessage;
                    break;
                }
            }

            return message;
        }

        private static PositionFixIndicator ParsePositionFixIndicator(string indicatorString)
        {
            int indicatorIndex = Convert.ToInt32(indicatorString);
            if (indicatorIndex > _postitionFixIndicatorValues.Length)
                throw new Exception("Unknown PositionFixIndicator value: " + indicatorString);
            else
                return _postitionFixIndicatorValues[indicatorIndex];
        }

        private static char ParseChar(string valueString)
        {
            if (valueString == null || valueString.Length == 0)
            {
                return ' ';
            }
            else
            {
                return (char)valueString[0];
            }
        }

        private static char ParseChar(string valueString, out bool isValueValid)
        {
            char value = ParseChar(valueString);
            isValueValid = (value != ' ');
            return value;
        }

        private static void ParseDouble(string valueString, out double value, out bool isValueValid)
        {
            value = 0;
            isValueValid = false;

            if (valueString != null && valueString.Trim().Length > 0)
            {
                value = Convert.ToDouble(valueString);
                isValueValid = true;
            }
        }

        private static void ParseInt(string valueString, out int value, out bool isValueValid)
        {
            value = 0;
            isValueValid = false;

            if (valueString != null && valueString.Trim().Length > 0)
            {
                value = Convert.ToInt32(valueString);
                isValueValid = true;
            }
        }

        private static TimeSpan ParseTime(string valueString)
        {
            int hours = Convert.ToInt32(valueString.Substring(0, 2));
            int minutes = Convert.ToInt32(valueString.Substring(2, 2));
            int seconds = Convert.ToInt32(valueString.Substring(4, 2));
            int milliseconds = Convert.ToInt32(valueString.Substring(7, 3));

            return new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }
    }
}
