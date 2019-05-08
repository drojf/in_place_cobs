using cobs_csharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioUploader
{
    enum DVMPacketTypes
    {
        DVM_PACKET__ID = 0,
        DVM_PACKET__TEXT_MESSAGE = 1,
        DVM_PACKET__BEGIN_PLAYBACK = 50,
        DVM_PACKET__FLASH_READ = 100,
        DVM_PACKET__FLASH_WRITE = 101,
        DVM_PACKET__FLASH_CHIP_ERASE = 110,
        DVM_PACKET__VOLUME = 200,
        DVM_PACKET__DO_FLASH_TEST = 255,
    };

    enum DVMStatusTypes
    {
        DVM_STATUS_OK = 0,
        DVM_STATUS_ERROR = 1,
    };

    class DVMPacket
    {
        //byte[] crc
        public readonly DVMPacketTypes type;
        public readonly byte[] payload;

        public DVMPacket(DVMPacketTypes type, byte[] payload)
        {
            this.type = type;
            this.payload = payload;
        }
    }

    class Program
    {
        const int APP_HEADER_SIZE = 5;
        const int APP_HEADER_CRC_OFFSET = 0;
        const int APP_HEADER_COMMAND_OFFSET = 4;
        const int APP_PAYLOAD_START_ADDR = APP_HEADER_SIZE; //the payload starts immediately after the header

        const int MAX_COGS_PACKET_SIZE = 254;
        const int MAX_APP_PACKET_SIZE = 254 - APP_HEADER_SIZE;

        static void HandleIDPacket(byte[] packet)
        {
            Console.WriteLine("Got ID packet:");
            for(int i = 0; i < packet.Length; i++)
            {
                Console.WriteLine($"{packet[i]:x}");
            }
        }

        static void HandleTextPacket(byte[] packet)
        {
            int strlen = 0;
            foreach (byte b in packet)
            {
                if (b == 0)
                {
                    break;
                }
                else
                {
                    strlen += 1;
                }
            }

            string received_string = Encoding.UTF8.GetString(packet);
            Console.Write(received_string);
        }

        static void HandleReadFlashPacket(byte[] packet)
        {
            /*Console.WriteLine("Got flash readout:");
            foreach (byte b in packet)
            {
                Console.WriteLine(b);
            }*/
        }

        static DVMPacket AppDecodePacket(byte[] receivedPacket)
        {
            //byte[] packetCRC = received_packet[0 to 4];
            //TODO: do some correctness check on the packet type earlier on than this
            DVMPacketTypes packetType = (DVMPacketTypes) receivedPacket[APP_HEADER_COMMAND_OFFSET];
            byte[] payload = new byte[receivedPacket.Length - APP_HEADER_SIZE]; //payload is assumed to be rest of packet without header
            //Copy the payload from the received packet into 'payload'
            Array.Copy(receivedPacket, APP_PAYLOAD_START_ADDR, payload, 0, payload.Length);
            return new DVMPacket(packetType, payload);
        }

        static void ReceivedPacketHandler(DVMPacket packet)
        {
            switch (packet.type)
            {
                case DVMPacketTypes.DVM_PACKET__ID:
                    HandleIDPacket(packet.payload);
                    break;

                case DVMPacketTypes.DVM_PACKET__TEXT_MESSAGE:
                    HandleTextPacket(packet.payload);
                    break;

                case DVMPacketTypes.DVM_PACKET__FLASH_READ:
                    HandleReadFlashPacket(packet.payload);
                    break;

                case DVMPacketTypes.DVM_PACKET__FLASH_CHIP_ERASE:
                    Console.WriteLine("Got chip erase confirmation");
                    break;

                case DVMPacketTypes.DVM_PACKET__FLASH_WRITE:
                    Console.WriteLine("Got flash write confirmation");
                    break;

                case DVMPacketTypes.DVM_PACKET__VOLUME:
                    Console.WriteLine($"New Volume: {packet.payload[0]}");
                    break;

                case DVMPacketTypes.DVM_PACKET__BEGIN_PLAYBACK:
                    Console.WriteLine($"Got begin playback confirmation");
                    break;

                default:
                    Console.WriteLine($"Unknown packet type {packet.type} received! Ignoring.");
                    break;
            }
        }

        static void SendPacket(COGSOverSerial cogs_serial, DVMPacketTypes packetType, byte[] data)
        {
            //TODO: remove hardcoded '254' with proper #define
            if (data.Length > (254 - APP_HEADER_SIZE))
            {
                throw new Exception("Data is too large!");
            }

            byte[] DVM_packing_buffer = new byte[APP_HEADER_SIZE + data.Length];
            //DVM_packing_buffer[0 to 3] = crc
            DVM_packing_buffer[4] = (byte) packetType;

            for (int i = 0; i < data.Length; i++)
            {
                DVM_packing_buffer[i + APP_PAYLOAD_START_ADDR] = data[i];
            }

            cogs_serial.Write(DVM_packing_buffer);
        }

        static List<byte> Int32AsLittleEndianBytes(UInt32 val)
        {
            return new List<byte> {
                (byte)(val & 0xFF),
                (byte)((val >> 8) & 0xFF),
                (byte)((val >> 16) & 0xFF),
                (byte)((val >> 24) & 0xFF),
               };
        }

        static void ReadFlashAddress(COGSOverSerial cogs_serial, UInt32 address)
        {
            SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__FLASH_READ, Int32AsLittleEndianBytes(address).ToArray());
        }

        static void WriteFlashAddress(COGSOverSerial cogs_serial, UInt32 address, IEnumerable<byte> dataToWrite)
        {
            byte[] dataToSend = Int32AsLittleEndianBytes(address).Concat(dataToWrite).ToArray();
            SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__FLASH_WRITE, dataToSend);
        }

        static void RequestChipErase(COGSOverSerial cogs_serial)
        {
            SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__FLASH_CHIP_ERASE, new byte[] { });
        }

        static async Task<DVMPacket> ReceiveAndWaitForExpectedPacketType(COGSOverSerial cogs_serial, DVMPacketTypes expectedPacketType)
        {
            DVMPacket app_packet;
            while (true)
            {
                byte[] raw_cogs_packet = await cogs_serial.ReadPacket();
                app_packet = AppDecodePacket(raw_cogs_packet);
                ReceivedPacketHandler(app_packet);
                if(app_packet.type == expectedPacketType)
                {
                    break;
                }
            }

            return app_packet;
        }

        static async void ReadBackAndVerify(COGSOverSerial cogs_serial, byte[] audioDataPadded)
        {
            //Read back data and verify
            {
                Console.WriteLine($"Begin Read Back");
                int debug_next_read_address = 1000;

                int readAddress = 0;
                while (readAddress != audioDataPadded.Length)
                {
                    if (readAddress > debug_next_read_address)
                    {
                        Console.WriteLine($"Reading address {readAddress}");
                        debug_next_read_address += 100000;
                    }
                    //write 50 bytes, or the remaining amount if less
                    int amountToVerify = Math.Min(50, audioDataPadded.Length - readAddress);

                    ReadFlashAddress(cogs_serial, (UInt32)readAddress);
                    DVMPacket packet = await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__FLASH_READ);

                    for (int i = 0; i < amountToVerify; i++)
                    {
                        //for now, just hardcode CRC offset of 4
                        if (packet.payload[i] != audioDataPadded[i + readAddress])
                        {
                            throw new Exception("Read back values do not match");
                        }
                    }

                    readAddress += amountToVerify;
                }
            }
        }

        static async Task WriteAudio(COGSOverSerial cogs_serial, byte[] audioDataPadded)
        {
             {
                 Console.WriteLine("Erasing Flash...");
                 RequestChipErase(cogs_serial);
                 await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__FLASH_CHIP_ERASE);
             }

             Console.WriteLine("Begin flash write...");
             //copy the audio data
             int writeAddress = 0;
             while(writeAddress != audioDataPadded.Length)
             {
                 Console.WriteLine($"Writing to address {writeAddress}");
                 //write 50 bytes, or the remaining amount if less
                 int amountToWrite = Math.Min(50, audioDataPadded.Length - writeAddress);

                 byte[] data = new byte[amountToWrite];
                 Array.Copy(audioDataPadded, writeAddress, data, 0, amountToWrite);
                 WriteFlashAddress(cogs_serial, (UInt32)(writeAddress), data);
                 await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__FLASH_WRITE);

                 writeAddress += amountToWrite;
             }
        }

        static void VolumeUp(COGSOverSerial cogs_serial)
        {
            SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__VOLUME, new byte[] { 0,1,2,3,4,5,6,7,8,9 });
        }

        static void VolumeDown(COGSOverSerial cogs_serial)
        {
            SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__VOLUME, new byte[10] );
        }

        static void BeginPlayback(COGSOverSerial cogs_serial)
        {
            SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__BEGIN_PLAYBACK, new byte[0]);
        }

        static async Task<int> Main(string[] args)
        {
            const int resampleRate = 32000;

            List<string> audioFilePathsToLoad = new List<string>()
            {
                "Door Close Mono 16 Gain.wav",
                "door_close.wav",
                "doorbell-1.wav",
            };

            List<List<byte>> audioFiles = audioFilePathsToLoad.Select(path => AudioFileLoader.LoadAsInt16WithSampleRate(path, resampleRate)).ToList();

            //max flash size is 4 mebibyte or 1 << 22 bytes
            AudioPacker audioPacker = new AudioPacker(1 << 22);
            AudioPackerStatus ret = audioPacker.Combine(audioFiles);

            if(ret.success)
            {
                Console.WriteLine($"Generated container {ret.combinedAudio.Count}, with {ret.bytesRemaining} bytes spare");
            }
            else
            {
                Console.WriteLine($"Error making container: {ret.errorMessage}");
            }

            File.WriteAllBytes("test_container.container", ret.combinedAudio.ToArray());

            //Console.WriteLine("Finished Program");
            //Console.ReadKey();
            //return -1;


            int sampleRate = 32000;
            //byte[] audioData = File.ReadAllBytes("dcmono48.raw");
            byte[] audioData = ret.combinedAudio.ToArray();

            int audioLengthSamples = audioData.Length / 2;
            float audioLengthSeconds = audioLengthSamples / (float) sampleRate;

            Console.WriteLine($"Input audio file is {audioLengthSamples} samples ({audioLengthSeconds} secs)");

            //let header be the first page (first 256 bytes) of the flash
            //first 32 bits (4 bytes) should be a CRC32 of the audio header and data (excluding the checksum itself)
            //followed by the number of audio files in this audio data (2 bytes)
            //followed by 16 bit pointers (2 bytes) to the audio start addresses. Allocate 32 of these for now (64 bytes total)
            //so 4 byte CRC32 + 2 byte audio file count + 64 bytes audio start addresses
            //
            // Every packet sent over usb should use the following format:
            // CRC32(4 bytes)
            // command (1 byte)
            
            //For now, send 128 bytes at a time in a cogs packet. Send as:
            //WRITE_PROG command (1 byte)
            //CRC32 (4 bytes)
            //Write Address (4 bytes)
            //Data (128 bytes?)
            //
            //Must wait for chip to reply back to avoid overfilling chip's internal buffer
            //If no message received for some time, resend packet
            //If incorrect sequence number, then just fail immediately?

            // NOTE: need to handle if packet is == 64 bytes will be transmitted in several receives? or doesn't work at all?


            SerialPortManager portManager = new SerialPortManager();
            portManager.OpenSerial("COM4", baudRate: 1000000);
            System.IO.Ports.SerialPort port = portManager.GetSerialPort();

            COGSOverSerial cogs_serial = new COGSOverSerial(port, discardBuffers: true);

            Console.WriteLine("Sending packet to start test");

            /*for(int packet_size_to_test = 57; packet_size_to_test <= MAX_APP_PACKET_SIZE; packet_size_to_test++)
            {
                byte[] bufferToSend = new byte[packet_size_to_test];
                for(int i = 0; i < packet_size_to_test; i++)
                {
                    bufferToSend[i] = (byte)i;
                }

                Console.WriteLine($"Sending {bufferToSend.Length} payload");
                SendPacket(cogs_serial, DVMPacketTypes.DVM_PACKET__ID, bufferToSend);
                Console.WriteLine("Waiting to receive message");
                byte[] raw_cogs_packet = await cogs_serial.ReadPacket();
                DVMPacket app_packet = AppDecodePacket(raw_cogs_packet);

                for (int i = 0; i < packet_size_to_test; i++)
                {
                    if(bufferToSend[i] != app_packet.payload[i])
                    {
                        throw new Exception("Buffers don't match!");
                    }
                }
                Console.WriteLine($"Got Raw packet length {raw_cogs_packet.Length} Payload Length: {app_packet.payload.Length}");

                ReceivedPacketHandler(app_packet);
            }*/

            //pad the audio data to 256 byte pages (TODO: this algorithm will add an extra 256 bytes if the audio is already length 256...)
            byte[] audioDataPadded = new byte[(audioData.Length / 256 + 1) * 256];
            Array.Copy(audioData, audioDataPadded, audioData.Length);


            //TODO: Volume up/down

            while(true)
            {
                //await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__VOLUME);

                var k = Console.ReadKey();

                if(!port.IsOpen)
                {
                    port.Open();
                }

                if(k.Key == ConsoleKey.U)
                {
                    VolumeUp(cogs_serial);
                    await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__VOLUME);
                }
                else if(k.Key == ConsoleKey.D)
                {
                    VolumeDown(cogs_serial);
                    await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__VOLUME);
                }
                else if(k.Key == ConsoleKey.W)
                {
                    Console.WriteLine("Writing flash");
                    await WriteAudio(cogs_serial, audioDataPadded);
                }
                else if(k.Key == ConsoleKey.P)
                {
                    Console.WriteLine("Beginning Playback");
                    BeginPlayback(cogs_serial);
                    await ReceiveAndWaitForExpectedPacketType(cogs_serial, DVMPacketTypes.DVM_PACKET__BEGIN_PLAYBACK);
                }
            }

            /*Console.WriteLine("Writing flash");
            await WriteAudio(cogs_serial, audioDataPadded);
            */

            Console.WriteLine("Finished Program");
            Console.ReadKey();
            return 0;
        }
    }
}
