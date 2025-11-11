# Wave Function Collapse: Procedural Dungeon Generator

![Hero GIF: Generation + expansion](gifs/demo.gif)

**Refactored from a personal Unity project (~5 years old)** to showcase WFC for tile-based map gen. Generates connected paths with entrances, mixes paths/deco, supports 7 styles.

## 🎮 Live Demo
[ itch.io / WebGL build ](link-if-you-make-one)

## ✨ Features
- **WFC Core**: Entropy-based collapse + constraint propagation (adjacency via status flags).
- **Map Styles**: Classic (4 entrances), Worlds A-E (varied starts/ends).
- **Expandable**: Click borders to grow (path-only).
- **Borders/Paths**: Auto-seed ends, deco fills gaps.

## 📱 Screenshots
| Classic | World E (Random Entrances) |
|---------|----------------------------|
| ![Classic](screenshots/classic.png) | ![WorldE](screenshots/worle.png) |

## 🚀 Quick Start
1. Clone: `git clone https://github.com/yourusername/WaveFunctionCollapse-DungeonGenerator`
2. Unity Hub > Add > Open folder (2022.3+).
3. Open `WFC_Demo.unity` → Play → Generate!
4. Tweak: Inspector → MapStyle, GridSize.

## 🛠️ Tech
- Unity 2022.3 (C#)
- Custom WFC (no external libs)
- ~500 LOC refactored

## 📈 Lessons Learned
- Propagation is key for coherence.
- Backtracking omitted (uses backups)—fast but simple.

## 🔗 Related
- [Original WFC](https://github.com/mxgmn/WaveFunctionCollapse)
- My .NET Portfolio: [link]

⭐ Star/fork if useful!
