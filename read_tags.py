from __future__ import annotations

import argparse
import sys
import time
from collections.abc import Callable
from dataclasses import dataclass

try:
    import serial
    from serial import Serial
except ImportError as exc:  # pragma: no cover - runtime environment dependent
    raise SystemExit("This script needs pyserial. Install it with: pip install pyserial") from exc


CMD_INVENTORY_G2 = 0x01
CMD_GET_READER_INFORMATION = 0x21
CMD_WRITE_SCAN_TIME = 0x25

AUTO = "auto"
SUPPORTED_BAUD_RATES = [9600, 19200, 38400, 57600, 115200]
BAUD_PROBE_ORDER = [57600, 115200, 38400, 19200, 9600]
AUTO_ADDRESS_PROBES = [0x00, 0xFF]

PRESET_VALUE = 0xFFFF
POLYNOMIAL = 0x8408
MIN_INTER_COMMAND_DELAY_S = 0.015
MIN_RESPONSE_LENGTH = 5
MAX_RESPONSE_LENGTH = 97

STATUS_TEXT = {
    0x00: "success",
    0x01: "inventory completed before scan time expired",
    0x02: "inventory scan time overflow",
    0x03: "inventory data continues in multiple frames",
    0x04: "reader buffer full during inventory",
    0x05: "access password error",
    0x09: "kill password error or poor communication",
    0x0B: "tag does not support command",
    0x0D: "tag is protected",
    0x13: "parameter save failed",
    0x14: "power cannot be adjusted",
    0xF9: "command execute error",
    0xFA: "poor communication between reader and tag",
    0xFB: "no tag in field",
    0xFC: "tag returned error code",
    0xFD: "command length wrong",
    0xFE: "illegal command or CRC error",
    0xFF: "invalid command parameter",
}


class ProtocolError(RuntimeError):
    pass


@dataclass
class ReaderResponse:
    length: int
    address: int
    command: int
    status: int
    data: bytes
    raw: bytes


@dataclass
class ReaderConnection:
    port_name: str
    baud: int
    address: int
    probe_address: int


@dataclass
class TagRecord:
    epc: str
    epc_length: int
    rssi: int


@dataclass
class InventoryResult:
    card_count: int
    tags: list[TagRecord]


def parse_number(value: str) -> int:
    try:
        return int(value, 0)
    except ValueError:
        return int(value, 16)


def parse_baud(value: str) -> int | str:
    if value.lower() == AUTO:
        return AUTO

    try:
        baud = int(value, 0)
    except ValueError as exc:
        raise argparse.ArgumentTypeError(
            f"baud must be one of {SUPPORTED_BAUD_RATES} or 'auto'"
        ) from exc

    if baud not in SUPPORTED_BAUD_RATES:
        raise argparse.ArgumentTypeError(
            f"baud must be one of {SUPPORTED_BAUD_RATES} or 'auto'"
        )
    return baud


def parse_address(value: str) -> int | str:
    if value.lower() == AUTO:
        return AUTO

    try:
        address = parse_number(value)
    except ValueError as exc:
        raise argparse.ArgumentTypeError(
            "address must be in the range 0x00..0xFF or 'auto'"
        ) from exc

    if not 0x00 <= address <= 0xFF:
        raise argparse.ArgumentTypeError(
            "address must be in the range 0x00..0xFF or 'auto'"
        )
    return address


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Read UHFReader188 tags directly over serial without the vendor DLL."
    )
    parser.add_argument("--port", default="COM1", help="Serial port, default: COM1")
    parser.add_argument(
        "--baud",
        type=parse_baud,
        default=AUTO,
        help="Reader baud rate or 'auto', default: auto",
    )
    parser.add_argument(
        "--address",
        type=parse_address,
        default=AUTO,
        help="Reader address or 'auto', default: auto",
    )
    parser.add_argument(
        "--q",
        type=int,
        default=4,
        choices=range(16),
        help="Inventory Q value, default: 4",
    )
    parser.add_argument(
        "--session",
        type=int,
        choices=range(4),
        default=0,
        help="Gen2 session, default: 0 (S0)",
    )
    parser.add_argument(
        "--scan-time",
        type=int,
        default=None,
        help="Optional reader scan time in units of 100 ms. Valid range: 3..255.",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=0.25,
        help="Serial read timeout in seconds, default: 0.25",
    )
    parser.add_argument(
        "--delay-ms",
        type=int,
        default=50,
        help="Delay between inventory commands in milliseconds, default: 50",
    )
    parser.add_argument(
        "--epc-length-unit",
        choices=["auto", "bytes", "words"],
        default="auto",
        help="How to interpret the EPC length byte in inventory records.",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Print raw request and response frames.",
    )
    parser.add_argument(
        "--once",
        action="store_true",
        help="Run one inventory cycle and exit.",
    )
    parser.add_argument(
        "--startup-retries",
        type=int,
        default=20,
        help="How many times to retry startup commands after the first failure, default: 20",
    )
    parser.add_argument(
        "--startup-retry-delay-ms",
        type=int,
        default=500,
        help="Delay between startup retries in milliseconds, default: 500",
    )
    return parser.parse_args()


