# Changelog

All notable changes to PRecorder will be documented in this file.

## [1.0.1] - 2026-06-27

### Added
- Multi-language support (English / Chinese)
- Language selection in Setup wizard and Settings panel
- Separate save duration setting (independent from buffer length)
- Save duration dropdown next to Save button, dynamically limited by buffer length

### Changed
- Save duration selector moved from Settings to MainWindow toolbar

### Fixed
- Self-contained single-file publish side-by-side configuration error (0x800736B1)
- SettingsWindow not fully localized when switching language
- Remaining hardcoded Chinese strings in MainWindow XAML
- Misleading "restart required" dialog after language change
- Portable version window focus and taskbar visibility on launch

## [1.0.0] - 2026-06-27

### Added
- Audio pre-recording with configurable ring buffer (1–60 minutes)
- One-click save to WAV, MP3, FLAC, AAC, and OGG Vorbis
- System tray support with minimize-to-tray option
- Custom save location and default format
- Configurable close behavior (tray vs. exit)
- Settings panel (path, format, buffer duration, close behavior)
- Audio device selection
- Traditional .exe installer with Inno Setup (full + portable)
- FFmpeg integration for non-WAV formats
- Persistent settings (JSON in %AppData%)
- App icon (multiple resolutions)

### Changed
- Migrated from console app to WPF GUI
- Switched from MSIX to Inno Setup packaging
- Optimized icon assets (2.5 MB → 60 KB)

### Fixed
- Ring buffer lock contention causing audio dropouts
- Missing icon causing silent crash on startup
- Framework-dependent portable version apphost compatibility

[1.0.1]: https://github.com/liml0324/PRecorder/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/liml0324/PRecorder/releases/tag/v1.0.0
