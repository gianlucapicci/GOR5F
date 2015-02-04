using Raspberry.IO.SerialPeripheralInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gor.Devices
{
    public class RelativeHumidity_HIH4000 : Sensor
    {
        public int channel { get; set; }

        public Adc_MCP3208 adc { get; set; }

        private bool firstValue = true;

        public RelativeHumidity_HIH4000(bool Simulation, Adc_MCP3208 adc, int Channel)
            : base(Simulation)
        {
            this.adc = adc;

            MinValue = 0;
            MaxValue = 100;

            AlarmMin = MinValue;
            AlarmMax = MaxValue;

            LastMeasurement.Unit = "R.H."; 

            channel = Channel;

            if (Simulation)
                PrimoValore();
        }

        public override string Read()
        {
            return "";
        }

        public override int ReadInt()
        {
            if (adc == null)
                throw new Exception("Nessuna connessione.");

            return adc.Read(channel);
        }

        public override Measurement Measure()
        {
            if (Simulation)
            {
                return simulaSensore();
            }
            else
            {
                return null;
            }
        }

        public override void Initialization()
        {
            // NO!! non deve fare la taratura tutte le volte. Solo una volta e sotto controllo di un altro programma,
            // che chiama i metodi di taratura del sensore
            //calibration = new Calibration_2Points(CalibrationFileName);
        }
    }
}