def crc16(data: bytes) -> int:
    value = PRESET_VALUE
    for byte in data:
        value ^= byte
        for _ in range(8):
            if value & 0x0001:
                value = (value >> 1) ^ POLYNOMIAL
            else:
                value >>= 1
    return value & 0xFFFF


def build_command(address: int, command: int, payload: bytes = b"") -> bytes:
    body = bytes([len(payload) + 4, address & 0xFF, command & 0xFF]) + payload
    checksum = crc16(body)
    return body + bytes([checksum & 0xFF, (checksum >> 8) & 0xFF])


def read_exact(port: Serial, size: int) -> bytes:
    data = port.read(size)
    if len(data) != size:
        raise TimeoutError(f"Timed out reading {size} bytes from {port.port}.")
    return data


def read_response(
    port: Serial,
    *,
    debug: bool = False,
    discard_noise: bool = False,
    expected_command: int | None = None,
) -> ReaderResponse:
    while True:
        length_bytes = port.read(1)
        if len(length_bytes) != 1:
            raise TimeoutError(f"Timed out reading 1 byte from {port.port}.")

        length = length_bytes[0]
        if length < MIN_RESPONSE_LENGTH or length > MAX_RESPONSE_LENGTH:
            if discard_noise:
                if debug:
                    print(
                        f"DROP invalid response length byte: 0x{length:02X}",
                        file=sys.stderr,
                    )
                continue
            raise ProtocolError(f"Invalid response length byte: 0x{length:02X}")

        body = port.read(length)
        if len(body) != length:
            if discard_noise:
                if debug:
                    print(
                        f"DROP partial frame after length 0x{length:02X}: "
                        f"got {len(body)} of {length} bytes",
                        file=sys.stderr,
                    )
                continue
            raise TimeoutError(f"Timed out reading {length} bytes from {port.port}.")

        frame = bytes([length]) + body
        expected_crc = body[-2] | (body[-1] << 8)
        actual_crc = crc16(frame[:-2])
        if actual_crc != expected_crc:
            if discard_noise:
                if debug:
                    print(
                        f"DROP CRC mismatch: expected 0x{expected_crc:04X}, "
                        f"computed 0x{actual_crc:04X}",
                        file=sys.stderr,
                    )
                continue
            raise ProtocolError(
                f"CRC mismatch: expected 0x{expected_crc:04X}, computed 0x{actual_crc:04X}"
            )

        response = ReaderResponse(
            length=length,
            address=body[0],
            command=body[1],
            status=body[2],
            data=body[3:-2],
            raw=frame,
        )

        if expected_command is not None and response.command != expected_command:
            if discard_noise:
                if debug:
                    print(
                        f"DROP unexpected response command: 0x{response.command:02X} "
                        f"(expected 0x{expected_command:02X})",
                        file=sys.stderr,
                    )
                continue
            raise ProtocolError(
                f"Expected response command 0x{expected_command:02X}, got 0x{response.command:02X}"
            )

        return response


