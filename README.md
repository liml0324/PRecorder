# PRecorder

A lightweight Windows desktop app for **pre-recording** audio — it continuously captures audio from a microphone or line-in device into a rolling 5-minute memory buffer. When you press save, it exports the last 5 minutes to disk. Never miss a moment you forgot to hit "record" for.

## Features

- **Always-on Pre-recording** — Continuously buffers audio in memory with zero disk usage until you save.
- **One-click Save** — Press a button (or right-click the tray icon) to export the recent audio as a file.
- **Configurable Buffer** — Adjust the ring buffer duration from 1 to 60 minutes via the Settings panel.
- **Multi-format Export** — Save as WAV, MP3, FLAC, AAC, or OGG Vorbis (with FFmpeg).
- **System Tray** — Minimizes to the notification area; keep recording in the background without a window.
- **Flexible Close Behavior** — Choose whether closing the window minimizes to tray or exits the app.
- **Custom Save Location & Format** — Set your preferred output folder and default file format in Settings.
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
│           Ring Buffer (in memory)                     │
│    Configurable: 1–60 min, 44100 Hz 16-bit stereo    │
│           zero allocations, zero GC pressure          │
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
| Save buffered audio | Click **💾 Save** or right-click tray icon → **Save** |
| Stop recording | Click **⏹ Stop Recording** |
| Open settings | Click **⚙ Settings** |
| Change save path / format | Settings → **Save Path** or **Format** dropdown |
| Change buffer duration | Settings → **Buffer Duration** (1–60 min) |
| Change close behavior | Settings → **Close Window** (tray vs. exit) |
| Minimize to tray | Click the window's **X** button (if tray mode is on) |
| Restore from tray | Double-click the tray icon |
| Exit completely | Right-click tray icon → **Exit** or click **退出** button |

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
├── MainWindow.xaml / .cs   # Main window UI and recording logic
├── SettingsWindow.xaml/.cs # Settings panel (path, format, buffer, behavior)
├── PianoRecorder.cs        # Core recorder with ring buffer
├── AppSettings.cs          # Persistent settings (JSON in %AppData%)
├── PRecorder.csproj        # Project configuration
└── Program.cs              # (placeholder — entry via App.xaml)
```

## License

MIT

---

*App icon created by Gemini, further modified with ChatGPT.*
