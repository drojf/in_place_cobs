#include "stdint.h"
#include "stdbool.h"

// Returned data starts at data[1] onwards. Returned data will take the indices 1 to data_size-1
// data[0] will always equal 0.
//note: the algoithm may have to be changed if you using uint8_t instead of uint32_t for some values
bool decode_in_place(uint8_t * data, uint32_t data_size)
{
	uint32_t i = 0;
	while (i < data_size)
	{
		// It is an error if any of the input values are zero (are the delimiter)
		if (data[i] == 0) { return false; }

		// Set the current position to 0, and advance i to the next zero location
		uint32_t next_zero_index = data[i] + i;
		data[i] = 0;
		i = next_zero_index;
	}

	// the last marker should always point to index "data_size"
	// if the last marker points anywhere else, it is considered an error
	return i == data_size;
}

// data_size must be less than 254 for this function to work correctly.
void encode(uint8_t * in, uint32_t data_size, uint8_t * out)
{
	uint32_t last_zero_index = 0;
	uint8_t consecutive_nonzero_plus_one = 1;

	for (uint32_t i = 0; i < data_size; i++)
	{
		if (in[i] == 0)
		{
			// save the number of consecutive non-zero seen so far in place of the last '0' seen. This ends the block.
			// reset the consecutive count, and set the new zero location. This starts the block.
			out[last_zero_index] = consecutive_nonzero_plus_one;
			last_zero_index = i + 1;
			consecutive_nonzero_plus_one = 1;
		}
		else
		{
			out[i + 1] = in[i];
			consecutive_nonzero_plus_one += 1;
		}
	}

	// The rightmost zero in the array would not have been filled in at this point, so fill it in.
	out[last_zero_index] = consecutive_nonzero_plus_one;
}