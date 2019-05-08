using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobs_csharp
{
    class Tests
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

            for (int i = 0; i < encoded_data.Length; i++)
            {
                if (encoded_data[i] != expected_encoded_data[i])
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

        static void RunTestCases()
        {
            foreach (TestCase tc in test_cases)
            {
                checkInputOutputSameTest(tc.input, tc.encoded);
            }

            test_Encode254NonzeroBytes();
        }
    }
}
