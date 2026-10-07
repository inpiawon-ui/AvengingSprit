# Conveyor asset verification

Reference: `mock_ch7_conveyor.png`  
Generation path: built-in image generation (reference-guided), followed only by frame splitting, nearest-neighbor sizing, rotation for the assembly preview, and binary-alpha cleanup.

| File | Measured canvas | Measured alpha values | Result |
|---|---:|---|---|
| `obj_conveyor_1.png` | 72×72 px | 0, 255 | PASS |
| `obj_conveyor_2.png` | 72×72 px | 0, 255 | PASS |
| `obj_conveyor_3.png` | 72×72 px | 0, 255 | PASS |
| `obj_conveyor_4.png` | 72×72 px | 0, 255 | PASS |
| `preview_conveyor.png` | 288×360 px | 0, 255 | PASS |

## Visual checks

- Four right-flow animation phases use the same dark blue-gray ribbed belt, ochre-yellow arrow, steel top/bottom rails, rivets, and dark outline.
- Lighting reads from the upper left.
- Transparent canvas surrounds the low horizontal belt body; no magenta, white, or black backdrop was added.
- `preview_conveyor.png` joins all four source tiles horizontally and reuses them rotated clockwise in a vertical run, matching the game's stated rotation assembly behavior.
- Nearest-neighbor scaling and binary-alpha thresholding preserve hard pixel edges without semi-transparent fringe pixels.
