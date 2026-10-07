# Gate 10 asset verification

| File | Measured size | Alpha values |
|---|---:|---|
| obj_oneway_frame.png | 216x72 | 0, 255 |
| obj_oneway_bars.png | 216x108 | 0, 255 |
| obj_oneway_lock.png | 40x40 | 0, 255 |
| obj_oneway_arrow.png | 72x72 | 0, 255 |
| preview_gate10.png | 504x180 | 0, 255 |

- Reference: `mock_ch10_oneway.png` lower two doors
- Processing: nearest-neighbor fit, transparent background, alpha thresholded to binary 0/255
- Preview: open assembly at left, closed/locked assembly at right
