#ifndef __COBS_IN_PLACE__
#define __COBS_IN_PLACE__

#include "stdint.h"
#include "stdbool.h"

bool decode_in_place(uint8_t * data, uint32_t data_size);
void encode_in_place(uint8_t * data, uint32_t data_size);

#endif