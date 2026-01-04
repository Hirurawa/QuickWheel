#QuickWheel (MVP)

QuickWheel is a lightweight, Windows-only productivity tool that triggers a radial menu (pie menu) at your mouse cursor. It allows for rapid application launching using muscle memory rather than precise clicking.

Current Features
Global Hotkey Hook: Listens for Tab globally (background process).

Input Interception: "Eats" the trigger key so it doesn't affect active windows (prevents accidental tabbing).

Mouse Trap: Locks the cursor inside the wheel radius while open to prevent accidental clicks outside.

Quadrant Detection: Mathematically calculates 4 slices (Top-Right, Bottom-Right, Bottom-Left, Top-Left).

JSON Configuration: Loads commands dynamically from settings.json.

Visual Overlay: Transparent, borderless WPF window that centers on the cursor.

Requirements
Windows 10 or 11

.NET SDK 8.0 (or 6.0+)

Setup & Run
Clone/Open the folder in VS Code.

Build:

```Bash

dotnet build
```
Run:

```Bash

dotnet run
```
Note: The terminal window must stay open for the app to run in this dev version.

Configuration (settings.json)
Modify settings.json in the root directory to change shortcuts. Note: You must restart the app (Ctrl+C -> dotnet run) to apply changes.

```JSON

{
  "slices": [
    {
      "id": "Top-Right", 
      "label": "Notepad",
      "path": "C:\\Windows\\System32\\notepad.exe", 
      "args": "" 
    },
    {
      "id": "Bottom-Right",
      "label": "Calculator",
      "path": "calc.exe",
      "args": ""
    }
    // ... Top-Left, Bottom-Left
  ]
}
```
id: Must match one of: Top-Right, Bottom-Right, Bottom-Left, Top-Left.

path: Absolute path to .exe or a recognizable command (e.g., calc.exe).

Troubleshooting
Logs not showing: Ensure <OutputType>Exe</OutputType> is set in the .csproj file.

Mouse "Jitter": The cursor trap fights the OS mouse driver ~100 times/second. Slight jitter at the edge is expected behavior.

App won't close: Press ESC to force-quit the application and release the mouse hook.

Roadmap (Next Steps)
[ ] Dynamic UI: Make the text labels (XAML) update automatically based on the JSON file.

[ ] Icons: Replace text with file icons or SVGs.

[ ] Context Awareness: Load different JSON profiles based on the active window (e.g., Chrome vs. Excel).
