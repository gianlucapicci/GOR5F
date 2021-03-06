#region References

using System;
using System.Globalization;
using Raspberry.IO.InterIntegratedCircuit;

#endregion

namespace Raspberry.IO.Components.Expanders.Pcf8563
{
    /// <summary>
    /// Represents a I2C connection to a Pcf8563 Real Time Clock.
    /// </summary>
    /// <remarks>See <see cref="http://www.ti.com/lit/ds/symlink/Pcf8563.pdf"/> for more information.</remarks>
    public class Pcf8563I2cConnection
    {
        #region Fields

        private readonly I2cDeviceConnection connection;

        private byte inputPins;
        private byte currentStatus;

        #endregion

        #region Instance Management

        /// <summary>
        /// Initializes a new instance of the <see cref="Pcf8563I2cConnection"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public Pcf8563I2cConnection(I2cDeviceConnection connection)
        {
            this.connection = connection;
            connection.WriteByte(0);
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Sets the pin status.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <param name="enabled">if set to <c>true</c>, specified pin is enabled.</param>
        public void SetPinStatus(Pcf8563Pin pin, bool enabled)
        {
            var bit = GetPinBit(pin);
            if ((inputPins & bit) != 0x00)
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Cannot set value of input pin {0}", pin));

            var status = currentStatus;
            var newStatus = (byte)(enabled
                ? status | bit
                : status & ~bit);

            connection.Write(newStatus);
            currentStatus = newStatus;
        }

        /// <summary>
        /// Gets the pin status.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <returns></returns>
        public bool GetPinStatus(Pcf8563Pin pin)
        {
            var bit = GetPinBit(pin);
            if ((inputPins & bit) == 0x00)
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Cannot get value of input pin {0}", pin));

            var status = connection.ReadByte();
            return (status & bit) != 0x00;
        }

        /// <summary>
        /// Toogles the specified pin.
        /// </summary>
        /// <param name="pin">The pin.</param>
        public void Toogle(Pcf8563Pin pin)
        {
            var bit = GetPinBit(pin);
            if ((inputPins & bit) != 0x00)
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Cannot set value of input pin {0}", pin));
            
            var status = currentStatus;

            var bitEnabled = (status & bit) != 0x00;
            var newBitEnabled = !bitEnabled;

            var newStatus = (byte)(newBitEnabled
                ? status | bit
                : status & ~bit);

            connection.Write(newStatus);
            currentStatus = newStatus;
        }

        #endregion

        #region Internal Helpers

        internal void SetInputPin(Pcf8563Pin pin, bool isInput)
        {
            var bit = GetPinBit(pin);
            inputPins = (byte) (isInput
                ? inputPins | bit
                : inputPins & ~bit);
        }

        #endregion

        #region Private Helpers

        private static byte GetPinBit(Pcf8563Pin pin)
        {
            return (byte) (int) pin;
        }

        #endregion
    }
}