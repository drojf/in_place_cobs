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

void test_Encode254NonzeroBytes(void)
{
	uint8_t input_array[NO_ZEROS_TEST_LENGTH];
	for (uint8_t i = 0; i < NO_ZEROS_TEST_LENGTH; i++)
	{
		input_array[i] = i+1;
	}

	uint8_t encoded_array[NO_ZEROS_TEST_LENGTH + 1];

	encode(input_array, NO_ZEROS_TEST_LENGTH, encoded_array);

	// The encoded data should have no 0's
	for (int i = 0; i < NO_ZEROS_TEST_LENGTH + 1; i++)
	{
		TEST_ASSERT_NOT_EQUAL(0, encoded_array[i]);
	}

	// The first element should be equal to 255 (to indicate 254 nonzero bytes ahead)
	TEST_ASSERT_EQUAL_UINT8(255, encoded_array[0]);

	decode_in_place(encoded_array, NO_ZEROS_TEST_LENGTH+1);

	for (int i = 0; i < NO_ZEROS_TEST_LENGTH; i++)
	{
		TEST_ASSERT_EQUAL_UINT8(input_array[i], encoded_array[i+1]);
	}
}

void test_BasicTest(void)
{
	uint8_t test_input_array[6] = { 1,2,0,3,0 };

	uint8_t encoded_array[6+1];

	encode(test_input_array, 6, encoded_array);

	// The encoded data should have no 0's
	for (int i = 0; i < 6; i++)
	{
		TEST_ASSERT_NOT_EQUAL(0, encoded_array[i]);
	}

	decode_in_place(encoded_array, 6+1);

	for (int i = 0; i < 6; i++)
	{
		TEST_ASSERT_EQUAL_UINT8(test_input_array[i], encoded_array[i+1]);
	}
}


int main()
{
	UNITY_BEGIN();
	//RUN_TEST(test_BasicTest);
	RUN_TEST(test_Encode254NonzeroBytes);
	return UNITY_END();
}
