#include "stdio.h"
#include "stdint.h"
#include "unity/unity.h"
#include "cobs/cobs.h"

#define NO_ZEROS_TEST_LENGTH 254

void setUp(void)
{
	/* This is run before EACH TEST */
}

void tearDown(void)
{

}

void checkInputOutputSameTest(uint8_t * data_to_encode, uint8_t * expected_encoded_data, uint8_t length)
{
	if (length == 0 || length >= 255)
	{
		printf("Invalid array length: must be between 1 and 254 bytes");
		TEST_ABORT();
	}

	uint8_t working_buffer[255];

	// encode data_to_encode into working_buffer
	encode(data_to_encode, length, working_buffer);

	if (expected_encoded_data != NULL)
	{
		TEST_ASSERT_EQUAL_UINT8_ARRAY_MESSAGE(expected_encoded_data, working_buffer, length + 1, "The encoded input does not match the expected encoded data!");
	}

	// decode working_buffer in-place. Data starts at working_buffer[1]
	decode_in_place(working_buffer, length + 1);

	TEST_ASSERT_EQUAL_UINT8_ARRAY_MESSAGE(data_to_encode, &working_buffer[1], length, "Decoded output is not the same as input!");
}


void test_Encode254NonzeroBytes(void)
{
	uint8_t input_array[NO_ZEROS_TEST_LENGTH];
	for (uint8_t i = 0; i < NO_ZEROS_TEST_LENGTH; i++)
	{
		input_array[i] = i+1;
	}

	checkInputOutputSameTest(input_array, NULL, sizeof(input_array));
}

struct TestCase {
	uint8_t input_length;
	uint8_t input[255];
	uint8_t encoded[255];
};

struct TestCase test_cases[] = {
	{ 
		.input_length = 1,
		.input   = { 0x00 }, 
		.encoded = { 0x01, 0x01 }
	},
	{
		.input_length = 2,
		.input   = { 0x00, 0x00 },
		.encoded = { 0x01, 0x01, 0x01 },
	},
	{
		.input_length = 4,
		.input   = { 0x11, 0x22, 0x00, 0x33 },
		.encoded = { 0x03, 0x11, 0x22, 0x02, 0x33 },
	},
	{
		.input_length = 4,
		.input   = { 0x11, 0x22, 0x33, 0x44 },
		.encoded = { 0x05, 0x11, 0x22, 0x33, 0x44 },
	},
};

void test_Encoded(void)
{
	size_t numTestCases = sizeof(test_cases) / sizeof(test_cases[0]);

	for (size_t tc = 0; tc < numTestCases; tc++)
	{
		checkInputOutputSameTest(test_cases[tc].input, test_cases[tc].encoded, test_cases[tc].input_length);
	}
}

void test_BasicTest(void)
{
	uint8_t test_input_array[6] = { 1,2,0,3,0 };
	checkInputOutputSameTest(test_input_array, NULL, sizeof(test_input_array));
}


int main()
{
	UNITY_BEGIN();
	RUN_TEST(test_BasicTest);
	RUN_TEST(test_Encode254NonzeroBytes);
	RUN_TEST(test_Encoded);
	return UNITY_END();
}
