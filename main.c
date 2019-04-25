#include "stdio.h"
#include "stdint.h"
#include "unity/unity.h"
#include "cobs/cobs.h"

#define NO_ZEROS_TEST_LENGTH 255

void setUp(void)
{
	/* This is run before EACH TEST */
}

void tearDown(void)
{

}

void test_Encode254NonzeroBytes()
{
	uint8_t input_array[NO_ZEROS_TEST_LENGTH];
	for (int i = 0; i < NO_ZEROS_TEST_LENGTH; i++)
	{
		input_array[i] = i;
	}

	encode_in_place(input_array, NO_ZEROS_TEST_LENGTH);

	// The encoded data should have no 0's
	for (int i = 0; i < NO_ZEROS_TEST_LENGTH; i++)
	{
		TEST_ASSERT_NOT_EQUAL(0, input_array[i]);
	}

	// The first element should be equal to 255 (to indicate 254 nonzero bytes ahead)
	TEST_ASSERT_EQUAL_UINT8(255, input_array[0]);

	decode_in_place(input_array, NO_ZEROS_TEST_LENGTH);

	for (int i = 0; i < NO_ZEROS_TEST_LENGTH; i++)
	{
		TEST_ASSERT_EQUAL_UINT8(i, input_array[i]);
	}
}

int main()
{
	UNITY_BEGIN();
	RUN_TEST(test_Encode254NonzeroBytes);
	return UNITY_END();
}
