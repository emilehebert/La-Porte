#!/usr/bin/env python3
"""
ps1_crush.py — fait sonner des SFX comme sur PlayStation 1.

Il n'existait pas de « bitcrusher PS1 » chez les studios : le son vient de la chaîne
matérielle par laquelle passait chaque son. Le script refait cette chaîne :

  1. Mono (la plupart des SFX PS1 étaient mono ; la réverb ajoute la stéréo).
  2. Aigus remontés avant conversion (--aigus, en dB) pour compenser l'étouffement
     de l'interpolation de la console.
  3. Baisse de la fréquence d'échantillonnage (22050 Hz par défaut, 11025 = plus sale).
     --resample brut : décimation sans filtre, l'aliasing donne le grain métallique
     (défaut) ; lineaire : un peu moins sale ; propre : filtre sinc, aucun aliasing.
  4. Encodage en ADPCM 4 bits du SPU (format VAG : blocs de 28 échantillons,
     5 filtres de prédiction, décalage 0-12), puis décodage.
  5. Relecture par le SPU à 44100 Hz : compteur de pitch 4.12 bits et interpolation
     gaussienne à 4 points avec la table exacte de la console.
  6. Réverb du SPU en option (--reverb), avec l'algorithme et les réglages d'usine
     de la console (Room, Studio, Hall, Space Echo...), calculée à 22050 Hz
     avec le filtre FIR à 39 points de la console.

Sources : psx-spx (https://psx-spx.consoledev.net/soundprocessingunitspu/).

Aucun module à installer : Python standard seulement. Lit les WAV (PCM 8/16/24/32 bits
et float 32/64). Les originaux ne sont jamais modifiés : tout part dans un dossier à côté.

Utilisation :
    python ps1_crush.py "C:\\chemin\\vers\\sfx"
    python ps1_crush.py "C:\\chemin\\vers\\sfx" --rate 11025
    python ps1_crush.py "C:\\chemin\\vers\\son.wav" --reverb hall --wet 0.4
"""

import argparse
import math
import os
import struct
import sys

# ---------------------------------------------------------------- lecture / écriture WAV

def read_wav(path):
    with open(path, "rb") as f:
        data = f.read()
    if data[:4] != b"RIFF" or data[8:12] != b"WAVE":
        raise ValueError("pas un fichier WAV RIFF")
    pos, fmt, raw = 12, None, None
    while pos + 8 <= len(data):
        cid, size = data[pos:pos + 4], struct.unpack("<I", data[pos + 4:pos + 8])[0]
        body = data[pos + 8:pos + 8 + size]
        if cid == b"fmt ":
            tag, ch, rate, _, _, bits = struct.unpack("<HHIIHH", body[:16])
            if tag == 0xFFFE and len(body) >= 26:  # WAVE_FORMAT_EXTENSIBLE
                tag = struct.unpack("<H", body[24:26])[0]
            fmt = (tag, ch, rate, bits)
        elif cid == b"data":
            raw = body
        pos += 8 + size + (size & 1)
    if fmt is None or raw is None:
        raise ValueError("chunk fmt ou data manquant")
    tag, ch, rate, bits = fmt
    width = bits // 8
    n = len(raw) // (width * ch)
    total = n * ch
    if tag == 3:  # float
        code = "f" if bits == 32 else "d"
        vals = list(struct.unpack("<%d%s" % (total, code), raw[:total * width]))
    elif tag == 1:
        if bits == 8:
            vals = [(b - 128) / 128.0 for b in raw[:total]]
        elif bits == 16:
            vals = [v / 32768.0 for v in struct.unpack("<%dh" % total, raw[:total * 2])]
        elif bits == 24:
            vals = []
            for i in range(0, total * 3, 3):
                v = raw[i] | (raw[i + 1] << 8) | (raw[i + 2] << 16)
                if v & 0x800000:
                    v -= 0x1000000
                vals.append(v / 8388608.0)
        elif bits == 32:
            vals = [v / 2147483648.0 for v in struct.unpack("<%di" % total, raw[:total * 4])]
        else:
            raise ValueError("profondeur %d bits non gérée" % bits)
    else:
        raise ValueError("format WAV non géré (code %d, probablement compressé)" % tag)
    channels = [vals[c::ch] for c in range(ch)]
    return channels, rate


