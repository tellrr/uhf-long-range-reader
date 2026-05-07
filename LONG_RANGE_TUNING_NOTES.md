# Long-Range Tuning Notes

Settings that affect read range and reliability, based on the manual and the
live device values read from the connected reader.

---

## 1. TX Power — most impactful

**Command:** `Set Power (0x2F)`
**Default per manual:** 30 dBm
**Live device reading:** `0x1A` = **26 dBm**

The device is running 4 dBm below its own maximum. Because dBm is logarithmic,
4 dBm is roughly 2.5× less transmit power. This is the single biggest lever
for extending read range.

**Recommendation: set power to 30 dBm on startup.**

```text
Command frame: 05 FF 2F 1E LSB MSB   (Pwr = 0x1E = 30)
```

Note: the value is stored in EEPROM and survives power cycles. You only need
to send this once unless the stored value is already correct. Check the
`Power` byte in the `GetReaderInformation` response first; only write if it
differs from 30.

---

## 2. Frequency Band

**Command:** `Set Region (0x22)`
**Live device reading:** `DMaxFre = 0x13`, `DMinFre = 0x40`

Decoded:
- Band: **Chinese band 2**
- Range: **920.125 MHz (N=0) to 924.875 MHz (N=19)**

The supported bands in this firmware are:

| Band code | Name | Formula | Range |
|---|---|---|---|
| 0b0001 | Chinese band 2 | 920.125 + N×0.25 MHz, N∈[0,19] | 920.1–924.9 MHz |
| 0b0010 | US band | 902.75 + N×0.5 MHz, N∈[0,49] | 902.8–927.3 MHz |
| 0b0011 | Korean band | 917.1 + N×0.2 MHz, N∈[0,31] | 917.1–923.3 MHz |
| 0b0000 | User band | 902.6 + N×0.4 MHz, N∈[0,62] | 902.6–927.4 MHz |

**Important:** None of these bands cover the European UHF RFID allocation
(865–868 MHz). This reader hardware appears designed for 900 MHz-class markets
(China, US, Korea). If the deployment is in the EU, operating at 920+ MHz may
be outside the legal band for your country. Verify local regulations before
deploying.

If operating in a 900 MHz-permitted region, the current Chinese band 2 setting
is fine. The US band gives broader frequency hopping (50 channels vs 20) which
can improve reliability through frequency diversity.

---

## 3. Scan Time

**Command:** `Set Scan Time (0x25)`
**Default per manual:** `0x0A` = 1 s
**Live device reading:** `0x3C` = **6 s**
**Range:** 0x03–0xFF (300 ms to 25.5 s)

The current 6 s is already well above the default. A longer scan time means
the reader keeps its RF field on longer per cycle, giving weak/distant tags
more opportunity to respond. The trade-off is lower polling frequency.

For long-range use, 6 s is a reasonable value. If you need faster detection
latency, you can lower it; if tags are still being missed, raising it further
(e.g. 10 s) is worth trying.

---

## 4. Q Value and Session

**Command:** `Set query tags parameter (0x3D)` / inventory payload
**Live device:** `QValue = 4`, `Session = S0`

`QValue` controls the Gen2 anti-collision slot count: `slots = 2^Q`.

| Q | Slots | Best for |
|---|---|---|
| 1–2 | 2–4 | Single tag, fastest |
| 3–4 | 8–16 | Small number of tags |
| 5–8 | 32–256 | Dense tag populations |

For a single-badge/single-card scenario, lowering Q to **2 or 3** reduces the
time the reader spends on empty anti-collision slots, effectively shortening
the time-to-first-read. This compounds with scan time: a lower Q at the same
scan time means more inventory attempts per cycle.

`Session = S0` is correct for fast continuous polling.

---

## Summary — What to Change

| Setting | Current | Recommended | Command |
|---|---|---|---|
| TX Power | 26 dBm | **30 dBm** | `0x2F`, payload `0x1E` |
| Scan time | 6 s | Keep or tune | `0x25` |
| Q value | 4 | **2–3** for single tag | `0x3D` or inventory payload |
| Region/band | Chinese band 2 | Verify legal band for locale | `0x22` |

The power increase to 30 dBm is the only change that is clearly an improvement
with no downside. Everything else is a trade-off.
