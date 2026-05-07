# Broadcast Address Finding for MCU Update

Supersedes the startup probe flow described in `NEW_READER_FINDINGS_FOR_MCU.md`.

## Finding

The UHFReader188 manual (section 3.1, `Adr` field) states:

> *"Value 255 is broadcasting address. All the readers will respond to the command
> data block with a broadcasting address."*

Address `0xFF` is permanently reserved for broadcast — the reader cannot be
configured to this address. A reader at any configured address (`0x00`–`0xFE`)
will always respond to a frame addressed to `0xFF`.

For a single-reader setup this means there is no need to discover or store the
reader's configured address at all. Every command frame can use `0xFF` directly.

## What to Change in the MCU Program

### Remove

- The two-step startup probe (`0x00` → `0xFF`).
- The `reader_address` variable that stored the discovered address.
- Any logic that reads the address byte from the `GetReaderInformation` response
  and uses it for subsequent commands.

### Replace with

Use `0xFF` as the address byte in every command frame — startup
`GetReaderInformation`, `WriteScanTime`, and all inventory commands.

Before:
```c
uint8_t reader_address = 0x00;  // updated after probe
build_command(reader_address, CMD_GET_READER_INFORMATION, ...);
// ... later ...
build_command(reader_address, CMD_INVENTORY_G2, ...);
```

After:
```c
#define READER_ADDRESS 0xFF
build_command(READER_ADDRESS, CMD_GET_READER_INFORMATION, ...);
// ... later ...
build_command(READER_ADDRESS, CMD_INVENTORY_G2, ...);
```

### Startup flow after the change

1. Send `GetReaderInformation (0x21)` to `0xFF`.
2. Retry on timeout or malformed response (power-up noise).
3. On a valid response, proceed — no address to store.
4. Send all subsequent commands to `0xFF`.

The response frame still carries the reader's real address in its `Adr` byte,
which can be logged for diagnostic purposes, but it no longer drives anything.
