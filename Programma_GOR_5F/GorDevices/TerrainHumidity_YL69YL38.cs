using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gor.Devices
{
    public class TerrainHumidity_YL69YL38 : Sensor, IMCP3208Convertible
    {

        public int Channel { get; set; }

        public Adc_MCP3208 Connection { get; set; }

        Random rnd;

        private bool firstValue = true;

        private double LastValue { get; set; }

        public TerrainHumidity_YL69YL38() : this(true)
        {

        }

        public TerrainHumidity_YL69YL38(bool sim) : base(sim)
        {
            MinValue = 0.826;
            
            MaxValue = 3.198;
            
            
        }

        public TerrainHumidity_YL69YL38(int channel) : base(false)
        {
            Channel = channel;
        }

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
            
            if (Simulation)
            {
                rnd = new Random();
                if (firstValue)
                {
                    do
                    {
                        LastValue = (rnd.Next(0, 4) + rnd.NextDouble());

                    } while (LastValue > MaxValue || LastValue < MinValue);
                    firstValue = false;

                }
                else
                {
                    bool ok = false;
                    do
                    {
                        double varianza = (rnd.Next(0, 2) + rnd.NextDouble()) / 100;
                        if (rnd.Next(0, 2) == 0 && (LastValue - varianza) > MinValue)
                        {
                            LastValue -= varianza;
                            ok = true;
                        }
                        else if ((LastValue + varianza) < MaxValue)
                        {
                            LastValue += varianza;
                            ok = true;
                        }
                    } while (!ok);
                }

                return new Measurement()
                {
                    Value = Math.Round((LastValue - MinValue) / 0.0315, 4),
                    Unit = "%"
                };
            }
            else
            {
                string read = Read();

                return new Measurement
                {
                    Value = calibration.Calculate(double.Parse(read)),
                    Unit = "[%]",
                    Name = "Terrain Humidity",
                    ReadValue = read
                };
            }
        }

        public override void Initialization()
        {
            calibration = new Calibration_2Points(CalibrationFileName);
        }
    }
}
