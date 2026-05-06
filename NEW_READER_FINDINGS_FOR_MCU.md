# New Reader Findings for MCU Update

This note captures the changes needed for firmware that was based on the earlier
`read_tags.py` behavior.

## Main Finding

The new reader does not respond when the host assumes reader address `0x00`.
It responds to `GetReaderInformation (0x21)` when probed with address `0xFF`,
and the valid response reports the actual reader address as `0x01`.

The MCU program should therefore not hard-code `0x00` as the only reader
address. On startup, it should probe and then use the address returned by the
reader.

## Recommended Startup Flow

1. Send `GetReaderInformation (0x21)` to address `0x00`.
2. If no valid response is received, send `GetReaderInformation (0x21)` to
   address `0xFF`.
3. When a valid response is received, store the response address byte as the
   active reader address.
4. Use that active address for all later commands, including inventory.

For the tested new reader, this means startup probing discovers address `0x01`,
then inventory commands must be sent to `0x01`.

## Inventory Behavior

The card is readable with the existing inventory payload:

```text
QValue = 0x04
Session = 0x00
```

The reader can still return a no-tag inventory response for the first few
cycles even when a card is present. The MCU program should keep polling rather
than treating one no-tag response as a device failure.

Observed card from the new reader:

```text
EPC = E200470709A06027B626010B
RSSI_RAW = 200
EPC_LENGTH = 12 bytes
```

## What Not To Add

Do not persistently rewrite the reader address to `0x00` by default. The safer
behavior is to auto-detect and use the configured address.

Do not add any antenna setup based on the vendor DLL test. In the tested DLL
build, `SetAntenna` is a no-op stub and does not send a reader command.
