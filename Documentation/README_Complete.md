# Dynamic Browser Panels - Complete Feature Guide

A sophisticated WinForms application featuring dynamic split-panel browsing with **tabbed browsing**, **custom tab naming**, **tab reordering**, and **local media playback** support, plus full persistent state management.

## 🎯 Complete Feature List

### Core Functionality
- ✅ **Tabbed Browsing** - Multiple tabs per browser panel, each with independent state
- ✅ **Custom Tab Names** - Rename tabs and persist names across sessions
- ✅ **Tab Reordering** - Move tabs left/right within a panel
- ✅ **Local Media Playback** - Play video and audio files directly in browser tabs
- ✅ **Compact WebView2 Control** - URL bar with no navigation buttons (all in right-click menu)
- ✅ **Dynamic Split Panels** - Split any browser panel horizontally or vertically at runtime
- ✅ **Nested Splits** - Unlimited nesting of split panels within split panels
- ✅ **Complete State Persistence** - Automatically saves and restores everything
- ✅ **Layout Management** - Save, load, and reset layouts
- ✅ **Installation System** - One-click install/uninstall with file associations
- ✅ **Password Management** - Integrated Edge password autosave/autofill

### Tab Management Features
- 📑 **Custom Tab Naming** - Right-click → "✎ Rename Tab..."
  - Name tabs like "Dashboard", "Email", "Monitoring"
  - Names persist across sessions
  - Clear name to revert to page title
- 📑 **Tab Reordering** - Right-click → "← Move Tab Left" / "Move Tab Right →"
  - Reorder tabs within a panel
  - Order persists in saved layouts
- 📑 **Tab Creation/Deletion** - "+" New Tab / "✕" Close Tab
  - Always maintains at least one tab
  - New tabs start at home URL
- 📑 **Tab Selection** - Click tabs to switch
  - URL bar updates to show active tab
  - Tab-specific navigation history

### Media Playback Features
- 🎬 **Supported Video Formats** - MP4, WebM, OGG
- 🎵 **Supported Audio Formats** - MP3, WAV, AAC, M4A, FLAC, Opus
- 🎬 **File Dialog Integration** - Filter by media type (All, Video, Audio, or All Files)
- 🎬 **Format Validation** - Automatic validation with helpful error messages
- 🎬 **Conversion Suggestions** - Guidance for unsupported formats
- 🎬 **State Persistence** - Media files restore on application restart
- 🎬 **HTML5 Player** - Full playback controls (play, pause, seek, volume, fullscreen)

### Layout & State Features
- 💾 **Auto-Save** - Current layout saved on exit (normal mode)
- 💾 **Named Layouts** - Save as `.frm` files with custom names
- 💾 **Template Mode** - Open `.frm` files as read-only templates
- 💾 **State Includes:**
  - Window size and position
  - Panel configuration (splits and orientations)
  - All tabs in all panels
  - Tab URLs and navigation state
  - Custom tab names
  - Selected tab per panel
  - Media file paths

### Installation Features
- ⚙️ **Program Files Installation** - Installs to `C:\Program Files\DynamicBrowserPanels`
- ⚙️ **File Association** - `.frm` files open with Dynamic Browser Panels
- ⚙️ **Update Support** - Re-install over existing version
- ⚙️ **Clean Uninstall** - Removes app but preserves user data
- ⚙️ **Portable Mode** - Run without installing
- ⚙️ **Backup System** - Auto-backup of layouts during development

### Right-Click Menu Options

**Navigation:**
- **← Back** - Navigate to previous page (current tab)
- **→ Forward** - Navigate to next page (current tab)
- **⟳ Refresh** - Reload current page (current tab)
- **⌂ Home** - Return to home page (current tab)

**Media:**
- **📁 Open Media File...** - Open video/audio files in current tab

**Tab Management:**
- **+ New Tab** - Create a new tab in this browser panel
- **✕ Close Tab** - Close current tab (must have at least 1 tab)
- **✎ Rename Tab...** - ⭐ **NEW!** Set custom name for current tab
- **← Move Tab Left** - ⭐ **NEW!** Move current tab one position left
- **Move Tab Right →** - ⭐ **NEW!** Move current tab one position right