def parse_inventory_records_with_multiplier(data: bytes, multiplier: int) -> list[TagRecord]:
    records: list[TagRecord] = []
    pos = 0

    while pos < len(data):
        epc_length_field = data[pos]
        pos += 1
        epc_length = epc_length_field * multiplier

        if pos + epc_length + 1 > len(data):
            raise ProtocolError("Inventory payload ended before EPC and RSSI were complete.")

        epc_bytes = data[pos : pos + epc_length]
        pos += epc_length

        if pos >= len(data):
            raise ProtocolError("Inventory payload ended before RSSI byte.")

        rssi = data[pos]
        pos += 1

        records.append(
            TagRecord(
                epc=epc_bytes.hex().upper(),
                epc_length=epc_length,
                rssi=rssi,
            )
        )

    return records


def parse_inventory_records(data: bytes, unit: str) -> list[TagRecord]:
    if unit == "bytes":
        return parse_inventory_records_with_multiplier(data, 1)
    if unit == "words":
        return parse_inventory_records_with_multiplier(data, 2)

    errors: list[str] = []
    for name, multiplier in [("bytes", 1), ("words", 2)]:
        try:
            return parse_inventory_records_with_multiplier(data, multiplier)
        except ProtocolError as exc:
            errors.append(f"{name}: {exc}")

    raise ProtocolError("Could not parse inventory payload. " + " | ".join(errors))


def parse_inventory_result(data: bytes, unit: str) -> InventoryResult:
    if not data:
        return InventoryResult(card_count=0, tags=[])

    card_count = data[0]
    tag_bytes = data[1:]
    tags = parse_inventory_records(tag_bytes, unit) if tag_bytes else []
    return InventoryResult(card_count=card_count, tags=tags)


def status_text(status: int) -> str:
    return STATUS_TEXT.get(status, f"unknown status 0x{status:02X}")


def retry_reader_operation(
    label: str,
    operation,
    *,
    retries: int,
    delay_s: float,
    debug: bool,
):
    last_error: Exception | None = None
    total_attempts = retries + 1

    for attempt in range(1, total_attempts + 1):
        try:
            return operation()
        except (TimeoutError, ProtocolError, serial.SerialException) as exc:
            last_error = exc
            if attempt >= total_attempts:
                break
            if debug:
                print(
                    f"RETRY {label} {attempt}/{total_attempts}: {exc}",
                    file=sys.stderr,
                )
            time.sleep(delay_s)

    assert last_error is not None
    raise last_error


def _baud_candidates(baud: int | str) -> list[int]:
    if baud == AUTO:
        return BAUD_PROBE_ORDER
    return [int(baud)]


def _address_candidates(address: int | str) -> list[int]:
    if address == AUTO:
        return AUTO_ADDRESS_PROBES
    return [int(address)]


def _probe_reader_information(
    port_name: str,
    baud: int,
    address: int,
    timeout: float,
    debug: bool,
    serial_factory: Callable[..., Serial],
) -> ReaderResponse:
    with serial_factory(
        port=port_name,
        baudrate=baud,
        bytesize=serial.EIGHTBITS,
        parity=serial.PARITY_NONE,
        stopbits=serial.STOPBITS_ONE,
        timeout=timeout,
        write_timeout=max(timeout, 0.1),
    ) as port:
        port.reset_input_buffer()
        if hasattr(port, "reset_output_buffer"):
            port.reset_output_buffer()

        frame = build_command(address, CMD_GET_READER_INFORMATION)
        if debug:
            print(
                f"PROBE baud={baud} address=0x{address:02X} "
                f"TX {frame.hex(' ').upper()}",
                file=sys.stderr,
            )
        port.write(frame)
        port.flush()
        response = read_response(
            port,
            debug=debug,
            discard_noise=True,
            expected_command=CMD_GET_READER_INFORMATION,
        )
        if debug:
            print(
                f"PROBE baud={baud} address=0x{address:02X} "
                f"RX {response.raw.hex(' ').upper()}",
                file=sys.stderr,
            )
        if response.status != 0x00:
            raise ProtocolError(
                f"GetReaderInformation failed with 0x{response.status:02X} "
                f"({status_text(response.status)})"
            )
        return response


