using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobs_csharp
{
    class COGS_Serial
    {
        SerialPort port;
        bool has_read_error;
        int position;


        public COGS_Serial(SerialPort port)
        {
            this.port = port;
        }

        public void Write(byte[] packet)
        {
            var encoded_message = COBS.Encode(packet);
            Console.WriteLine("sending encoded message", encoded_message);
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

    class Program
    {
        struct TestCase
        {
            public byte[] input;
            public byte[] encoded;
        };

        static TestCase[] test_cases = new TestCase[]{
            new TestCase
            {
                input   = new byte[]{ 0x00 },
                encoded = new byte[]{ 0x01, 0x01 }
            },
            new TestCase
            {
                input   = new byte[]{ 0x00, 0x00 },
                encoded = new byte[]{ 0x01, 0x01, 0x01 },
            },
            new TestCase
            {
                input   = new byte[]{ 0x11, 0x22, 0x00, 0x33 },
                encoded = new byte[]{ 0x03, 0x11, 0x22, 0x02, 0x33 },
            },
            new TestCase
            {
                input   = new byte[]{ 0x11, 0x22, 0x33, 0x44 },
                encoded = new byte[]{ 0x05, 0x11, 0x22, 0x33, 0x44 },
            },
        };

        static void checkInputOutputSameTest(byte[] data_to_encode, byte[] expected_encoded_data)
        {
            // encode data_to_encode into working_buffer
            byte[] encoded_data = COBS.Encode(data_to_encode);
            
            for(int i = 0; i < encoded_data.Length; i++)
            {
                if(encoded_data[i] != expected_encoded_data[i])
                {
                    throw new Exception("The encoded input does not match the expected encoded data!");
                }
            }

            // decode working_buffer in-place. Data starts at working_buffer[1]
            byte[] decoded_data = COBS.Decode(encoded_data);

            for (int i = 0; i < data_to_encode.Length; i++)
            {
                if (decoded_data[i] != data_to_encode[i])
                {
                    throw new Exception("Decoded output is not the same as input!");
                }
            }
        }

        static void test_Encode254NonzeroBytes()
        {
            byte[] input_array = new byte[254];
            for (byte i = 0; i < 254; i++)
            {
                input_array[i] = (byte)(i + 1);
            }

            var expected_encoded_data = new List<byte>() { 255 };
            expected_encoded_data.AddRange(input_array.ToArray());

            checkInputOutputSameTest(input_array, expected_encoded_data.ToArray());
        }

        static void ReceivedPacketHandler(byte[] received_packet)
        {
            /*Console.WriteLine("Received packet:");
            foreach (byte b in received_packet)
            {
                Console.Write($"{b:x} ");
            }*/

            if(received_packet[0] == 1)
            {
                int strlen = 0;
                foreach(byte b in received_packet.Skip(1))
                {
                    if(b == 0)
                    {
                        break;
                    }
                    else
                    {
                        strlen += 1;
                    }
                }

                string received_string = Encoding.UTF8.GetString(received_packet, 1, strlen);
                Console.Write(received_string);
            }
        }

        static void Main(string[] args)
        {
            foreach(TestCase tc in test_cases)
            {
                checkInputOutputSameTest(tc.input, tc.encoded);
            }

            test_Encode254NonzeroBytes();


            SerialPortManager portManager = new SerialPortManager();
            portManager.OpenSerial("COM4", baudRate: 1000000);
            System.IO.Ports.SerialPort port = portManager.GetSerialPort();

            COGS_Serial cogs_serial = new COGS_Serial(port);

            port.DiscardInBuffer();
            port.DiscardOutBuffer();

            //Console.WriteLine("Sending packet");
            //cogs_serial.Write(new byte[] { 1, 2, 3, 0, 4, 5, 0, 123, 0, 2 });

            Console.WriteLine("Sending packet to start test");
            cogs_serial.Write(new byte[] { 255 });

            while (true)
            {
                //Console.WriteLine("Receiving packet");
                //            byte[] received_packet = new byte[1000];
                //            int bytes_read = port.Read(received_packet, 0, 1000);

                byte[] received_packet = cogs_serial.ReadPacket().Result;
                ReceivedPacketHandler(received_packet);
            }



            Console.ReadKey();
        }
    }
}
