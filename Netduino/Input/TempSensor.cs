

namespace Bewarro.Netduino.Input
{
    using System;
    using System.Threading;

    using Microsoft.SPOT.Hardware;

    using SecretLabs.NETMF.Hardware;

    public delegate void TempSensorSampleEventHandler(object sender, float sample);

    public enum TempSensorMode
    {
        Celcius,
        Farenheit
    }

    public class TempSensor : IDisposable
    {
        private const float PinVoltageRange = 3.3f;
        private const int PinMaxValue = 1024;

        // (Voltage - 500mv) * 100) = Degrees Celcius
        // -50C = 0mv, -25C = 250mv, 0C = 500mv, 25C = 750mV
        private const float SensorZeroDegreesVoltage = 0.500f; // 500mV @ 0 degrees celcius

        private AnalogInput _sensor;

        private int _sampleInterval;

        private TempSensorMode _mode;

        private Thread _thread;

        private float _sample;

        public TempSensorMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        public float Sample
        {
            get { return _sample; }
            set { _sample = value; }
        }

        public event TempSensorSampleEventHandler SampleReceived;

        public TempSensor(Cpu.Pin pin, int sampleInterval, TempSensorMode mode)
        {
            _sensor = new AnalogInput(pin);
            _sampleInterval = sampleInterval;
            _mode = mode;
        }

        public static TempSensor Start(Cpu.Pin pin, int sampleInterval = 1000, TempSensorMode mode = TempSensorMode.Celcius)
        {
            TempSensor sensor = new TempSensor(pin, sampleInterval, mode);
            sensor._thread = new Thread(sensor.Run);
            sensor._thread.Start();
            return sensor;
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
                while (true)
                {
                    float newSample = (_mode == TempSensorMode.Celcius) ? GetDegreesCelcius() : GetDegreesFarenheit();
                    if (newSample != _sample)
                    {
                        _sample = newSample;

                        if (SampleReceived != null)
                        {
                            SampleReceived(this, _sample);
                        }
                    }

                    Thread.Sleep(_sampleInterval);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }

        private float GetDegreesCelcius()
        {
            float pinVoltage = _sensor.Read() * (PinVoltageRange / PinMaxValue);
            return (pinVoltage - SensorZeroDegreesVoltage) * 100f;
        }

        private float GetDegreesFarenheit()
        {
            return (GetDegreesCelcius() * 1.8f) + 32;
        }
    }
}
