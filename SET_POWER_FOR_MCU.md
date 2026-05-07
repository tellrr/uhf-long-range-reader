# Set Maximum TX Power at Startup

The reader ships with power set to **26 dBm** but supports up to **30 dBm**.
Setting it to the maximum is the single most effective step for extending read
range. The value is stored in EEPROM and survives power cycles, so the MCU only
needs to write it when the stored value is not already 30.

---

## Where the Current Power Is Reported

`GetReaderInformation (0x21)` is already sent at startup. Its response data
field contains the current power at **byte index 6** (zero-based, counting from
the first byte after the status byte):

```text
Response data layout:
  [0] Version major
  [1] Version minor
  [2] reader_type
  [3] tr_type
  [4] DMaxFre
  [5] DMinFre
  [6] Power          ← current TX power, range 0–30
  [7] ScanTime
```

Read this byte. If it equals `0x1E` (30), skip the write. Otherwise proceed.

---

## Set Power Command

**Command code:** `0x2F`  
**Payload:** 1 byte — the desired power value (0–30, where 30 = `0x1E`)

Command frame for 30 dBm sent to broadcast address:

```text
05 FF 2F 1E [CRC_L] [CRC_H]
```

Success response:

```text
05 FF 2F 00 [CRC_L] [CRC_H]
```

`Status = 0x00` means the new value was written to EEPROM. Any other status is
an error.

---

## Where to Insert This in the Startup Flow

Place it immediately after processing the `GetReaderInformation` response, before
starting inventory:

```
1. Send GetReaderInformation (0x21) to 0xFF — retry until valid response.
2. Read Power byte (data[6]).
3. If Power != 30:
       Send Set Power (0x2F) with payload 0x1E.
       Verify response status == 0x00.
4. Send WriteScanTime (0x25) if scan time needs adjustment.
5. Begin inventory loop.
```

---

## C Pseudocode

```c
#define MAX_POWER_DBM  0x1E   /* 30 dBm */
#define CMD_SET_POWER  0x2F

/* After a successful GetReaderInformation response: */
uint8_t current_power = reader_info_data[6];

if (current_power != MAX_POWER_DBM) {
    uint8_t payload = MAX_POWER_DBM;
    build_command(READER_ADDRESS, CMD_SET_POWER, &payload, 1, tx_buf);
    uart_send(tx_buf);
    /* wait for response, check status == 0x00 */
}
```
