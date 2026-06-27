# Changelog

All notable changes to PRecorder will be documented in this file.

## [Unreleased]

### Added
- Multi-language support (English / Chinese)
- Language selection in Setup wizard and Settings panel

### Fixed
- Self-contained single-file publish side-by-side configuration error (0x800736B1)

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

[Unreleased]: https://github.com/liml0324/PRecorder/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/liml0324/PRecorder/releases/tag/v1.0.0
