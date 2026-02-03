# Dynamic Browser Panels

**A customizable multi-panel web browser with tabbed browsing, persistent layouts, and local media playback built on Microsoft Edge WebView2.**

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## Overview

Dynamic Browser Panels transforms your browsing experience by allowing you to create and save custom multi-panel layouts. Built on the Microsoft Edge WebView2 engine, it provides a full-featured browsing experience while enabling you to split your workspace horizontally or vertically into multiple independent browser panels. Each panel supports unlimited tabs with custom naming, making it perfect for workflows that require monitoring multiple web pages simultaneously—such as data dashboards, research, development, or content creation.

## ✨ Key Features

- 🌐 **Multi-Panel Browsing** - Split workspace into unlimited independent browser panels
- 📑 **Advanced Tab Management** - Unlimited tabs with custom naming and reordering
- 💾 **Layout Persistence** - Save and load multiple named layouts
- 🎬 **Local Media Playback** - Play video and audio files directly in browser tabs
- 🔐 **Password Management** - Integrated autosave and autofill
- ⚙️ **One-Click Installation** - Installs to Program Files with `.frm` file association

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- .NET 8.0 Runtime ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- WebView2 Runtime (usually pre-installed)

### Installation

**Option 1: Install from Build (Recommended)**
1. Build the project in Visual Studio
2. Run the application
3. Right-click in URL bar → Select "⚙️ Install Application..."
4. Grant administrator permissions
5. App installs to `C:\Program Files\DynamicBrowserPanels`

**Option 2: Run Portable**
dotnet build
dotnet run

### First Steps

1. **Create your first layout:**
   - Right-click URL bar → "Split Horizontal" or "Split Vertical"
   - Navigate each panel to your desired pages
   - Add tabs with "+ New Tab" from context menu

2. **Customize tabs:**
   - Right-click URL bar → "✎ Rename Tab..."
   - Name tabs like "Dashboard", "Email", "Monitoring"
   - Reorder with "← Move Tab Left" / "Move Tab Right →"

3. **Save your layout:**
   - Right-click URL bar → "💾 Save Layout As..."
   - Give it a name like "Work Setup.frm"
   - Double-click the `.frm` file anytime to restore

## 📖 Documentation

- **[Complete Feature Guide](README_Complete.md)** - Detailed documentation of all features
- **[Media Playback Guide](README_Complete.md#-how-to-use-media-files)** - How to play local video/audio files
- **[Architecture Overview](ARCHITECTURE.md)** - Technical design and code structure

## 🎯 Use Cases

### Development Workflow
┌──────────────────┬──────────────────┐
│ Code Editor Web  │ API Docs         │
│ (GitHub Codespace│ (localhost:3000) │
├──────────────────┴──────────────────┤
│ Build Logs & Console                │
└─────────────────────────────────────┘

### Trading Dashboard
┌──────────┬──────────┬──────────┐
│ BTC/USD  │ ETH/USD  │ News     │
│ Chart    │ Chart    │ Feed     │
├──────────┴──────────┴──────────┤
│ Trading Platform                │
└─────────────────────────────────┘

### Research & Writing
┌──────────────────┬──────────────────┐
│ Research Tab 1   │ Google Docs      │
│ Research Tab 2   │ (Writing)        │
│ Reference Video  │                  │
└──────────────────┴──────────────────┘

## 🎬 Media Features

### Supported Formats

**Video:** MP4, WebM, OGG  
**Audio:** MP3, WAV, AAC, M4A, FLAC, Opus

### How to Play Media

1. Right-click URL bar → "📁 Open Media File..."
2. Select your video or audio file
3. Use HTML5 player controls (play, pause, seek, volume, fullscreen)
4. Media reopens automatically when you restore the layout

## 🔑 Context Menu Commands

**Navigation:**
- ← Back, → Forward, ⟳ Refresh, ⌂ Home

**Tab Management:**
- \+ New Tab
- ✕ Close Tab (keeps at least 1 tab)
- ✎ Rename Tab...
- ← Move Tab Left / Move Tab Right →

**Media:**
- 📁 Open Media File...

**Layout:**
- Split Horizontal ⬌
- Split Vertical ⬍
- 💾 Save Layout As...
- 📂 Load Layout...
- Reset Layout

**Tools:**
- 🔑 Manage Passwords
- ⚙️ Install Application...
- 🗑️ Uninstall Application...

## ⚙️ Configuration

### State Storage

**Normal Mode:**
- Auto-saves to: `%LocalAppData%\DynamicBrowserPanels\Current Layout.frm`
- Custom layouts: Save anywhere as `.frm` files

**Template Mode (Read-Only):**
- Open any `.frm` file via double-click or command line
- Changes are NOT saved to the template
- Exit template mode with "Reset Layout"

### Backup System

When installed, app creates backups of layouts in:
- `C:\Program Files\DynamicBrowserPanels\Backups`
- Original development location (if `backup.dat` exists)

## 🏗️ Building from Source

bash
# Clone repository
git clone https://github.com/yourusername/DynamicBrowserPanels.git
cd DynamicBrowserPanels

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

**Common Issues:**

- **Media file not found:** File was moved/deleted after saving layout
- **Can't install:** Requires administrator privileges
- **Tabs not renaming:** Make sure to press OK in the rename dialog
- **Layout not saving:** Check write permissions to `%LocalAppData%`

**Need Help?** Open an issue on GitHub with:
- Application version
- Steps to reproduce
- Error messages (if any)

---

**Built with ❤️ using .NET 8 and WebView2**