def detect_reader_connection(
    port_name: str,
    *,
    baud: int | str,
    address: int | str,
    timeout: float,
    debug: bool,
    serial_factory: Callable[..., Serial] = serial.Serial,
    probe_rounds: int = 1,
) -> ReaderConnection:
    last_error: Exception | None = None

    for _ in range(max(probe_rounds, 1)):
        for baud_candidate in _baud_candidates(baud):
            for address_candidate in _address_candidates(address):
                try:
                    response = _probe_reader_information(
                        port_name,
                        baud_candidate,
                        address_candidate,
                        timeout,
                        debug,
                        serial_factory,
                    )
                    return ReaderConnection(
                        port_name=port_name,
                        baud=baud_candidate,
                        address=response.address,
                        probe_address=address_candidate,
                    )
                except (TimeoutError, ProtocolError, serial.SerialException) as exc:
                    last_error = exc
                    if debug:
                        print(
                            f"DROP probe baud={baud_candidate} "
                            f"address=0x{address_candidate:02X}: {exc}",
                            file=sys.stderr,
                        )

    if last_error is not None:
        raise last_error
    raise ProtocolError("No reader probe attempts were made.")


class UHFReader188Serial:
    def __init__(
        self, port_name: str, baud: int, address: int, timeout: float, debug: bool = False
    ) -> None:
        self.address = address & 0xFF
        self.debug = debug
        self.port = serial.Serial(
            port=port_name,
            baudrate=baud,
            bytesize=serial.EIGHTBITS,
            parity=serial.PARITY_NONE,
            stopbits=serial.STOPBITS_ONE,
            timeout=timeout,
        )

    def close(self) -> None:
        if self.port.is_open:
            self.port.close()

    def transact(self, command: int, payload: bytes = b"") -> ReaderResponse:
        self.port.reset_input_buffer()
        frame = build_command(self.address, command, payload)
        if self.debug:
            print(f"TX {frame.hex(' ').upper()}")
        self.port.write(frame)
        self.port.flush()
        response = read_response(
            self.port,
            debug=self.debug,
            discard_noise=True,
            expected_command=command,
        )
        if self.debug:
            print(f"RX {response.raw.hex(' ').upper()}")
        time.sleep(MIN_INTER_COMMAND_DELAY_S)
        return response

    def get_reader_information(self) -> ReaderResponse:
        return self.transact(CMD_GET_READER_INFORMATION)

    def write_scan_time(self, scan_time: int) -> ReaderResponse:
        if not 3 <= scan_time <= 255:
            raise ValueError("scan_time must be in the range 3..255")
        return self.transact(CMD_WRITE_SCAN_TIME, bytes([scan_time]))

    def inventory_once(
        self, q_value: int, session: int, epc_length_unit: str
    ) -> tuple[InventoryResult, list[ReaderResponse]]:
        # The live reader accepts EPC inventory as a 2-byte payload: QValue + Session.
        payload = bytes([q_value & 0xFF, session & 0xFF])
        self.port.reset_input_buffer()
        frame = build_command(self.address, CMD_INVENTORY_G2, payload)
        if self.debug:
            print(f"TX {frame.hex(' ').upper()}")
        self.port.write(frame)
        self.port.flush()

        combined_card_count = 0
        tags: list[TagRecord] = []
        responses: list[ReaderResponse] = []

        while True:
            response = read_response(
                self.port,
                debug=self.debug,
                discard_noise=True,
                expected_command=CMD_INVENTORY_G2,
            )
            responses.append(response)
            if self.debug:
                print(f"RX {response.raw.hex(' ').upper()}")

            if response.status in {0x01, 0x02, 0x03, 0x04}:
                result = parse_inventory_result(response.data, epc_length_unit)
                combined_card_count += result.card_count
                tags.extend(result.tags)

            if response.status == 0x03:
                continue

            break

        time.sleep(MIN_INTER_COMMAND_DELAY_S)
        return InventoryResult(card_count=combined_card_count, tags=tags), responses


