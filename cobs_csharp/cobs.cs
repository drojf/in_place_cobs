using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobs_csharp
{
    // See the comments on the c version of the same functions for documentation
    public class COBS
    {
        // if the cobs packet cannot be decoded, this exception is raised
        public class BadCOBSPacketException : Exception
        {
            public BadCOBSPacketException(string message) : base(message)
            {
            }
        }

        public static byte[] Encode(byte[] input)
        {
            byte[] output = new byte[input.Length + 1];

            int last_zero_index = 0;
            byte consecutive_nonzero_plus_one = 1;
            for(int i = 0; i < input.Length; i++)
            {
                if(input[i] == 0)
                {
                    output[last_zero_index] = consecutive_nonzero_plus_one;
                    last_zero_index = i + 1;
                    consecutive_nonzero_plus_one = 1;
                }
                else
                {
                    output[i + 1] = input[i];
                    consecutive_nonzero_plus_one += 1;
                }
            }

            output[last_zero_index] = consecutive_nonzero_plus_one;

            return output;
        }

        public static byte[] Decode(byte[] input)
        {
            if(input == null || input.Length == 0)
            {
                throw new ArgumentException("'input' must be non-null and be non-zero length");
            }

            byte[] output = new byte[input.Length - 1];
            int next_zero_index = input[0];

            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] == 0)
                {
                    throw new BadCOBSPacketException("A zero was found in the input packet");
                }

                // when you reach the next position where a zero should be written,
                // determine the next zero position, then write the zero into position
                if(i == next_zero_index)
                {
                    next_zero_index = i + input[i];
                    output[i-1] = 0;
                }
                else
                {
                    output[i-1] = input[i];
                }
            }

            //check that the pointer markers were valid
            if(next_zero_index != input.Length)
            {
                throw new BadCOBSPacketException("The next-zero pointers in the packet were invalid!");
            }

            return output;
        }
    }
}
