using Raspberry.IO.SerialPeripheralInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gor.Devices
{
    public class RelativeHumidity_HIH4000 : Sensor, IMCP3208Convertible
    {
        public int Channel { get; set; }
        public Adc_MCP3208 Connection { get; set; }

        private bool firstValue = true;

        private Measurement LastMeasurement { get; set; }

        public RelativeHumidity_HIH4000() : this(true)
        {
            MaxValue = 0.826;
            MinValue = 3.198;
        }

        public RelativeHumidity_HIH4000(bool sim) : base(sim)
        {

        }

        public RelativeHumidity_HIH4000(int channel) : base(false)
        {
            Channel = channel;
        }

        private double _startRead = 2;

        public override string Read()
        {
            if (Connection == null)
                throw new Exception("Nessuna connessione.");

            double val = Connection.Read(Channel) * voltage / 4096;

            return val.ToString();
        }

        public override int ReadInt()
        { return -1; }
        public override Measurement Measure()
        {
            Random rnd = new Random();
            double value;
            if(Simulation)
            {
                if (firstValue)
                    value = rnd.Next(0, 4)+rnd.NextDouble();
                
                rnd.Next(0, 101);
            }
            return new Measurement();
        }

        public override void Initialization()
        {
            calibration = new Calibration_2Points(CalibrationFileName);
        }
    }
}
