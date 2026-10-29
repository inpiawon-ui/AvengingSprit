#!/usr/bin/env python3
"""Build and validate the Python side-head sprite and its body-chain review image."""

from __future__ import annotations

import argparse
import json
import shutil
import struct
import subprocess
import sys
from collections import deque
from pathlib import Path

try:
    from PIL import Image
except ImportError:  # The repository runner may not have Pillow installed.
    Image = None


CANVAS = 256
TARGET_BOX = (43, 67, 247, 188)  # inclusive: 205 x 122, center y=127.5
REVIEW_BG = (32, 36, 44, 255)


def find_repo_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / "Assets" / "BaseResource").is_dir():
            return candidate
    raise RuntimeError("Could not locate the repository root")


def ffmpeg_tools() -> tuple[str, str]:
    ffmpeg = shutil.which("ffmpeg")
    ffprobe = shutil.which("ffprobe")
    if not ffmpeg:
        bundled = Path(r"C:\won\tools\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe")
        ffmpeg = str(bundled) if bundled.exists() else None
    if not ffprobe:
        bundled = Path(r"C:\won\tools\ffmpeg-master-latest-win64-gpl\bin\ffprobe.exe")
        ffprobe = str(bundled) if bundled.exists() else None
    if not ffmpeg or not ffprobe:
        raise RuntimeError("Pillow is unavailable and ffmpeg/ffprobe fallback was not found")
    return ffmpeg, ffprobe


def load_rgba(path: Path) -> tuple[int, int, bytearray]:
    if Image is not None:
        with Image.open(path) as im:
            rgba = im.convert("RGBA")
            return rgba.width, rgba.height, bytearray(rgba.tobytes())

    ffmpeg, ffprobe = ffmpeg_tools()
    probe = subprocess.run(
        [ffprobe, "-v", "error", "-select_streams", "v:0", "-show_entries",
         "stream=width,height", "-of", "json", str(path)],
        check=True, capture_output=True, text=True,
    )
    stream = json.loads(probe.stdout)["streams"][0]
    width, height = int(stream["width"]), int(stream["height"])
    decoded = subprocess.run(
        [ffmpeg, "-v", "error", "-i", str(path), "-f", "rawvideo",
         "-pix_fmt", "rgba", "-"],
        check=True, capture_output=True,
    ).stdout
    if len(decoded) != width * height * 4:
        raise RuntimeError(f"Unexpected decoded size for {path}")
    return width, height, bytearray(decoded)


