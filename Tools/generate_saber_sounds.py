#!/usr/bin/env python3
"""Synthesizes the lightsaber sound effects for Assignment 5, Part 5 (no third-party audio is used).

Writes 16-bit mono 44.1 kHz WAV files:
  saber_hum.wav     2 s seamless loop: buzzy ~90 Hz "projector motor" hum with slow beating and a faint fizz
  saber_ignite.wav  snap-hiss plus a rising "vwoom" that settles at the hum's pitch
  saber_retract.wav a falling "vwoom" that powers down
  saber_swing.wav   a Doppler-style pitch swell of the hum plus an airy whoosh

Only the Python standard library is needed. Usage (from the project root):
  python3 Tools/generate_saber_sounds.py            # writes to Assets/Audio
  python3 Tools/generate_saber_sounds.py <out_dir>
"""
import math
import random
import sys
import wave
from array import array
from pathlib import Path

SR = 44100
TAU = 2.0 * math.pi
random.seed(590)  # deterministic output


def harmonic_stack(phase, harmonics, tilt):
    """Sum of harmonics with amplitudes 1/n^tilt: a buzzy, sawtooth-like tone."""
    total = 0.0
    for n in range(1, harmonics + 1):
        total += math.sin(n * phase) / (n ** tilt)
    return total


def normalize(samples, peak):
    biggest = max(abs(s) for s in samples) or 1.0
    return [s * peak / biggest for s in samples]


def hum(seconds=2.0):
    # Every frequency is a multiple of 0.5 Hz, so each completes whole cycles in 2 s and the loop is seamless.
    out = []
    for i in range(int(SR * seconds)):
        t = i / SR
        p1 = TAU * 90.0 * t
        p2 = TAU * 92.0 * t  # 2 Hz beating against 90 Hz
        tone = 0.75 * harmonic_stack(p1, 16, 1.2) + 0.35 * harmonic_stack(p2, 8, 1.4)
        fizz = 0.04 * sum(math.sin(k * p1) for k in (15, 21, 27, 30)) * (0.6 + 0.4 * math.sin(TAU * 6.0 * t))
        swell = 0.85 + 0.15 * math.sin(TAU * 0.5 * t)
        out.append((tone + fizz) * swell)
    return normalize(out, 0.7)


def ignite(seconds=0.8):
    out, phase, noise_lp = [], 0.0, 0.0
    for i in range(int(SR * seconds)):
        t = i / SR
        if t < 0.32:  # sweep up past the hum pitch...
            x = t / 0.32
            freq = 45.0 + 55.0 * (1.0 - (1.0 - x) ** 3)
        else:  # ...then settle back to the hum's 90 Hz
            freq = 100.0 - 10.0 * min((t - 0.32) / 0.25, 1.0)
        phase += TAU * freq / SR
        tone = harmonic_stack(phase, 14, 1.1)
        attack = min(t / 0.015, 1.0)
        swell = 0.55 + 0.45 * math.exp(-(((t - 0.22) / 0.12) ** 2))
        tail = 1.0 if t < 0.65 else max(0.0, 1.0 - (t - 0.65) / 0.15)
        noise_lp += 0.35 * (random.uniform(-1.0, 1.0) - noise_lp)  # snap-hiss at the very start
        hiss = noise_lp * 1.6 * math.exp(-t / 0.05) * min(t / 0.002, 1.0)  # 2 ms fade-in: sharp, but no click
        out.append(tone * attack * swell * tail + hiss)
    return normalize(out, 0.85)


def retract(seconds=0.6):
    out, phase = [], 0.0
    for i in range(int(SR * seconds)):
        t = i / SR
        x = min(t / 0.5, 1.0)
        freq = 92.0 - 60.0 * x ** 1.5
        phase += TAU * freq / SR
        tone = harmonic_stack(phase, 14, 1.1)
        env = min(t / 0.01, 1.0) * (1.0 - x) ** 1.3 * (1.0 + 0.3 * math.exp(-(((t - 0.08) / 0.06) ** 2)))
        out.append(tone * env)
    return normalize(out, 0.8)


def swing(seconds=0.55):
    out, phase, noise_lp = [], 0.0, 0.0
    for i in range(int(SR * seconds)):
        u = i / (SR * seconds)
        env = math.sin(math.pi * u) ** 1.6
        freq = 90.0 * (1.0 + 0.55 * math.sin(math.pi * u))  # Doppler-like rise and fall
        phase += TAU * freq / SR
        tone = harmonic_stack(phase, 12, 1.1)
        noise_lp += (0.02 + 0.25 * env) * (random.uniform(-1.0, 1.0) - noise_lp)  # whoosh brightens as it swells
        out.append(0.8 * tone * env + 1.8 * noise_lp * env)
    return normalize(out, 0.85)


def write_wav(path, samples):
    pcm = array("h", (int(max(-1.0, min(1.0, s)) * 32767) for s in samples))
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SR)
        wav.writeframes(pcm.tobytes())


def main():
    out_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent.parent / "Assets" / "Audio"
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, make in (("saber_hum", hum), ("saber_ignite", ignite), ("saber_retract", retract), ("saber_swing", swing)):
        samples = make()
        write_wav(out_dir / f"{name}.wav", samples)
        rms = math.sqrt(sum(s * s for s in samples) / len(samples))
        print(f"{name}.wav  {len(samples) / SR:.2f}s  peak={max(abs(s) for s in samples):.2f}  rms={rms:.3f}")


if __name__ == "__main__":
    main()
