import argparse
import unittest

import read_tags


class FakeSerial:
    def __init__(self, factory, **kwargs):
        self.factory = factory
        self.port = kwargs["port"]
        self.baudrate = kwargs["baudrate"]
        self.timeout = kwargs["timeout"]
        self.write_timeout = kwargs.get("write_timeout")
        self.is_open = True
        self._read_buffer = b""
        self.writes = []

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, traceback):
        self.close()

    def reset_input_buffer(self):
        self._read_buffer = b""

    def reset_output_buffer(self):
        pass

    def write(self, data):
        frame = bytes(data)
        self.writes.append(frame)
        self.factory.writes.append((self.baudrate, frame))
        self._read_buffer = self.factory.responses.get((self.baudrate, frame), b"")
        return len(frame)

    def flush(self):
        pass

    def read(self, size):
        data = self._read_buffer[:size]
        self._read_buffer = self._read_buffer[size:]
        return data

    def close(self):
        self.is_open = False


class FakeSerialFactory:
    def __init__(self, responses):
        self.responses = responses
        self.instances = []
        self.writes = []

    def __call__(self, **kwargs):
        instance = FakeSerial(self, **kwargs)
        self.instances.append(instance)
        return instance


class ReaderAutoDetectionTests(unittest.TestCase):
    def test_parse_baud_accepts_auto_or_supported_numeric_rate(self):
        self.assertEqual(read_tags.parse_baud("auto"), "auto")
        self.assertEqual(read_tags.parse_baud("57600"), 57600)
        with self.assertRaises(argparse.ArgumentTypeError):
            read_tags.parse_baud("12345")

    def test_parse_address_accepts_auto_or_number(self):
        self.assertEqual(read_tags.parse_address("auto"), "auto")
        self.assertEqual(read_tags.parse_address("0x01"), 0x01)
        self.assertEqual(read_tags.parse_address("FF"), 0xFF)

        with self.assertRaises(argparse.ArgumentTypeError):
            read_tags.parse_address("0x100")

    def test_auto_detect_uses_broadcast_probe_response_address(self):
        response = bytes.fromhex(
            "0F 01 21 00 05 01 89 02 4E 00 1A 3C 00 00 54 E4"
        )
        factory = FakeSerialFactory(
            {
                (
                    57600,
                    read_tags.build_command(0xFF, read_tags.CMD_GET_READER_INFORMATION),
                ): response
            }
        )

        detected = read_tags.detect_reader_connection(
            "COM1",
            baud="auto",
            address="auto",
            timeout=0.1,
            debug=False,
            serial_factory=factory,
        )

        self.assertEqual(detected.port_name, "COM1")
        self.assertEqual(detected.baud, 57600)
        self.assertEqual(detected.address, 0x01)
        self.assertEqual(detected.probe_address, 0xFF)
        self.assertEqual(
            factory.writes[:2],
            [
                (
                    57600,
                    read_tags.build_command(
                        0x00, read_tags.CMD_GET_READER_INFORMATION
                    ),
                ),
                (
                    57600,
                    read_tags.build_command(
                        0xFF, read_tags.CMD_GET_READER_INFORMATION
                    ),
                ),
            ],
        )


if __name__ == "__main__":
    unittest.main()
