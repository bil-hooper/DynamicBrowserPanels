# Layout Management Guide

## Overview

Dynamic Browser Panels now includes powerful layout management features that let you save and load custom panel configurations. This is perfect for different workflows, projects, or tasks.

## How Layout Persistence Works

### Automatic Persistence (Current Layout)

**What happens automatically:**
1. **On Close:** When you close the application, your current layout is automatically saved to `Current Layout.json`
2. **On Open:** When you launch the application, it automatically loads from `Current Layout.json`
3. **No action required:** This happens seamlessly in the background

**File Location:**
```
%LocalAppData%\DynamicBrowserPanels\Current Layout.json
```

**What's saved:**
- Form size and position
- All split panel configurations
- Splitter positions
- All tabs in every browser panel
- URLs for every tab
- Selected tab in each panel
- Media file paths

### Manual Save/Load (Named Layouts)

**Use cases:**
- Different workflows (development, research, entertainment)
- Different projects (Project A layout, Project B layout)
- Different tasks (coding, writing, video editing)
- Sharing layouts with team members

## Using Save Layout

### Step-by-Step: Saving a Layout

1. **Set up your layout** - Create splits, tabs, and navigate to desired URLs
2. **Right-click** on the URL bar of any browser panel
3. **Select "💾 Save Layout As..."**
4. **Choose location** - Default: `%LocalAppData%\DynamicBrowserPanels\`
5. **Enter filename** - e.g., "Development Workflow", "Research Layout"
6. **Click Save**
7. **Confirmation** - You'll see: "Layout saved successfully to: [filepath]"

### Example Saved Layout Files

```
%LocalAppData%\DynamicBrowserPanels\
├── Current Layout.json          ← Auto-saved on close
├── Development Workflow.json    ← Your saved layout
├── Research Setup.json          ← Your saved layout
├── Video Editing.json           ← Your saved layout
└── Client Project A.json        ← Your saved layout
```

### What Gets Saved

**Complete state snapshot:**
```json
{
  "FormWidth": 1920,
  "FormHeight": 1080,
  "FormX": 100,
  "FormY": 100,
  "RootPanel": {
    "IsSplit": true,
    "SplitOrientation": "Vertical",
    "SplitterDistance": 960,
    "Panel1": {
      "TabsState": {
        "SelectedTabIndex": 0,
        "TabUrls": [
          "https://github.com",
          "https://stackoverflow.com"
        ]
      }
    },
    "Panel2": {
      "TabsState": {
        "SelectedTabIndex": 0,
        "TabUrls": [
          "media:///C:/Videos/tutorial.mp4"
        ]
      }
    }
  }
}
```

## Using Load Layout

### Step-by-Step: Loading a Layout

1. **Right-click** on the URL bar of any browser panel
2. **Select "📂 Load Layout..."**
3. **Browse** to your saved layout file
4. **Select** the .json file
5. **Click Open**
6. **Layout loads** - Your current layout is:
   - Reset completely
   - Replaced with the loaded layout
   - All panels, tabs, and URLs restored
7. **Confirmation** - You'll see: "Layout loaded successfully from: [filepath]"

### What Happens When You Load

**Automatic actions:**
1. ✅ Current layout is cleared (all panels disposed)
2. ✅ Form resizes to saved dimensions
3. ✅ Form repositions to saved location
4. ✅ All split panels recreate exactly
5. ✅ All browser panels restore
6. ✅ All tabs recreate with saved URLs
7. ✅ Selected tabs restore
8. ✅ Media files reload (if still available)

**Important:** Loading a layout does NOT modify the layout file you loaded from!

## Current Layout vs. Named Layouts

### Current Layout Behavior

**Scenario 1: Normal workflow**
```
1. Start app → Loads "Current Layout.json"
2. Work, make changes
3. Close app → Saves to "Current Layout.json"
4. Reopen app → Loads updated "Current Layout.json"
```

**Scenario 2: Load a named layout**
```
1. Start app → Loads "Current Layout.json"
2. Right-click → Load Layout → Select "Development Workflow.json"
3. Layout changes to Development Workflow
4. Work, make changes
5. Close app → Saves current state to "Current Layout.json" (NOT to Development Workflow.json!)
6. Reopen app → Loads "Current Layout.json" (with your changes)
```

**Scenario 3: Save changes to a named layout**
```
1. Right-click → Load Layout → Select "Development Workflow.json"
2. Make changes to the layout
3. Right-click → Save Layout As... → Overwrite "Development Workflow.json"
4. Changes now saved to named layout
5. Close app → Also saves to "Current Layout.json"
```

### Key Principle

**The Rule:**
- **Current Layout.json** = Always reflects what you see when you close
- **Named layouts** = Only change when you explicitly save to them
- Loading a named layout does NOT link it to auto-save

## Practical Examples

### Example 1: Development Workflow

**Setup:**
```
┌─────────────────────┬─────────────────────┐
│ Code (3 tabs)       │ Docs (2 tabs)       │
│ - GitHub            │ - React Docs        │
│ - GitLab            │ - MDN               │
│ - Local Repo        │                     │
├─────────────────────┴─────────────────────┤
│ Localhost (1 tab)                         │
└───────────────────────────────────────────┘
```

**Save:**
1. Create this layout
2. Right-click → Save Layout As → "Development Workflow.json"

**Use:**
- Load whenever you start coding
- Modify as needed during work
- Save again if you want to update it

### Example 2: Research Projects

**Setup Multiple Layouts:**
```
Project A Research.json:
├─ Academic Papers (5 tabs)
├─ Data Sources (3 tabs)
└─ Notes (Google Docs)

