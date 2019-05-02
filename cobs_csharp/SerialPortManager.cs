using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobs_csharp
{
    public class SerialPortManager : IDisposable
    {
        private SerialPort ser;

        public SerialPortManager()
        {
            ser = new SerialPort();
        }

        public SerialPort GetSerialPort()
        {
            return ser;
        }

        public bool IsOpen()
        {
            return ser.IsOpen;
        }

        //Note: trying to open a port which was unplugged a short time (< 5s ?) ago may result in an UnauthorizedAccessException
        //      To prevent this, stop the user from re-connecting so quickly, and close the port, even if it is not open.
        public virtual bool OpenSerial(string portName, bool forceOpen = false, int baudRate = 9600, Parity parity = Parity.Even)
        {
            if (ser.IsOpen)
            {
                //if port already open and don't want to force it open, just return true
                if (forceOpen)
                    ser.Close();
                else
                    return true;
            }

            ser.BaudRate = baudRate;
            ser.Parity = parity;
            ser.DataBits = 8;
            ser.StopBits = StopBits.One;
            ser.ReadTimeout = 300; // 5500;
            ser.WriteTimeout = 300;

            ser.PortName = portName;
            LogDebug("Connecting to service terminal on " + ser.PortName);
            LogDebug($"Baudrate: {ser.BaudRate} Parity: {ser.Parity.ToString()} Stopbits: {ser.StopBits}");

            try
            {
                ser.Open();
            }
            catch (UnauthorizedAccessException)
            {
                LogError($"Another program is using {ser.PortName}. Please close it before running this program.");
                ser.Close();
            }
            catch (System.IO.IOException e)
            {
                LogError(e.ToString());
            }

            if (!ser.IsOpen)
            {
                LogError("Couldn't open to service terminal port");
            }
            else
            {
                LogDebug("Sucessfully opened service terminal port");
            }

            //return OK if serial port was opened
            return ser.IsOpen;
        }

        public void CloseSerial()
        {
            LogDebug("Serial Port Closed");
            ser.Close();
        }

        public void Dispose()
        {
            ser.Close();
        }

        //Fill in with your own logging solution
        private void LogDebug(string s)
        {
            Console.WriteLine(s);
        }

        private void LogError(string s)
        {
            Console.WriteLine(s);
        }
    }
}
