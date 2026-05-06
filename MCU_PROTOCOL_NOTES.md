# UHFReader188 MCU Protocol Notes

This note describes the minimum device behavior needed to reproduce what [read_tags.py](</c:/Python_WORK/_GITHUB_PROJECTS/uhf-long-range-reader/read_tags.py>) currently does, but without any vendor DLL.

## Scope

Current host-side capability:

- Open the reader over UART.
- Read basic reader information.
- Optionally set `scan_time`.
- Repeatedly run EPC inventory.
- Survive power-cycle garbage on the serial line by discarding malformed packets and retrying startup.

Not fully bench-confirmed yet:

- Exact tag record layout when a real tag is present.
- Whether the EPC length byte in inventory data is in bytes or 16-bit words.
- RSSI scaling to dBm. Current code treats it as a raw byte.

## Serial Settings

Verified on the real device connected as `COM1`:

- Baud: `57600`
- Data bits: `8`
- Parity: `none`
- Stop bits: `1`
- Flow control: `none`
- Reader address on tested unit: `0x00`

## Startup Behavior

When the reader is power-cycled, it can emit garbage bytes or partial frames on the UART.

Recommended MCU behavior:

- Open UART.
- Repeatedly send `GetReaderInformation (0x21)` until a valid frame is received.
- Discard any received bytes that do not form a valid frame:
  - invalid length
  - partial frame
  - CRC mismatch
  - valid frame for the wrong command
- Retry startup for a bounded time window.

The current Python reference uses:

- `20` startup retries
- `500 ms` delay between retries

## Frame Format

### Command frame

```text
[LEN] [ADR] [CMD] [DATA...] [CRC_L] [CRC_H]
```

- `LEN = len(DATA) + 4`
- CRC is computed over all bytes from `LEN` through the end of `DATA`
- CRC bytes are little-endian on the wire: `CRC_L`, then `CRC_H`

### Response frame

```text
[LEN] [ADR] [CMD] [STATUS] [DATA...] [CRC_L] [CRC_H]
```

- `LEN = len(DATA) + 5`
- Same CRC rules as above

Observed sane response length range on this device:

- minimum valid response length: `5`
- maximum practical response length used by the current code: `97`

## CRC16

The vendor manual uses:

- preset: `0xFFFF`
- polynomial: `0x8408`

Reference C-style implementation:

```c
uint16_t uhf_crc16(const uint8_t *data, uint8_t len) {
    uint16_t crc = 0xFFFF;
    for (uint8_t i = 0; i < len; ++i) {
        crc ^= data[i];
        for (uint8_t j = 0; j < 8; ++j) {
            if (crc & 0x0001) {
                crc = (crc >> 1) ^ 0x8408;
            } else {
                crc >>= 1;
            }
        }
    }
    return crc;
}
```

## Commands Used

### 1. GetReaderInformation

- Command: `0x21`
- Request payload: none

Request seen on the live device:

```text
04 00 21 D9 6A
```

Live response:

```text
0F 00 21 00 05 01 89 02 13 40 1A 3C 00 00 65 55
```

Decoded fields used by the current program:

- address: `0x00`
- firmware: `05.01`
- reader_type: `0x89`
- tr_type: `0x02`
- dmaxfre: `0x13`
- dminfre: `0x40`
- power_dbm: `0x1A` = `26`
- scan_time: `0x3C` = `60 * 100 ms = 6.0 s`

Notes:

- The live response contains two extra trailing data bytes `00 00`.
- The current program ignores those extra bytes.

### 2. WriteScanTime

- Command: `0x25`
- Request payload: one byte

Payload meaning:

- `scan_time` in units of `100 ms`
- valid range: `3..255`

### 3. EPC Inventory

- Command: `0x01`

Important live-device finding:

- This firmware accepts a `2-byte` inventory payload:
  - `QValue`
  - `Session`
- Earlier guessed `5-byte` variants returned `0xFD` (`command length wrong`)

Verified request:

```text
06 00 01 04 00 AC 36
```

This means:

- `LEN = 0x06`
- `ADR = 0x00`
- `CMD = 0x01`
- `QValue = 0x04`
- `Session = 0x00`

Verified no-tag response:

```text
06 00 01 01 00 14 48
```

Interpretation:

- `CMD = 0x01`
- `STATUS = 0x01`
- `DATA[0] = 0x00` => `card_count = 0`

Important nuance:

- The manual mentions `0xFB` as "no tag in field".
- On this tested firmware, a no-tag EPC inventory also occurs as:
  - `STATUS = 0x01`
  - `card_count = 0`

## Inventory Response Handling

For inventory command `0x01`, the current program treats statuses `0x01`, `0x02`, `0x03`, and `0x04` as inventory data responses.

Behavior:

- `0x03` means inventory data continues in additional frames.
- Accumulate tag data across frames until a final inventory frame with status not equal to `0x03`.
- First data byte is treated as `card_count`.
- Remaining bytes are treated as repeated tag records.

Current tag-record assumption:

```text
[EPC_LEN] [EPC_BYTES...] [RSSI]
```

Remaining uncertainty:

- `EPC_LEN` may be a byte count or a 16-bit word count depending on firmware.
- The current Python reference supports `auto`, `bytes`, and `words`.
- This must be verified with one known tag capture on the bench.

## Status Codes Worth Handling

The MCU implementation should at least handle these:

- `0x00`: success
- `0x01`: inventory completed before scan time expired
- `0x02`: inventory scan time overflow
- `0x03`: inventory data continues in multiple frames
- `0x04`: reader buffer full during inventory
- `0xFB`: no tag in field
- `0xFD`: command length wrong
- `0xFE`: illegal command or CRC error

## Timing Recommendations

- After sending a command, do not immediately send the next one.
- The current host reference waits at least `15 ms` between commands.
- For inventory reads, response timeout should be at least:

```text
scan_time * 100 ms + margin
```

Current host reference uses:

```text
inventory_timeout = scan_time * 0.1 s + 0.5 s
```

With the tested reader configuration:

- `scan_time = 60`
- inventory timeout = `6.5 s`

## Recommended MCU Flow

1. Initialize UART at `57600 8N1`.
2. Repeatedly send `GetReaderInformation (0x21)` until a valid frame is received.
3. Extract reader address and `scan_time`.
4. Optionally send `WriteScanTime (0x25)` if configuration must change.
5. Set inventory receive timeout from `scan_time`.
6. Repeatedly send `Inventory (0x01)` with payload `[QValue, Session]`.
7. Validate frame length and CRC on every response.
8. Ignore malformed power-up noise and retry.
9. Parse `card_count` and tag records.

## Practical Note for the LLM Generating MCU Code

Do not use the Windows DLL as a design dependency.

Use only:

- UART driver
- CRC16
- frame builder/parser
- startup retry logic
- `0x21`, `0x25`, and `0x01` commands

The only protocol item still needing live-tag confirmation is the exact inventory record format when a tag is present.
