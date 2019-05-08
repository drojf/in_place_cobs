using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace cobs_csharp
{
    public class COGSOverSerial
    {
        SerialPort port;
        bool has_read_error;
        int position;

        public COGSOverSerial(SerialPort port, bool discardBuffers)
        {
            this.port = port;

            if(discardBuffers)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }
        }

        public void Write(byte[] packet)
        {
            var encoded_message = COBS.Encode(packet);
            //Console.WriteLine("sending encoded message", encoded_message);
            //append zero to indicate end of packet
            var packetWithEndMarker = new List<byte>(encoded_message);
            packetWithEndMarker.Add(0);

            port.Write(packetWithEndMarker.ToArray(), 0, packetWithEndMarker.Count);
        }

        public async Task<byte[]> ReadPacket()
        {
            List<byte> allBytes = new List<byte>();

            while(true)
            {
                byte[] recvBuf = new byte[1];
                int num_bytes_received = await port.BaseStream.ReadAsync(recvBuf, 0, 1);
                //Console.WriteLine($"Got raw byte {num_bytes_received}");

                if (num_bytes_received != 1)
                {
                    throw new Exception("invalid num bytes received");
                }

                if (allBytes.Count <= 255)
                {
                    if (recvBuf[0] != 0)
                    {
                        allBytes.Add(recvBuf[0]);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    //If too many bytes are received, then continue to receive bytes, but don't store them.
                    //Once the zero is reached, throw exception indicating too many bytes in packet
                    if (recvBuf[0] == 0)
                    {
                        throw new Exception("Too many bytes in packet!");
                    }
                }
            }

            return COBS.Decode(allBytes.ToArray());
        }
    }
}