def print_reader_information(response: ReaderResponse) -> None:
    data = response.data
    if response.status != 0x00:
        raise ProtocolError(
            f"GetReaderInformation failed with 0x{response.status:02X} ({status_text(response.status)})"
        )

    if len(data) >= 8:
        version = f"{data[0]:02d}.{data[1]:02d}"
        reader_type = data[2]
        tr_type = data[3]
        dmaxfre = data[4]
        dminfre = data[5]
        power_dbm = data[6]
        scan_time = data[7]
        print(
            f"Reader info: addr=0x{response.address:02X} firmware={version} "
            f"reader_type=0x{reader_type:02X} tr_type=0x{tr_type:02X} "
            f"power={power_dbm} scan_time={scan_time}*100ms "
            f"dmin=0x{dminfre:02X} dmax=0x{dmaxfre:02X}"
        )
    else:
        print(f"Reader info raw: {data.hex(' ').upper()}")


def get_scan_time_units(response: ReaderResponse) -> int | None:
    if response.status == 0x00 and len(response.data) >= 8:
        return response.data[7]
    return None


def main() -> int:
    args = parse_args()
    startup_retry_delay_s = max(args.startup_retry_delay_ms, 0) / 1000.0

    if args.baud == AUTO or args.address == AUTO:
        connection = detect_reader_connection(
            args.port,
            baud=args.baud,
            address=args.address,
            timeout=args.timeout,
            debug=args.debug,
            probe_rounds=min(max(args.startup_retries, 0) + 1, 3),
        )
        print(
            f"Detected reader: port={connection.port_name} baud={connection.baud} "
            f"address=0x{connection.address:02X} "
            f"(probe address=0x{connection.probe_address:02X})"
        )
    else:
        connection = ReaderConnection(
            port_name=args.port,
            baud=args.baud,
            address=args.address,
            probe_address=args.address,
        )

    reader = UHFReader188Serial(
        connection.port_name,
        connection.baud,
        connection.address,
        args.timeout,
        args.debug,
    )

    try:
        info = retry_reader_operation(
            "GetReaderInformation",
            reader.get_reader_information,
            retries=max(args.startup_retries, 0),
            delay_s=startup_retry_delay_s,
            debug=args.debug,
        )
        print_reader_information(info)
        scan_time_units = get_scan_time_units(info)

        if args.scan_time is not None:
            response = retry_reader_operation(
                "WriteScanTime",
                lambda: reader.write_scan_time(args.scan_time),
                retries=max(args.startup_retries, 0),
                delay_s=startup_retry_delay_s,
                debug=args.debug,
            )
            if response.status != 0x00:
                raise ProtocolError(
                    f"WriteScanTime failed with 0x{response.status:02X} ({status_text(response.status)})"
                )
            print(f"Scan time updated to {args.scan_time}*100ms")
            scan_time_units = args.scan_time

        if scan_time_units is not None:
            reader.port.timeout = max(args.timeout, scan_time_units * 0.1 + 0.5)
            print(f"Inventory timeout set to {reader.port.timeout:.2f}s")
        else:
            reader.port.timeout = max(args.timeout, 1.5)
            print(f"Inventory timeout fallback set to {reader.port.timeout:.2f}s")

        print("Reading continuously over raw serial protocol. Press Ctrl+C to stop.")

        while True:
            try:
                result, responses = reader.inventory_once(
                    args.q, args.session, args.epc_length_unit
                )
            except (TimeoutError, ProtocolError, serial.SerialException) as exc:
                if args.once:
                    raise
                if args.debug:
                    print(f"DROP inventory cycle: {exc}", file=sys.stderr)
                if args.delay_ms > 0:
                    time.sleep(args.delay_ms / 1000.0)
                continue

            final_status = responses[-1].status

            if result.tags:
                timestamp = time.strftime("%H:%M:%S")
                for tag in result.tags:
                    print(
                        f"{timestamp} EPC={tag.epc} RSSI_RAW={tag.rssi} LEN={tag.epc_length}"
                    )
            elif result.card_count == 0 and final_status in {0x01, 0xFB}:
                pass
            elif final_status not in {0xFB}:
                print(
                    f"Inventory status 0x{final_status:02X} ({status_text(final_status)}) "
                    f"card_count={result.card_count} with no tag data",
                    file=sys.stderr,
                )

            if args.delay_ms > 0:
                time.sleep(args.delay_ms / 1000.0)

            if args.once:
                break

    except KeyboardInterrupt:
        print("\nStopping.")
    finally:
        reader.close()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