Project B Research.json:
├─ News Articles (4 tabs)
├─ Reference Videos (2 tabs)
└─ Spreadsheet Analysis
```

**Workflow:**
1. Working on Project A → Load "Project A Research.json"
2. Switch to Project B → Load "Project B Research.json"
3. Each loads instantly with all tabs ready

### Example 3: Daily Routines

**Morning Routine:**
```
Morning News.json:
├─ News Sites (5 tabs)
├─ Email (1 tab)
└─ Calendar (1 tab)
```

**Work Routine:**
```
Work Dashboard.json:
├─ Project Management (2 tabs)
├─ Communication (Slack, Teams)
└─ Analytics Dashboard
```

**Evening Routine:**
```
Entertainment.json:
├─ Streaming Services (3 tabs)
├─ Social Media (2 tabs)
└─ Music Player
```

## Tips and Best Practices

### Naming Conventions

**✅ Good names:**
- "Development - Frontend"
- "Research - Machine Learning"
- "Client - ABC Corp"
- "Personal - News & Social"

**❌ Avoid:**
- "Layout1", "Layout2" (not descriptive)
- Very long names (harder to browse)

### Organization Strategy

**Create a folder structure:**
```
%LocalAppData%\DynamicBrowserPanels\Layouts\
├── Work\
│   ├── Development Workflow.json
│   ├── Code Review.json
│   └── Documentation.json
├── Personal\
│   ├── Morning Routine.json
│   └── Entertainment.json
└── Projects\
    ├── Project A.json
    └── Project B.json
```

### Backup Important Layouts

**Recommended:**
1. Copy important layout files to cloud storage
2. Version control them with your projects
3. Share with team members

### Update Layouts Regularly

**When to re-save:**
- Found a better panel arrangement
- Added useful tabs
- Discovered better URLs for reference

## Troubleshooting

### Layout Won't Load

**Issue:** Error loading layout file

**Solutions:**
1. Check file isn't corrupted (open in text editor)
2. Verify JSON is valid
3. Try loading a different layout
4. Delete "Current Layout.json" and restart

### Media Files Not Loading

**Issue:** "Media file not found" errors

**Solution:**
- Files were moved/deleted since layout was saved
- Update paths or re-open media files
- Re-save layout with new paths

### Layout Looks Wrong After Loading

**Issue:** Panels are wrong size or position

**Solutions:**
1. Window size/resolution changed
2. Resize window manually
3. Adjust splitters
4. Re-save layout on current monitor

### Lost My Layout

**Issue:** Made changes and forgot to save

**Solution:**
- Changes are in "Current Layout.json"
- Load it, verify it's what you want
- Save As to preserve it

## Advanced Usage

### Sharing Layouts with Team

1. Save your layout
2. Share the .json file (email, chat, version control)
3. Teammates place in their DynamicBrowserPanels folder
4. They load it via Load Layout menu

### Version Control Layouts

```bash
# In your project repo
mkdir .browser-layouts
cp "%LocalAppData%\DynamicBrowserPanels\Project Layout.json" .browser-layouts/

# Commit to git
git add .browser-layouts/
git commit -m "Add development browser layout"
```

### Template Layouts

**Create base templates:**
1. Empty layout with just splits (no URLs)
2. Save as "Template - 4 Panel.json"
3. Load template when starting new project
4. Add URLs specific to that project
5. Save as new named layout

## Quick Reference

### Menu Options

| Menu Item | Shortcut | Action |
|-----------|----------|--------|
| 💾 Save Layout As... | Right-click URL bar | Save current layout to named file |
| 📂 Load Layout... | Right-click URL bar | Load layout from file |
| Reset Layout | Right-click URL bar | Clear everything, start fresh |

### File Locations

| File | Purpose | Auto-saved? |
|------|---------|-------------|
| Current Layout.json | Active layout | Yes, on close |
| [Name].json | Named layouts | Only when you save |

### Workflow Summary

```
┌─────────────────────────────────────┐
│ 1. Create/modify layout             │
│ 2. Close app                         │
│    → Saves to "Current Layout.json" │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 1. Load named layout                │
│ 2. Use/modify it                    │
│ 3. Want to keep changes?            │
│    → Save Layout As (same name)     │
│ 4. Close app                         │
│    → Saves to "Current Layout.json" │
└─────────────────────────────────────┘
```

## Conclusion

Layout management gives you complete control over your workspace configurations. Use it to:
- Switch between workflows instantly
- Save time recreating panel setups
- Share configurations with others
- Maintain different layouts for different tasks

**Remember:** Current Layout auto-saves, named layouts only change when you explicitly save!
