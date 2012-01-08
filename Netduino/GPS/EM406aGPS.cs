namespace Bewarro.Netduino.GPS
{
    using System;
    using System.IO.Ports;
    using System.Text;
    using System.Threading;

    using Bewarro.Netduino.GPS.NMEA;

    public delegate void GPSPositionEventHandler(object sender, GPSPosition message);

    public class GPSPosition
    {
        public bool HasFix;
        public bool IsEstimate;
        public double Latitude;
        public double Longitude;
    }

    public class EM406aGPS : IDisposable
    {
        private const string CrLf = "\r\n";

        private Thread _thread;

        private SerialPort _port;

        public event GPSPositionEventHandler PositionReceived;

        public EM406aGPS(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _port.Open();
        }

        public static EM406aGPS Start(string portName, int baudRate = 4800, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            EM406aGPS gps = new EM406aGPS(portName, baudRate, parity, dataBits, stopBits);
            gps._thread = new Thread(gps.Run);
            gps._thread.Start();
            return gps;
        }

        public void Stop()
        {
            _thread.Abort();
        }

        public void Dispose()
        {
            Stop();
        }

        private void Run()
        {
            try
            {
                string workingBuffer = string.Empty;

                while (true)
                {
                    int bytesToRead = _port.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] buffer = new byte[bytesToRead];
                        _port.Read(buffer, 0, buffer.Length);

                        string text = new string(Encoding.UTF8.GetChars(buffer));
                        workingBuffer += text.ToString();

                        int sentenceEndIndex;
                        while ((sentenceEndIndex = workingBuffer.IndexOf(CrLf)) != -1)
                        {
                            string sentence = workingBuffer.Substring(0, sentenceEndIndex);
                            if (sentence.IndexOf('$') != -1)
                            {
                                IGPSMessage message = GPSMessageParser.Parse(sentence);
                                if (message != null)
                                {
                                    if (message is FixedDataMessage)
                                    {
                                        GPSPosition position = ParsePosition(message as FixedDataMessage);
                                        if (PositionReceived != null)
                                            PositionReceived(this, position);
                                    }
                                }
                            }

                            if (sentenceEndIndex + CrLf.Length == workingBuffer.Length)
                            {
                                workingBuffer = string.Empty;
                            }
                            else
                            {
                                workingBuffer = workingBuffer.Substring(sentenceEndIndex + CrLf.Length, workingBuffer.Length - sentenceEndIndex - CrLf.Length);
                            }

                            sentenceEndIndex = workingBuffer.IndexOf(CrLf);
                        }

                        Thread.Sleep(100);
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        private GPSPosition ParsePosition(FixedDataMessage message)
        {
            GPSPosition position = new GPSPosition();

            position.HasFix = (message.PositionFix != PositionFixIndicator.Invalid);
            position.IsEstimate = (message.PositionFix == PositionFixIndicator.Estimated);

            if (position.HasFix)
            {
                position.Latitude = Convert.ToDouble(message.Latitude.Substring(0, 2)) + (Convert.ToDouble(message.Latitude.Substring(2, 7)) / 60);
                if (message.NSIndicator == 'S')
                    position.Latitude = -position.Latitude;

                position.Longitude = Convert.ToDouble(message.Longitude.Substring(0, 3)) + (Convert.ToDouble(message.Longitude.Substring(3, 7)) / 60);
                if (message.EWIndicator == 'W')
                    position.Longitude = -position.Longitude;
            }

            return position;
        }
    }

}
