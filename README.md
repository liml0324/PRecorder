# PRecorder

A lightweight Windows desktop app for **pre-recording** audio — it continuously captures audio from a microphone or line-in device into a rolling 5-minute memory buffer. When you press save, it exports the last 5 minutes to disk. Never miss a moment you forgot to hit "record" for.

## Features

- **Always-on Pre-recording** — Continuously buffers the last 5 minutes of audio in memory with zero disk usage until you save.
- **One-click Save** — Press a button (or right-click the tray icon) to export the recent audio as a file.
- **Multi-format Export** — Save as WAV, MP3, FLAC, AAC, or OGG Vorbis (with FFmpeg).
- **System Tray** — Minimizes to the notification area; keep recording in the background without a window.
- **Custom Save Location** — Choose any folder as the output directory.
- **Device Selection** — Pick any available audio input device (microphone, line-in, USB audio interface, etc.).

## How It Works

```
┌──────────────────────────────────────────────────────┐
│                  Audio Input Device                   │
│            (microphone / line-in / piano)             │
└──────────────────────┬───────────────────────────────┘
                       │ PCM audio stream
                       ▼
┌──────────────────────────────────────────────────────┐
│              Ring Buffer (in memory)                  │
│            Last 5 minutes, 44100 Hz 16-bit stereo     │
│                  ~53 MB, zero GC pressure             │
└──────────────────────┬───────────────────────────────┘
                       │ User clicks "Save"
                       ▼
┌──────────────────────────────────────────────────────┐
│   WAV (direct)  ──or──▶  FFmpeg  ──▶  MP3 / FLAC / … │
└──────────────────────────────────────────────────────┘
```

The ring buffer uses a fixed `byte[]` with write-position tracking — no allocations during recording, no GC stalls, and save operations hold the lock for microseconds only.

## Requirements

- Windows 10 or later
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for building from source)
- At least one audio input device
- [FFmpeg](https://ffmpeg.org/download.html) (optional — required only for MP3, FLAC, AAC, and OGG export)

## Quick Start

```bash
# Clone the repository
git clone <repo-url>
cd PRecorder

# Build and run
dotnet run

# Or specify an audio device by ID
dotnet run -- 2
```

## Build

```bash
dotnet build -c Release
```

The compiled executable is at `bin/Release/net10.0-windows/PRecorder.exe`.

## Usage

| Action | How |
|--------|-----|
| Start recording | Click **▶ Start Recording** |
| Save last 5 minutes | Click **💾 Save** or right-click tray icon → **Save** |
| Change save location | Click **Browse...** next to the path field |
| Change output format | Select from the format dropdown |
| Stop recording | Click **⏹ Stop Recording** |
| Minimize to tray | Click the window's **X** button |
| Restore from tray | Double-click the tray icon |
| Exit completely | Right-click tray icon → **Exit** or click **退出** in the window |

## Supported Export Formats

| Format | FFmpeg Required | Codec | Quality |
|--------|:---------------:|-------|---------|
| **WAV** | No | PCM 16-bit | Lossless (44100 Hz stereo) |
| **MP3** | Yes | libmp3lame | 320 kbps CBR |
| **FLAC** | Yes | FLAC | Lossless compression |
| **AAC** | Yes | AAC | 256 kbps |
| **OGG Vorbis** | Yes | libvorbis | Quality 6 (~192 kbps) |

## Tech Stack

- **.NET 10** (WPF + Windows Forms for tray icon)
- **NAudio 2.3** — Audio capture
- **FFmpeg** — Format transcoding (optional)

## Project Structure

```
PRecorder/
├── App.xaml / .cs          # WPF application entry, system tray icon
├── MainWindow.xaml / .cs   # Main window UI and logic
├── PianoRecorder.cs        # Core recorder with ring buffer
├── PRecorder.csproj        # Project configuration
└── Program.cs              # (placeholder — entry via App.xaml)
```

## License

MIT