**Layout:**
- **Split Horizontal ⬌** - Create horizontal split (top/bottom panels)
- **Split Vertical ⬍** - Create vertical split (left/right panels)
- **💾 Save Layout As...** - Save current layout to `.frm` file
- **📂 Load Layout...** - Load a saved layout
- **Reset Layout** - Remove all splits and return to single browser

**Tools:**
- **🔑 Manage Passwords** - Open Google Password Manager
- **⚙️ Install Application...** - Install to Program Files (shows when running portable)
- **🗑️ Uninstall Application...** - Uninstall from system (shows when installed)

## 📋 Prerequisites

1. **.NET 8.0 SDK or later** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **WebView2 Runtime** - Usually pre-installed on Windows 10/11
   - [Manual Download](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
3. **Windows 10 or later**

## 🚀 Quick Start

### Build and Run

# Navigate to project directory
cd DynamicBrowserPanels

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

### Install to System

1. Build and run the application
2. Right-click in URL bar
3. Select "⚙️ Install Application..."
4. Grant administrator permissions when prompted
5. App installs to `C:\Program Files\DynamicBrowserPanels`
6. `.frm` files now open with Dynamic Browser Panels

## 📖 How to Use Custom Tab Names

### Renaming a Tab

1. Right-click in the URL bar
2. Select **"✎ Rename Tab..."**
3. Enter your custom name (e.g., "Email Dashboard")
4. Click **OK**

**To revert to page title:**
1. Right-click → "✎ Rename Tab..."
2. Leave the text box empty
3. Click **OK**

### Tab Names Persist!

**What Gets Saved:**
- Custom tab names are stored in the layout
- Saved to `.frm` files when you save layouts
- Auto-saved with "Current Layout.frm" on exit

**What Gets Restored:**
- When you reopen the app, custom tab names return
- When you load a `.frm` file, tab names are restored
- Page title changes DON'T override custom names

**Example:**

Before renaming:
Tab 1: "Gmail - Inbox"     (page title)
Tab 2: "GitHub"            (page title)
Tab 3: "localhost:3000"    (page title)

After renaming:
Tab 1: "Email"             (custom name - persists!)
Tab 2: "Code Review"       (custom name - persists!)
Tab 3: "Dev Server"        (custom name - persists!)


## 📖 How to Reorder Tabs

### Moving Tabs

**Move Left:**
1. Select the tab you want to move
2. Right-click in URL bar
3. Select **"← Move Tab Left"**
4. Tab swaps position with the tab on its left

**Move Right:**
1. Select the tab you want to move
2. Right-click in URL bar
3. Select **"Move Tab Right →"**
4. Tab swaps position with the tab on its right

**Rules:**
- Can't move leftmost tab further left (disabled)
- Can't move rightmost tab further right (disabled)
- Tab order persists in saved layouts

**Example Workflow:**

Initial Order:
[Home] [GitHub] [Email] [Docs]
         ↑ (selected)

After "Move Tab Left":
[GitHub] [Home] [Email] [Docs]
  ↑ (still selected)

After "Move Tab Right" twice:
[Home] [Email] [Docs] [GitHub]
                        ↑ (still selected)


## 📖 How to Use Media Files

### Opening a Media File

**Method 1: Right-Click Menu (Recommended)**
1. Right-click on any browser panel
2. Select **"📁 Open Media File..."**
3. Choose filter:
   - **All Media Files** - Shows all supported video and audio
   - **Video Files** - Shows only MP4, WebM, OGG
   - **Audio Files** - Shows only MP3, WAV, AAC, etc.
   - **All Files** - Shows everything
4. Select your file
5. File opens with HTML5 player controls in current tab

**If you cancel:** Nothing changes, current tab stays in current state

### Supported Media Formats

#### ✅ Video Formats (Fully Supported)
- **MP4** (.mp4) - H.264 video + AAC audio ← **Best choice!**
- **WebM** (.webm) - VP8/VP9 video + Opus audio
- **OGG** (.ogv, .ogg) - Theora video + Vorbis audio

#### ✅ Audio Formats (Fully Supported)
- **MP3** (.mp3)
- **WAV** (.wav)
- **AAC** (.aac, .m4a)
- **FLAC** (.flac)
- **Opus** (.opus)
- **OGG** (.ogg)

#### ❌ NOT Supported
- AVI, WMV, MOV, MKV, FLV
- These require conversion to MP4 (recommendations provided in error messages)

### Media File State Persistence

**What Gets Saved:**
When you open a media file and close the application:
- ✅ File path is saved (stored as `media:///C:/path/to/file.mp4`)
- ✅ Which tab had the media file
- ✅ Custom tab name (if you renamed it)
- ✅ Which panel contained the tab
- ✅ Complete layout with all splits

**What Gets Restored:**
When you reopen the application:
- ✅ Media file automatically reopens in same tab
- ✅ Custom tab name restored
- ✅ Same panel and layout position
- ✅ If file moved/deleted, shows friendly error and loads home page

**Example State File:**

{
  "RootPanel": {
    "TabsState": {
      "SelectedTabIndex": 0,
      "TabUrls": [
        "media:///C:/Users/Public/Videos/vacation.mp4",
        "https://www.github.com"
      ],
      "TabCustomNames": [
        "Family Vacation",
        "Code Review"
      ]
    }
  }
}


### Media Playback Controls

When a media file is playing, you get:
- ▶️ Play/Pause button
- ⏮️ Seek bar (drag to jump to any point)
- 🔊 Volume control
- ⛶ Fullscreen button (video only)
- ⏱️ Duration display

## 💡 Real-World Examples

### Example 1: Development Workflow with Named Tabs

┌──────────────────────┬──────────────────────┐
│ "API Docs"           │ "Code Editor"        │
│ "Build Logs"         │ "Pull Requests"      │
│ "Localhost:3000"     │                      │
├──────────────────────┴──────────────────────┤
│ "Database Admin" (phpMyAdmin)              │
└───────────────────────────────────────────┘


**Setup:**
1. Create 3 panels (2 splits)
2. Add tabs and navigate to your tools
3. Rename each tab with descriptive names
4. Save as "Dev Workflow.frm"
5. Double-click "Dev Workflow.frm" anytime to restore

### Example 2: Video Editing Reference with Custom Names

┌─────────────────────┬─────────────────────┐
│ "Source Footage"    │ "Tutorial"          │
│ (local MP4)         │ (YouTube)           │
├─────────────────────┴─────────────────────┤
│ "Editing Software" (DaVinci Resolve Web)  │
└───────────────────────────────────────────┘


**Setup:**
1. Split vertically
2. Left panel: Open Media File → rename tab to "Source Footage"
3. Right panel: YouTube tutorial → rename to "Tutorial"
4. Bottom: Editor → rename to "Editing Software"
5. Save as "Video Edit Project.frm"

### Example 3: Multi-Tab Research with Organization

┌──────────────────────────────────────────┐
│ "Primary Source" | "Secondary" | "Notes"│
│  (Tab 1)         | (Tab 2)     | (Tab 3)│
├──────────────────────────────────────────┤
│ "Writing" (Google Docs)                  │
└──────────────────────────────────────────┘


**Setup:**
1. Top panel: Create 3 tabs for research
2. Rename tabs: "Primary Source", "Secondary", "Notes"
3. Reorder tabs as needed with Move Left/Right
4. Bottom: Google Docs → rename to "Writing"
5. Save as "Research Project.frm"

### Example 4: Music Production with Audio References

┌──────────────────────────────────────────┐
│ "Ref 1: Bass" | "Ref 2: Drums" | "Mix"  │
│ (MP3)         | (MP3)          | (MP3)  │
├──────────────────────────────────────────┤
│ "DAW" (Web-based DAW)                    │
└──────────────────────────────────────────┘


**Setup:**
1. Top panel: Open 3 media files
2. Rename tabs to describe each reference
3. Bottom panel: DAW interface
4. Reorder reference tabs as needed
5. Save as "Mix Session.frm"

(Continues with Technical Details, Troubleshooting, etc... file is too long for one message)

Would you like me to split this into multiple parts, or would you prefer a different delivery method (like a downloadable text file or GitHub Gist link)?