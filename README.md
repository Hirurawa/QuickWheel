# 🎡 QuickWheel

QuickWheel is a lightweight, Windows-only productivity tool that triggers a radial menu (pie menu) at your mouse cursor. It allows for rapid application launching using muscle memory rather than precise clicking.

---

## ✨ Current Features

- **Global Hotkey Hook:** Listens for Tab globally (background process).
- **Input Interception:** "Eats" the trigger key so it doesn't affect active windows (prevents accidental tabbing).
- **Mouse Trap:** Locks the cursor inside the wheel radius while open to prevent accidental clicks outside.
- **Slice Detection:** Mathematically calculates which slice the mouse is in.
- **JSON Configuration:** Loads commands dynamically from `settings.json`.
- **Visual Overlay:** Transparent, borderless WPF window that centers on the cursor.

---

## 📋 Requirements

- Windows 10 or 11
- .NET SDK 8.0 (or 6.0+)

---

## 🚀 Setup & Run

1. Clone/Open the folder in VS Code.

2. **Build:**
   ```bash
   dotnet build
   ```

3. **Run:**
   ```bash
   dotnet run
   ```

> **Note:** The terminal window must stay open for the app to run in this dev version.

---

## ⚙️ Configuration (settings.json)

Modify `settings.json` in the root directory to change shortcuts.

> **Note:** You must restart the app (`Ctrl+C` -> `dotnet run`) to apply changes.

```json
{
  "slices": [
    { 
      "label": "Notepad", 
      "type": "App", 
      "path": "C:\\Windows\\System32\\notepad.exe" 
    },
    { "label": "Chrome", "path": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe" },
    { 
      "label": "Paste Email", 
      "type": "Paste", 
      "data": "myname@example.com" 
    },
    { 
      "label": "Work Tools", 
      "items": [
         { 
             "label": "Paste Signature", 
             "type": "Paste", 
             "data": "Best Regards,\nJohn Doe\nSoftware Engineer" 
         },
         { "label": "VS Code", "path": "code.exe" },
         { "label": "Slack", "path": "slack.exe" },
         { "label": "Calculator", "path": "calc.exe" }
      ]
    },
    { "label": "Explorer", "path": "explorer.exe" },
    { 
      "label": "GitHub", 
      "type": "Web", 
      "path": "https://github.com/pulls" 
    }
  ]
}
```

- **path:** Absolute path to `.exe` or a recognizable command (e.g., `calc.exe`).

---

## 🔧 Troubleshooting

- **Mouse "Jitter":** The cursor trap fights the OS mouse driver ~100 times/second. Slight jitter at the edge is expected behavior.
- **App won't close:** Press `ESC` to force-quit the application and release the mouse hook.

---

## 🗺️ Roadmap (Next Steps)

- [ ] **Icons:** Replace text with file icons or SVGs.
- [ ] **Context Awareness:** Load different JSON profiles based on the active window (e.g., Chrome vs. Excel).
