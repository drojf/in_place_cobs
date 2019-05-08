using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioUploader
{
    public class AudioFileLoader
    {
        public static List<byte> LoadAsInt16WithSampleRate(string filePath, int newSampleRate)
        {
            List<byte> allAudioData = new List<byte>();

            using (var reader = new AudioFileReader(filePath))
            {
                var resampler = new WdlResamplingSampleProvider(reader, newSampleRate);
                var waveProvider = resampler.ToMono().ToWaveProvider16();

                Console.WriteLine($"Bits Per Sample: {waveProvider.WaveFormat.BitsPerSample}");

                const int bufSize = 100000;

                while (true)
                {
                    byte[] rawData = new byte[bufSize];
                    int bytesRead = waveProvider.Read(rawData, 0, bufSize);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    allAudioData.AddRange(rawData.Take(bytesRead));
                }

                Console.WriteLine($"Read {allAudioData.Count} bytes from {filePath}");
            }

            return allAudioData;
        }
    }

    class BitConverterHelper
    {
        public static byte[] UInt32ToByte(UInt32 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
    }

    class AudioPackerStatus
    {
        public bool success;
        public string errorMessage;
        public List<byte> combinedAudio;
        public int bytesRemaining;

        public AudioPackerStatus(string errorMessage)
        {
            this.success = false;
            this.errorMessage = errorMessage;
            this.combinedAudio = default;
            this.bytesRemaining = default;
        }

        public AudioPackerStatus(List<byte> combinedAudio, int bytesRemaining)
        {
            this.success = true;
            this.errorMessage = "Success";
            this.combinedAudio = combinedAudio;
            this.bytesRemaining = bytesRemaining;
        }
    }

    /// <summary>
    /// Packs many audio files into a single byte array which can be uploaded to the DVM
    /// </summary>
    class AudioPacker
    {
        const int CONTAINER_MAX_HEADER_SIZE = 256;
        const int CONTAINER_AUDIO_DATA_START = 256;

        class AudioFilePointer
        {
            int ptrStartBytes; //the number of samples in the audio file
            int audioLengthBytes; //absolute address where the audio file starts

            public AudioFilePointer(int ptrStartBytes, int audioLengthBytes)
            {
                this.ptrStartBytes = ptrStartBytes;
                this.audioLengthBytes = audioLengthBytes;
            }

            public IEnumerable<byte> ToBytes()
            {
                return BitConverterHelper.UInt32ToByte((UInt32)ptrStartBytes)
                    .Concat(BitConverterHelper.UInt32ToByte((UInt32)audioLengthBytes));
            }
        }

        int maxContainerSize;
        
        public AudioPackerStatus Combine(List<List<byte>> audioFiles)
        {
            // Calculate the audio offsets and lengths
            List<AudioFilePointer> audioFilePointers = new List<AudioFilePointer>();
            int currentAudioStartBytes = CONTAINER_AUDIO_DATA_START;
            foreach(List<byte> audioData in audioFiles)
            {
                audioFilePointers.Add(new AudioFilePointer(currentAudioStartBytes, audioData.Count));
                Console.WriteLine($"Added audio starting at {currentAudioStartBytes} and of length {audioData.Count}");
                currentAudioStartBytes += audioData.Count;
            }

            List<byte> packedAudioContainer = new List<byte>();

            // Construct header
            packedAudioContainer.AddRange(BitConverterHelper.UInt32ToByte((UInt16)audioFiles.Count));
            foreach(AudioFilePointer fp in audioFilePointers)
            {
                var pterBytes = fp.ToBytes();
                packedAudioContainer.AddRange(pterBytes);
            }

            // Check the header is less than 256 bytes
            if (packedAudioContainer.Count > CONTAINER_MAX_HEADER_SIZE)
            {
                return new AudioPackerStatus($"Header is too large - is {packedAudioContainer.Count}, must be less than {CONTAINER_MAX_HEADER_SIZE}. Reduce number of audio files!");
            }

            // Pad header to 256 bytes
            packedAudioContainer.AddRange(new byte[CONTAINER_MAX_HEADER_SIZE-packedAudioContainer.Count]);

            // Append audio files 
            foreach (List<byte> audioFile in audioFiles)
            {
                packedAudioContainer.AddRange(audioFile);
            }
            
            // Check audio container fits in memory
            if(packedAudioContainer.Count > maxContainerSize)
            {
                return new AudioPackerStatus($"Audio container is too large - is {packedAudioContainer.Count}, must be less than {maxContainerSize}. Reduce number or length of audio files!");
            }

            return new AudioPackerStatus(packedAudioContainer, maxContainerSize - packedAudioContainer.Count);
        }

        public AudioPacker(int maxFileSize)
        {
            this.maxContainerSize = maxFileSize;
        }
    }
}