def write_wav16(path, channels, rate):
    """channels : listes d'échantillons à l'échelle 16 bits (-32768..32767)."""
    ch, n = len(channels), len(channels[0])
    inter = [0] * (n * ch)
    for c, samples in enumerate(channels):
        for i, s in enumerate(samples):
            inter[i * ch + c] = max(-32768, min(32767, int(round(s))))
    raw = struct.pack("<%dh" % len(inter), *inter)
    hdr = b"RIFF" + struct.pack("<I", 36 + len(raw)) + b"WAVE"
    hdr += b"fmt " + struct.pack("<IHHIIHH", 16, 1, ch, rate, rate * ch * 2, ch * 2, 16)
    hdr += b"data" + struct.pack("<I", len(raw))
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "wb") as f:
        f.write(hdr + raw)

# ---------------------------------------------------------------- préparation

def high_shelf(x, rate, gain_db, freq=3000.0):
    """Étagère d'aigus (biquad RBJ) : remonte tout ce qui est au-dessus de freq."""
    if gain_db == 0:
        return list(x)
    A = 10 ** (gain_db / 40.0)
    w0 = 2 * math.pi * freq / rate
    cw, sw = math.cos(w0), math.sin(w0)
    alpha = sw / 2 * math.sqrt(2)  # pente S = 1
    sa = 2 * math.sqrt(A) * alpha
    b0 = A * ((A + 1) + (A - 1) * cw + sa)
    b1 = -2 * A * ((A - 1) + (A + 1) * cw)
    b2 = A * ((A + 1) + (A - 1) * cw - sa)
    a0 = (A + 1) - (A - 1) * cw + sa
    a1 = 2 * ((A - 1) - (A + 1) * cw)
    a2 = (A + 1) - (A - 1) * cw - sa
    b0, b1, b2, a1, a2 = b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0
    out = [0.0] * len(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        y = b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1, y2, y1 = x1, v, y1, y
        out[i] = y
    return out


def resample(x, src, dst, mode):
    if src == dst:
        return list(x)
    step = src / dst
    n_out = int(len(x) / step)
    n = len(x)
    if mode == "brut":  # décimation sans filtre : l'aliasing reste
        return [x[min(n - 1, int(j * step))] for j in range(n_out)]
    if mode == "lineaire":
        out = []
        for j in range(n_out):
            t = j * step
            i = int(t)
            f = t - i
            a = x[i]
            b = x[i + 1] if i + 1 < n else a
            out.append(a + (b - a) * f)
        return out
    # propre : sinc fenêtré (Blackman) avec passe-bas
    ratio = dst / src
    cutoff = min(1.0, ratio) * 0.95
    taps = 16
    half = int(math.ceil(taps / cutoff / 2))
    out = [0.0] * n_out
    for j in range(n_out):
        t = j / ratio
        c = int(t)
        acc = wsum = 0.0
        for k in range(c - half + 1, c + half + 1):
            if 0 <= k < n:
                d = t - k
                arg = math.pi * d * cutoff
                s = 1.0 if arg == 0 else math.sin(arg) / arg
                w = 0.42 + 0.5 * math.cos(math.pi * d / half) + 0.08 * math.cos(2 * math.pi * d / half)
                acc += x[k] * s * w
                wsum += s * w
        out[j] = acc / wsum if wsum else 0.0
    return out

# ---------------------------------------------------------------- ADPCM SPU (VAG)

FILTERS = [(0, 0), (60, 0), (115, -52), (98, -55), (122, -60)]


def _encode_block(block, s1, s2, f, shift):
    f0, f1 = FILTERS[f]
    err = 0
    dec = []
    for x in block:
        pred = (s1 * f0 + s2 * f1 + 32) >> 6
        q = ((x - pred) * (1 << shift) + (1 << 11)) >> 12
        q = max(-8, min(7, q))
        y = max(-32768, min(32767, ((q << 12) >> shift) + pred))
        err += (x - y) * (x - y)
        dec.append(y)
        s2, s1 = s1, y
    return err, dec, s1, s2


def adpcm_roundtrip(pcm):
    """pcm : entiers 16 bits. Encode en ADPCM SPU puis décode, comme la console."""
    n = len(pcm)
    pcm = pcm + [0] * ((-n) % 28)
    out = []
    s1 = s2 = 0
    for b in range(0, len(pcm), 28):
        block = pcm[b:b + 28]
        best = None
        for f in range(5):
            f0, f1 = FILTERS[f]
            p1, p2, peak = s1, s2, 0
            for x in block:
                r = x - ((p1 * f0 + p2 * f1 + 32) >> 6)
                peak = max(peak, abs(r))
                p2, p1 = p1, x
            shift = 12
            while shift > 0 and peak > (7 << (12 - shift)):
                shift -= 1
            for sh in {shift, max(0, shift - 1), min(12, shift + 1)}:
                res = _encode_block(block, s1, s2, f, sh)
                if best is None or res[0] < best[0]:
                    best = res
        _, dec, s1, s2 = best
        out.extend(dec)
    return out[:n]

# ---------------------------------------------------------------- relecture SPU (gaussienne)

_GAUSS_HEX = (
    "0000 0000 0000 0000 0000 0000 0000 0001 0001 0001 0001 0002 0002 0002 0003 0003 "
    "0003 0004 0004 0005 0005 0006 0007 0007 0008 0009 0009 000A 000B 000C 000D 000E "
    "000F 0010 0011 0012 0013 0015 0016 0018 0019 001B 001C 001E 0020 0021 0023 0025 "
    "0027 0029 002C 002E 0030 0033 0035 0038 003A 003D 0040 0043 0046 0049 004D 0050 "
    "0054 0057 005B 005F 0063 0067 006B 006F 0074 0078 007D 0082 0087 008C 0091 0096 "
    "009C 00A1 00A7 00AD 00B3 00BA 00C0 00C7 00CD 00D4 00DB 00E3 00EA 00F2 00FA 0101 "
    "010A 0112 011B 0123 012C 0135 013F 0148 0152 015C 0166 0171 017B 0186 0191 019C "
    "01A8 01B4 01C0 01CC 01D9 01E5 01F2 0200 020D 021B 0229 0237 0246 0255 0264 0273 "
    "0283 0293 02A3 02B4 02C4 02D6 02E7 02F9 030B 031D 0330 0343 0356 036A 037E 0392 "
    "03A7 03BC 03D1 03E7 03FC 0413 042A 0441 0458 0470 0488 04A0 04B9 04D2 04EC 0506 "
    "0520 053B 0556 0572 058E 05AA 05C7 05E4 0601 061F 063E 065C 067C 069B 06BB 06DC "
    "06FD 071E 0740 0762 0784 07A7 07CB 07EF 0813 0838 085D 0883 08A9 08D0 08F7 091E "
    "0946 096F 0998 09C1 09EB 0A16 0A40 0A6C 0A98 0AC4 0AF1 0B1E 0B4C 0B7A 0BA9 0BD8 "
    "0C07 0C38 0C68 0C99 0CCB 0CFD 0D30 0D63 0D97 0DCB 0E00 0E35 0E6B 0EA1 0ED7 0F0F "
    "0F46 0F7F 0FB7 0FF1 102A 1065 109F 10DB 1116 1153 118F 11CD 120B 1249 1288 12C7 "
    "1307 1347 1388 13C9 140B 144D 1490 14D4 1517 155C 15A0 15E6 162C 1672 16B9 1700 "
    "1747 1790 17D8 1821 186B 18B5 1900 194B 1996 19E2 1A2E 1A7B 1AC8 1B16 1B64 1BB3 "
    "1C02 1C51 1CA1 1CF1 1D42 1D93 1DE5 1E37 1E89 1EDC 1F2F 1F82 1FD6 202A 207F 20D4 "
    "2129 217F 21D5 222C 2282 22DA 2331 2389 23E1 2439 2492 24EB 2545 259E 25F8 2653 "
    "26AD 2708 2763 27BE 281A 2876 28D2 292E 298B 29E7 2A44 2AA1 2AFF 2B5C 2BBA 2C18 "
    "2C76 2CD4 2D33 2D91 2DF0 2E4F 2EAE 2F0D 2F6C 2FCC 302B 308B 30EA 314A 31AA 3209 "
    "3269 32C9 3329 3389 33E9 3449 34A9 3509 3569 35C9 3629 3689 36E8 3748 37A8 3807 "
    "3867 38C6 3926 3985 39E4 3A43 3AA2 3B00 3B5F 3BBD 3C1B 3C79 3CD7 3D35 3D92 3DEF "
    "3E4C 3EA9 3F05 3F62 3FBD 4019 4074 40D0 412A 4185 41DF 4239 4292 42EB 4344 439C "
    "43F4 444C 44A3 44FA 4550 45A6 45FC 4651 46A6 46FA 474E 47A1 47F4 4846 4898 48E9 "
    "493A 498A 49D9 4A29 4A77 4AC5 4B13 4B5F 4BAC 4BF7 4C42 4C8D 4CD7 4D20 4D68 4DB0 "
    "4DF7 4E3E 4E84 4EC9 4F0E 4F52 4F95 4FD7 5019 505A 509A 50DA 5118 5156 5194 51D0 "
    "520C 5247 5281 52BA 52F3 532A 5361 5397 53CC 5401 5434 5467 5499 54CA 54FA 5529 "
    "5558 5585 55B2 55DE 5609 5632 565B 5684 56AB 56D1 56F6 571B 573E 5761 5782 57A3 "
    "57C3 57E2 57FF 581C 5838 5853 586D 5886 589E 58B5 58CB 58E0 58F4 5907 5919 592A "
    "593A 5949 5958 5965 5971 597C 5986 598F 5997 599E 59A4 59A9 59AD 59B0 59B2 59B3"
)
GAUSS = [-1] * 16 + [int(h, 16) for h in _GAUSS_HEX.split()]
assert len(GAUSS) == 512


def spu_voice(samples, rate):
    """Joue des échantillons décodés comme une voix SPU : pitch 4.12, sortie 44100 Hz."""
    pitch = min(0x4000, int(round(rate * 4096 / 44100)))
    n = len(samples)
    hist = [0, 0, 0] + samples  # 3 zéros avant le début (historique de la voix)
    out = []
    counter = 0
    g = GAUSS
    while True:
        pos = counter >> 12
        if pos >= n + 3:  # +3 pour vider l'historique
            break
        i = (counter >> 4) & 0xFF
        new = hist[pos + 3] if pos < n else 0
        old = hist[pos + 2] if pos - 1 < n else 0
        older = hist[pos + 1] if pos - 2 < n else 0
        oldest = hist[pos] if pos - 3 < n else 0
        v = (g[0xFF - i] * oldest) >> 15
        v += (g[0x1FF - i] * older) >> 15
        v += (g[0x100 + i] * old) >> 15
        v += (g[i] * new) >> 15
        out.append(v)
        counter += pitch
    return out

# ---------------------------------------------------------------- réverb SPU

# Ordre des registres 1F801DC0h..1F801DFEh (psx-spx)
_REV_NAMES = ("dAPF1 dAPF2 vIIR vCOMB1 vCOMB2 vCOMB3 vCOMB4 vWALL "
              "vAPF1 vAPF2 mLSAME mRSAME mLCOMB1 mRCOMB1 mLCOMB2 mRCOMB2 "
              "dLSAME dRSAME mLDIFF mRDIFF mLCOMB3 mRCOMB3 mLCOMB4 mRCOMB4 "
              "dLDIFF dRDIFF mLAPF1 mRAPF1 mLAPF2 mRAPF2 vLIN vRIN").split()

# Réglages d'usine (taille de la zone de travail en octets, 32 registres)
_REV_PRESETS = {
    "room": (0x26C0, "007D 005B 6D80 54B8 BED0 0000 0000 BA80 5800 5300 04D6 0333 03F0 0227 0374 01EF "
                     "0334 01B5 0000 0000 0000 0000 0000 0000 0000 0000 01B4 0136 00B8 005C 8000 8000"),
    "studio_small": (0x1F40, "0033 0025 70F0 4FA8 BCE0 4410 C0F0 9C00 5280 4EC0 03E4 031B 03A4 02AF 0372 0266 "
                             "031C 025D 025C 018E 022F 0135 01D2 00B7 018F 00B5 00B4 0080 004C 0026 8000 8000"),
    "studio_medium": (0x4840, "00B1 007F 70F0 4FA8 BCE0 4510 BEF0 B4C0 5280 4EC0 0904 076B 0824 065F 07A2 0616 "
                              "076C 05ED 05EC 042E 050F 0305 0462 02B7 042F 0265 0264 01B2 0100 0080 8000 8000"),
    "studio_large": (0x6FE0, "00E3 00A9 6F60 4FA8 BCE0 4510 BEF0 A680 5680 52C0 0DFB 0B58 0D09 0A3C 0BD9 0973 "
                             "0B59 08DA 08D9 05E9 07EC 04B0 06EF 03D2 05EA 031D 031C 0238 0154 00AA 8000 8000"),
    "hall": (0xADE0, "01A5 0139 6000 5000 4C00 B800 BC00 C000 6000 5C00 15BA 11BB 14C2 10BD 11BC 0DC1 "
                     "11C0 0DC3 0DC0 09C1 0BC4 07C1 0A00 06CD 09C2 05C1 05C0 041A 0274 013A 8000 8000"),
    "half_echo": (0x3C00, "0017 0013 70F0 4FA8 BCE0 4510 BEF0 8500 5F80 54C0 0371 02AF 02E5 01DF 02B0 01D7 "
                          "0358 026A 01D6 011E 012D 00B1 011F 0059 01A0 00E3 0058 0040 0028 0014 8000 8000"),
    "space_echo": (0xF6C0, "033D 0231 7E00 5000 B400 B000 4C00 B000 6000 5400 1ED6 1A31 1D14 183B 1BC2 16B2 "
                           "1A32 15EF 15EE 1055 1334 0F2D 11F6 0C5D 1056 0AE1 0AE0 07A2 0464 0232 8000 8000"),
    "chaos_echo": (0x18040, "0001 0001 7FFF 7FFF 0000 0000 0000 8100 0000 0000 1FFF 0FFF 1005 0005 0000 0000 "
                            "1005 0005 0000 0000 0000 0000 0000 0000 0000 0000 1004 1002 0004 0002 8000 8000"),
    "delay": (0x18040, "0001 0001 7FFF 7FFF 0000 0000 0000 0000 0000 0000 1FFF 0FFF 1005 0005 0000 0000 "
                       "1005 0005 0000 0000 0000 0000 0000 0000 0000 0000 1004 1002 0004 0002 8000 8000"),
}

# FIR à 39 points de la console pour passer de 44100 à 22050 Hz et retour
_FIR = [-0x0001, 0, 0x0002, 0, -0x000A, 0, 0x0023, 0, -0x0067, 0, 0x010A, 0, -0x0268, 0,
        0x0534, 0, -0x0B90, 0, 0x2806, 0x4000, 0x2806, 0, -0x0B90, 0, 0x0534, 0, -0x0268, 0,
        0x010A, 0, -0x0067, 0, 0x0023, 0, -0x000A, 0, 0x0002, 0, -0x0001]


def _s16(v):
    return v - 0x10000 if v & 0x8000 else v


def _sat(v):
    return -32768 if v < -32768 else (32767 if v > 32767 else v)


def _fir_decimate(x):
    """44100 -> 22050 avec le FIR de la console."""
    taps = [(k - 19, c) for k, c in enumerate(_FIR) if c]
    n = len(x)
    out = []
    for j in range(0, n, 2):
        acc = 0
        for off, c in taps:
            k = j + off
            if 0 <= k < n:
                acc += x[k] * c
        out.append(acc / 32768.0)
    return out


def _fir_interpolate(x, n_out):
    """22050 -> 44100 avec le FIR de la console (insertion de zéros puis filtrage)."""
    taps = [(k - 19, c) for k, c in enumerate(_FIR) if c]
    up = [0.0] * n_out
    for j, v in enumerate(x):
        if 2 * j < n_out:
            up[2 * j] = v
    out = [0.0] * n_out
    for j in range(n_out):
        acc = 0.0
        for off, c in taps:
            k = j + off
            if 0 <= k < n_out:
                acc += up[k] * c
        out[j] = acc * 2 / 32768.0
    return out


def spu_reverb(dry, preset, tail_seconds):
    """Réverb SPU sur un signal mono à 44100 Hz. Retourne (gauche, droite) mouillés à 44100 Hz."""
    size, hexs = _REV_PRESETS[preset]
    r = {name: int(h, 16) for name, h in zip(_REV_NAMES, hexs.split())}
    N = size // 2  # échantillons 16 bits dans la zone de travail
    buf = [0] * N

    def off(name):  # registres d'adresse : unités de 8 octets = 4 échantillons
        return r[name] * 4

    def vol(name):
        return _s16(r[name]) / 32768.0

    vIIR, vWALL = vol("vIIR"), vol("vWALL")
    vAPF1, vAPF2 = vol("vAPF1"), vol("vAPF2")
    vC = [vol("vCOMB%d" % k) for k in range(1, 5)]
    vLIN, vRIN = vol("vLIN"), vol("vRIN")
    dAPF1, dAPF2 = off("dAPF1"), off("dAPF2")
    a = {k: off(k) for k in _REV_NAMES if k[0] in "md" and not k.startswith("dAPF")}

    pad = [0] * int(tail_seconds * 44100)
    inp = _fir_decimate(dry + pad)
    outL, outR = [], []
    cur = 0
    for s in inp:
        def rd(o):
            return buf[(cur + o) % N]

        def wr(o, v):
            buf[(cur + o) % N] = _sat(int(v))

        Lin = vLIN * s
        Rin = vRIN * s
        wr(a["mLSAME"], (Lin + rd(a["dLSAME"]) * vWALL - rd(a["mLSAME"] - 1)) * vIIR + rd(a["mLSAME"] - 1))
        wr(a["mRSAME"], (Rin + rd(a["dRSAME"]) * vWALL - rd(a["mRSAME"] - 1)) * vIIR + rd(a["mRSAME"] - 1))
        wr(a["mLDIFF"], (Lin + rd(a["dRDIFF"]) * vWALL - rd(a["mLDIFF"] - 1)) * vIIR + rd(a["mLDIFF"] - 1))
        wr(a["mRDIFF"], (Rin + rd(a["dLDIFF"]) * vWALL - rd(a["mRDIFF"] - 1)) * vIIR + rd(a["mRDIFF"] - 1))
        L = (vC[0] * rd(a["mLCOMB1"]) + vC[1] * rd(a["mLCOMB2"])
             + vC[2] * rd(a["mLCOMB3"]) + vC[3] * rd(a["mLCOMB4"]))
        R = (vC[0] * rd(a["mRCOMB1"]) + vC[1] * rd(a["mRCOMB2"])
             + vC[2] * rd(a["mRCOMB3"]) + vC[3] * rd(a["mRCOMB4"]))
        t = rd(a["mLAPF1"] - dAPF1)
        L = L - vAPF1 * t
        wr(a["mLAPF1"], L)
        L = L * vAPF1 + t
        t = rd(a["mRAPF1"] - dAPF1)
        R = R - vAPF1 * t
        wr(a["mRAPF1"], R)
        R = R * vAPF1 + t
        t = rd(a["mLAPF2"] - dAPF2)
        L = L - vAPF2 * t
        wr(a["mLAPF2"], L)
        L = L * vAPF2 + t
        t = rd(a["mRAPF2"] - dAPF2)
        R = R - vAPF2 * t
        wr(a["mRAPF2"], R)
        R = R * vAPF2 + t
        outL.append(L)
        outR.append(R)
        cur = (cur + 1) % N
    n_out = len(dry) + len(pad)
    return _fir_interpolate(outL, n_out), _fir_interpolate(outR, n_out)

# ---------------------------------------------------------------- traitement

def process_file(src_path, dst_path, opts):
    channels, src_rate = read_wav(src_path)
    if not opts.stereo and len(channels) > 1:
        n = len(channels[0])
        channels = [[sum(c[i] for c in channels) / len(channels) for i in range(n)]]
    g = 10 ** (opts.gain / 20.0) * 32767
    voices = []
    for ch in channels:
        x = high_shelf(ch, src_rate, opts.aigus)
        x = resample(x, src_rate, opts.rate, opts.resample)
        pcm = [max(-32768, min(32767, int(round(v * g)))) for v in x]
        dec = adpcm_roundtrip(pcm)
        voices.append(spu_voice(dec, opts.rate))
    if opts.reverb != "aucune":
        mono = voices[0] if len(voices) == 1 else [sum(v) / len(voices) for v in zip(*voices)]
        wl, wr = spu_reverb(mono, opts.reverb, opts.queue)
        n = len(wl)
        if len(voices) == 1:
            voices = [voices[0], list(voices[0])]
        out = []
        for v, w in zip(voices, (wl, wr)):
            v = v + [0] * (n - len(v))
            out.append([v[i] + w[i] * opts.wet for i in range(n)])
        voices = out
    write_wav16(dst_path, voices, 44100)
    return src_rate, len(voices)


def main():
    ap = argparse.ArgumentParser(description="Rend des SFX façon PlayStation 1 (chaîne SPU complète).")
    ap.add_argument("dossier", help="dossier de sons (ou un seul fichier .wav)")
    ap.add_argument("--out", help="dossier de sortie (défaut : <dossier>_PS1 à côté)")
    ap.add_argument("--rate", type=int, default=22050,
                    help="fréquence de l'échantillon dans la console : 22050 (défaut), 11025 = plus sale")
    ap.add_argument("--resample", choices=("brut", "lineaire", "propre"), default="brut",
                    help="brut = aliasing garanti (défaut), propre = filtré, sonne étouffé")
    ap.add_argument("--aigus", type=float, default=6.0,
                    help="dB d'aigus ajoutés avant conversion pour compenser l'étouffement (défaut 6, 0 = aucun)")
    ap.add_argument("--reverb", default="aucune", choices=["aucune"] + sorted(_REV_PRESETS),
                    help="réverb du SPU (défaut : aucune)")
    ap.add_argument("--wet", type=float, default=0.5, help="quantité de réverb (0 à 1, défaut 0.5)")
    ap.add_argument("--queue", type=float, default=1.5,
                    help="secondes ajoutées à la fin pour laisser la réverb mourir (défaut 1.5)")
    ap.add_argument("--stereo", action="store_true", help="garder la stéréo au lieu de passer en mono")
    ap.add_argument("--gain", type=float, default=0.0, help="gain en dB avant traitement (ex. -3)")
    opts = ap.parse_args()

    src = os.path.abspath(opts.dossier)
    if os.path.isfile(src):
        files, base = [src], os.path.dirname(src)
    else:
        base, files = src, []
        for root, _, names in os.walk(src):
            for nm in sorted(names):
                files.append(os.path.join(root, nm))
    out_dir = os.path.abspath(opts.out) if opts.out else base.rstrip("\\/") + "_PS1"

    wavs = [f for f in files if f.lower().endswith(".wav")]
    skipped = [f for f in files if os.path.splitext(f)[1].lower()
               in (".mp3", ".ogg", ".flac", ".aif", ".aiff", ".m4a", ".mp4")]
    ok, failed = 0, []
    for i, f in enumerate(wavs, 1):
        rel = os.path.relpath(f, base)
        try:
            sr, ch = process_file(f, os.path.join(out_dir, rel), opts)
            ok += 1
            print("[%d/%d] %s  (%d Hz -> %d Hz console, %d canal/aux)" % (i, len(wavs), rel, sr, opts.rate, ch))
        except Exception as e:  # on continue avec les autres
            failed.append((rel, str(e)))
            print("[%d/%d] ÉCHEC %s : %s" % (i, len(wavs), rel, e))

    print("\n%d fichier(s) traité(s) -> %s" % (ok, out_dir))
    if skipped:
        print("%d fichier(s) non-WAV ignoré(s) (convertis-les en WAV d'abord) :" % len(skipped))
        for f in skipped:
            print("   ", os.path.relpath(f, base))
    if failed:
        print("%d échec(s) :" % len(failed))
        for rel, e in failed:
            print("   ", rel, "-", e)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    main()