def save_rgba(path: Path, width: int, height: int, pixels: bytearray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if Image is not None:
        Image.frombytes("RGBA", (width, height), bytes(pixels)).save(path)
        return

    ffmpeg, _ = ffmpeg_tools()
    subprocess.run(
        [ffmpeg, "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgba",
         "-s", f"{width}x{height}", "-i", "-", "-frames:v", "1", str(path)],
        input=bytes(pixels), check=True,
    )


def offset(width: int, x: int, y: int) -> int:
    return (y * width + x) * 4


def is_light_neutral(pixels: bytearray, i: int) -> bool:
    r, g, b = pixels[i], pixels[i + 1], pixels[i + 2]
    return min(r, g, b) >= 178 and max(r, g, b) - min(r, g, b) <= 28


def extract_largest_foreground(width: int, height: int, pixels: bytearray) -> bytearray:
    """Flood away a baked light checkerboard, then retain its largest enclosed object."""
    background = bytearray(width * height)
    queue: deque[tuple[int, int]] = deque()

    def seed(x: int, y: int) -> None:
        pos = y * width + x
        if not background[pos] and is_light_neutral(pixels, pos * 4):
            background[pos] = 1
            queue.append((x, y))

    for x in range(width):
        seed(x, 0)
        seed(x, height - 1)
    for y in range(height):
        seed(0, y)
        seed(width - 1, y)

    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height:
                pos = ny * width + nx
                if not background[pos] and is_light_neutral(pixels, pos * 4):
                    background[pos] = 1
                    queue.append((nx, ny))

    candidate = bytearray(1 if not v else 0 for v in background)
    seen = bytearray(width * height)
    largest: list[int] = []
    for start, value in enumerate(candidate):
        if not value or seen[start]:
            continue
        component: list[int] = []
        stack = [start]
        seen[start] = 1
        while stack:
            pos = stack.pop()
            component.append(pos)
            x, y = pos % width, pos // width
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if 0 <= nx < width and 0 <= ny < height:
                    npos = ny * width + nx
                    if candidate[npos] and not seen[npos]:
                        seen[npos] = 1
                        stack.append(npos)
        if len(component) > len(largest):
            largest = component

    mask = bytearray(width * height)
    for pos in largest:
        mask[pos] = 1
    return mask


def alpha_mask(width: int, height: int, pixels: bytearray) -> bytearray:
    return bytearray(1 if pixels[i * 4 + 3] >= 128 else 0 for i in range(width * height))


def mask_bbox(width: int, height: int, mask: bytearray) -> tuple[int, int, int, int]:
    xs, ys = [], []
    for pos, value in enumerate(mask):
        if value:
            xs.append(pos % width)
            ys.append(pos // width)
    if not xs:
        raise RuntimeError("No foreground pixels found")
    return min(xs), min(ys), max(xs), max(ys)


def reference_palette(paths: list[Path]) -> list[tuple[int, int, int]]:
    colors: set[tuple[int, int, int]] = set()
    for path in paths:
        width, height, pixels = load_rgba(path)
        for pos in range(width * height):
            i = pos * 4
            r, g, b, a = pixels[i:i + 4]
            if a < 128 or (r > 200 and b > 200 and g < 100):
                continue
            colors.add((r, g, b))
    if not colors:
        raise RuntimeError("Reference palette is empty")
    return sorted(colors)


def nearest_palette_color(
    rgb: tuple[int, int, int], palette: list[tuple[int, int, int]],
    cache: dict[tuple[int, int, int], tuple[int, int, int]],
) -> tuple[int, int, int]:
    if rgb not in cache:
        r, g, b = rgb
        cache[rgb] = min(
            palette,
            key=lambda c: (r - c[0]) ** 2 + (g - c[1]) ** 2 + (b - c[2]) ** 2,
        )
    return cache[rgb]


def build_head(
    src_width: int, src_height: int, src: bytearray, src_mask: bytearray,
    palette: list[tuple[int, int, int]],
) -> bytearray:
    sx0, sy0, sx1, sy1 = mask_bbox(src_width, src_height, src_mask)
    source_w, source_h = sx1 - sx0 + 1, sy1 - sy0 + 1
    tx0, ty0, tx1, ty1 = TARGET_BOX
    target_w, target_h = tx1 - tx0 + 1, ty1 - ty0 + 1
    output = bytearray(CANVAS * CANVAS * 4)
    cache: dict[tuple[int, int, int], tuple[int, int, int]] = {}

    for ty in range(target_h):
        sy = sy0 + min(source_h - 1, (ty * source_h) // target_h)
        for tx in range(target_w):
            sx = sx0 + min(source_w - 1, (tx * source_w) // target_w)
            if not src_mask[sy * src_width + sx]:
                continue
            si = offset(src_width, sx, sy)
            rgb = nearest_palette_color(tuple(src[si:si + 3]), palette, cache)
            oi = offset(CANVAS, tx0 + tx, ty0 + ty)
            output[oi:oi + 4] = bytes((*rgb, 255))
    return output


def composite(
    dst: bytearray, dst_width: int, dst_height: int,
    src: bytearray, src_width: int, src_height: int, x0: int, y0: int,
) -> None:
    for sy in range(src_height):
        dy = y0 + sy
        if not 0 <= dy < dst_height:
            continue
        for sx in range(src_width):
            dx = x0 + sx
            if not 0 <= dx < dst_width:
                continue
            si = offset(src_width, sx, sy)
            if src[si + 3] < 128:
                continue
            di = offset(dst_width, dx, dy)
            dst[di:di + 4] = src[si:si + 4]


def build_review(head: bytearray, body_path: Path) -> tuple[int, int, bytearray]:
    body_w, body_h, body = load_rgba(body_path)
    review_w, review_h = 944, 192
    review = bytearray(REVIEW_BG * (review_w * review_h))
    body_y = 31
    for x in (20, 260, 500):
        composite(review, review_w, review_h, body, body_w, body_h, x, body_y)
    # The body trails on the left; the head is on the right so its left neck joint overlaps it.
    composite(review, review_w, review_h, head, CANVAS, CANVAS, 674, -32)
    return review_w, review_h, review


def mean_rgb(values: list[tuple[int, int, int]]) -> tuple[int, int, int]:
    if not values:
        raise RuntimeError("Empty measurement region")
    return tuple(round(sum(v[i] for v in values) / len(values)) for i in range(3))


def measure(width: int, height: int, pixels: bytearray) -> dict[str, object]:
    opaque: list[tuple[int, int, int, int, int]] = []
    magenta = 0
    alpha_values: set[int] = set()
    transparent_nonzero_rgb = 0
    for y in range(height):
        for x in range(width):
            i = offset(width, x, y)
            r, g, b, a = pixels[i:i + 4]
            alpha_values.add(a)
            if a:
                opaque.append((x, y, r, g, b))
                if r > 200 and b > 200 and g < 100:
                    magenta += 1
            elif r or g or b:
                transparent_nonzero_rgb += 1
    xs = [p[0] for p in opaque]
    ys = [p[1] for p in opaque]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    dorsal = [(r, g, b) for x, y, r, g, b in opaque if y0 <= y <= 126]
    ventral = [(r, g, b) for x, y, r, g, b in opaque if 160 <= y <= y1]
    return {
        "canvas": [width, height],
        "opaque_x": [x0, x1],
        "opaque_y": [y0, y1],
        "opaque_width": x1 - x0 + 1,
        "opaque_height": y1 - y0 + 1,
        "vertical_center": round((y0 + y1) / 2, 1),
        "magenta_pixels": magenta,
        "dorsal_mean_rgb": list(mean_rgb(dorsal)),
        "ventral_mean_rgb": list(mean_rgb(ventral)),
        "alpha_values": sorted(alpha_values),
        "transparent_nonzero_rgb": transparent_nonzero_rgb,
        "backend": "Pillow" if Image is not None else "ffmpeg fallback",
    }


def validate(metrics: dict[str, object]) -> None:
    x0, x1 = metrics["opaque_x"]
    y0, y1 = metrics["opaque_y"]
    dr, dg, db = metrics["dorsal_mean_rgb"]
    vr, vg, vb = metrics["ventral_mean_rgb"]
    checks = {
        "canvas": metrics["canvas"] == [256, 256],
        "opaque x bounds": 40 <= x0 <= x1 <= 250,
        "opaque width": 180 <= metrics["opaque_width"] <= 210,
        "opaque y bounds": 67 <= y0 <= y1 <= 189,
        "opaque height": 118 <= metrics["opaque_height"] <= 126,
        "vertical center": 125 <= metrics["vertical_center"] <= 131,
        "snout reaches right": x1 >= 240,
        "no magenta": metrics["magenta_pixels"] == 0,
        "binary alpha": set(metrics["alpha_values"]) <= {0, 255},
        "transparent background RGB": metrics["transparent_nonzero_rgb"] == 0,
        "green dorsal average": dg > dr and dg > db,
        "orange ventral average": vr > vg > vb,
    }
    failures = [name for name, passed in checks.items() if not passed]
    if failures:
        raise RuntimeError("Validation failed: " + ", ".join(failures))


def parse_args() -> argparse.Namespace:
    script = Path(__file__).resolve()
    root = find_repo_root(script.parent)
    exchange = root / "Projects" / "AVSR" / "_exchange"
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, default=script.parent / "imagegen_python_side_head_raw.png")
    parser.add_argument("--output", type=Path, default=exchange / "in" / "unit_python_e_cross1.png")
    parser.add_argument("--review", type=Path, default=exchange / "in" / "python_e_cross1_review.png")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    root = find_repo_root(Path(__file__).resolve().parent)
    refs = [
        root / "Assets" / "BaseResource" / "Unit" / "python" / "unit_python_s_out4.png",
        root / "Assets" / "BaseResource" / "InGameMainUI" / "obj_python_body_1.png",
        root / "Assets" / "BaseResource" / "InGameMainUI" / "obj_python_neck_1.png",
    ]
    src_w, src_h, src = load_rgba(args.input)
    src_mask = alpha_mask(src_w, src_h, src)
    if all(src[i * 4 + 3] == 255 for i in range(src_w * src_h)):
        src_mask = extract_largest_foreground(src_w, src_h, src)
    head = build_head(src_w, src_h, src, src_mask, reference_palette(refs))
    save_rgba(args.output, CANVAS, CANVAS, head)
    review_w, review_h, review = build_review(head, refs[1])
    save_rgba(args.review, review_w, review_h, review)

    # Reload final PNG from disk before reporting measurements.
    width, height, saved = load_rgba(args.output)
    metrics = measure(width, height, saved)
    validate(metrics)
    print(json.dumps(metrics, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
