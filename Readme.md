# In Place Cobs

## Description

This repository contains an implementation of Consistent Overhead Byte Stuffing (COBS).
It performs an in-place encode/decode, which simplifies the algorithm.

### Quick Explanation of COBS

Given a communication channel where data is not sent as packets, an algorithm is required to overlay a 'packet' model.
Simply transmitting the packet length + packet data will eventually fail if the receiver ever gets out of sync or if the packet length gets corrupted.
Using this method, there is no way to recover without telling the sender to restart communication.

There are various methods for "packet framing":

- ASCII control characters: https://en.wikipedia.org/wiki/Control_character
- https://en.wikipedia.org/wiki/Serial_Line_Internet_Protocol
- COBS, and more...
- Using out-of-band signalling/extra signals to negotiate packets (https://en.wikipedia.org/wiki/RS-232#RTS,_CTS,_and_RTR)
- Using a time delay to delimit packets ('if nothing is sent for 50ms, the packet has ended').

The main advantage of COBS is that it has a consistent overhead of adding one extra byte to your packet (max packet length 254 bytes).
This equates to roughly a .4% overhead for a 255 byte packet, where a basic 'control character' encoding scheme will be inconsistent -
in the best case no bytes are added, and in the worst case, it will double the size of the packet.

The main disadvantage is that the encoder needs to look ahead up to 254 bytes in advance (might be a problem on small microcontrollers).

This explanation is not rigorous, but might help those who aren't familiar in this field understand what COBS does. Please read the below information for more information.

### Information about COBS

- This article on COBS: https://www.embeddedrelated.com/showarticle/113.php
- The original COBS paper: http://conferences.sigcomm.org/sigcomm/1997/papers/p062.pdf
- The COBS wikipedia article: https://en.wikipedia.org/wiki/Consistent_Overhead_Byte_Stuffing

## Usage

```void encode_in_place(uint8_t * data, uint32_t data_size)```

This function encodes a packet of size up to 254 bytes. There are **special requirements** for the data formatting as below:

- The first byte of data (`data[0]`) must not contain your packet data. It will be overwritten by the encoder.
- The bytes from `data[1]` to `data[data_size-1]` should contain your actual data
- `data_size` is the size of all the data, including the first dummy byte. Your actual packet size will be `data_size - 1`

The encoded packet is will not contain any zeros. You should insert/transmit your own `0`'s to delimit packets yourself.

```bool decode_in_place(uint8_t * data, uint32_t data_size)```

This function decodes a packet. After decoding, the output data will start at `data[1]`.
It is assumed that a higher level process will detect `0`'s in the bytestream and chunk those into packets before being fed into this function.
The function returns true on success, false if there appears to be an error. Note that many errors are undetectable with this scheme -
you should use your own method for error detection.

## Implementation

There are various other implementations on Github, but I haven't seen one which does an in-place encode/decode.
I have written my implementation to be easy to understand and analyse, not necessarily for best performance.
I think this implementation is similar to the one described in the embeddedrelated.com article.

## Explanation of COBS

TODO

## Tests

Tests use the Unity framework: http://www.throwtheswitch.org/unity
