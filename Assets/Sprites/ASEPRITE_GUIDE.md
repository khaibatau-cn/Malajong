# 🀄 Malajong — Aseprite Pixel Art Specification & Guide

> **Target Engine**: Unity 6 (2D URP)  
> **Team**: SanRokuNana / CodeCatalyst  
> **Art Style**: Chunky Balatro-inspired 2D Pixel Art with modern shaders  

---

## 1. Canvas & Grid Setup in Aseprite

* **Grid Size**: `32 x 32 px` (or `32 x 44 px` for tall rectangular tiles)
* **Color Mode**: `Indexed` or `RGBA (32-bit)`
* **Color Palette**: Endesga 32 / Pico-8 / Custom Malajong Palette (below)

### Spritesheet Master Grid (`sheet.png` / `MahjongTiles.aseprite`)
| Row (0-indexed) | Items | Pixel Inset / Notes |
|---|---|---|
| **Row 0** | Character 1–9 (萬) | Bold red/black kanji on ivory face |
| **Row 1** | Bamboo 1–9 (索) | Green/red bamboo sticks (1-Bamboo = Bird/Peacock) |
| **Row 2** | Dots 1–9 (筒) | Multi-ring floral circles |
| **Row 3 (Col 0–3)** | 4 Winds (東, 南, 西, 北) | Blue/slate wind kanji |
| **Row 3 (Col 4–6)** | 3 Dragons (中, 發, 白) | Red Dragon, Green Dragon, Blank/Beveled Face |
| **Row 3 (Col 7–8)** | Tile Back & Blank Base | Royal blue back `#1E3A8A` / Jade green back `#065F46` |
| **Row 4 (Col 0–7)** | 4 Flowers & 4 Seasons | Plum, Orchid, Bamboo, Chrysanthemum / Spring, Summer, Fall, Winter |
| **Row 5 (Col 0–7)** | Artifact / Spirit Icons | 8+ unique pixel badges (Bamboo Vow, Compass, etc.) |

---

## 2. Official Color Palette

| Element | Hex Code | Visual Sample |
|---|---|---|
| **Tile Face Base** | `#F6F4EB` | Warm ivory card face |
| **Tile Shadow / Bevel** | `#C4BFAC` | Bottom and right bevel shadow |
| **Bamboo Green** | `#2ECC71` / `#16A34A` | Crisp emerald green |
| **Characters Red** | `#E74C3C` / `#DC2626` | Deep vermillion red |
| **Dots Blue** | `#3498DB` / `#2563EB` | Cobalt blue |
| **Gold / Honors** | `#F1C40F` / `#D97706` | Rich warm yellow/gold |
| **Tile Back Classic** | `#1E3A8A` | Royal Navy Blue |

---

## 3. Unity Auto-Import Settings

Unity handles pixel art best with these texture settings (already automated via `MahjongAssetWiringTool.cs`):
- **Texture Type**: `Sprite (2D and UI)`
- **Sprite Mode**: `Multiple`
- **Pixels Per Unit**: `32`
- **Filter Mode**: `Point (no filter)`
- **Compression**: `None / High Quality`

---

## 4. Unity Menu Shortcuts

In Unity's top toolbar:
- **`Malajong -> Auto-Slice Spritesheet and Wire Tiles`**: Auto-slices `sheet.png` and assigns sprites to all 34 `TileData` ScriptableObjects.
- **`Malajong -> Setup Playable Scene Placeholder`**: Rebuilds the entire playable UI, assigns managers, wires buttons, and links all 8 Artifacts.
