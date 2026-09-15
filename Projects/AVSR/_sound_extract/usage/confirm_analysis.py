"""무적 확정 실험(보스·근접·캐릭터·빙의·어트랙트) 로그를 한 번에 분석한다.

사용법: python confirm_analysis.py <작업 폴더> <boss|melee|ident|possess|attract|all>
"""
import collections
import glob
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw

Q = re.compile(r"^Q ([\d.]+) cmd=([0-9a-f]{4}) ret=([0-9a-f]{6}) stage=(\d+) area=(\d+) shot=(-?\d+) "
               r"a0=\w+ a5=\w+ a6=\w+ tA6=([0-9a-f]{4}) tA2=([0-9a-f]{4}) host=(\d+)")
H = re.compile(r"^H ([\d.]+) host=(\d+) stage=\d+ shot=(-?\d+)")
P = re.compile(r"^P ([\d.]+) shot=(\d+)")
T = re.compile(r"^T ([\d.]+) enemy=\w+ kind=(\d+)")

FIRE_PERIOD, ON_START, ON_END = 6.0, 0.1, 3.3
RATIO, MIN_COUNT = 4.0, 8


def norm_sfx(cmd):
    n = cmd & 0xff
    return n - 24 if n >= 40 else n


def read(path):
    return open(path, encoding="utf-8", errors="ignore").read().splitlines()


def boss(folder):
    for path in sorted(glob.glob(os.path.join(folder, "boss_*.txt"))):
        name = os.path.basename(path)[:-4]
        sfx = collections.Counter()
        first = {}
        bgm = []
        last_bgm = None
        for line in read(path):
            m = Q.match(line)
            if not m:
                continue
            t, cmd, shot = float(m.group(1)), int(m.group(2), 16), int(m.group(6))
            if cmd >> 8 == 0:
                if cmd not in (0, 0xff) and cmd != last_bgm and t > 1:
                    bgm.append(f"{t:.0f}s:bgm{cmd}")
                last_bgm = cmd
                continue
            n = norm_sfx(cmd)
            if n == 0:
                continue
            key = (n, m.group(3), m.group(7))
            sfx[key] += 1
            if shot >= 0 and key not in first:
                first[key] = shot
        print(f"=== {name}: BGM {bgm}")
        for (n, ret, t6), c in sorted(sfx.items(), key=lambda kv: (-kv[1])):
            print(f"    sfx_{n} @{ret} tA6={t6} x{c} firstShot={first.get((n, ret, t6))}")


def melee(folder):
    for path in sorted(glob.glob(os.path.join(folder, "melee_*.txt")),
                       key=lambda p: [int(x) for x in re.findall(r"\d+", os.path.basename(p))]):
        on, off = collections.Counter(), collections.Counter()
        teleports = 0
        for line in read(path):
            if T.match(line):
                teleports += 1
                continue
            m = Q.match(line)
            if not m or int(m.group(9)) == 0 or int(m.group(2), 16) >> 8 != 2:
                continue
            n = norm_sfx(int(m.group(2), 16))
            if n == 0:
                continue
            phase = float(m.group(1)) % FIRE_PERIOD
            (on if ON_START < phase < ON_END else off)[n] += 1
        attack = [n for n in on if on[n] >= MIN_COUNT and on[n] >= RATIO * max(off[n], 1)]
        verdict = ", ".join(f"sfx_{n} ({on[n]}:{off[n]})" for n in sorted(attack)) or "판정 불가"
        print(f"{os.path.basename(path)[:-4]:16s} teleports={teleports:3d} 공격 관련 {verdict} | ON {dict(on.most_common(6))} | OFF {dict(off.most_common(6))}")


def ident(folder, at_time=45.0):
    frames = {}
    for k in range(1, 27):
        path = os.path.join(folder, f"ident_k{k}.txt")
        if not os.path.exists(path):
            continue
        shot = None
        for line in read(path):
            m = P.match(line)
            if m and abs(float(m.group(1)) - at_time) < 0.5:
                shot = int(m.group(2))
        img = os.path.join(folder, f"snap_ident_k{k}", "avspirit", f"{shot:04d}.png") if shot is not None else None
        if img and os.path.exists(img):
            frames[k] = np.asarray(Image.open(img).convert("RGB")).astype(np.int16)
    if len(frames) < 2:
        print("ident: 스냅샷 부족")
        return
    stack = np.stack(list(frames.values()))
    spread = stack.std(axis=0).sum(axis=2)
    spread[190:, :48] = 0          # HUD 초상화 영역은 몸체가 아니다
    ys, xs = np.nonzero(spread > 40)
    if ys.size == 0:
        print("ident: 번호별 차이가 없음 — 몸체가 바뀌지 않았거나 화면 밖")
        return
    y0, y1 = max(int(np.percentile(ys, 2)) - 6, 0), min(int(np.percentile(ys, 98)) + 6, 223)
    x0, x1 = max(int(np.percentile(xs, 2)) - 6, 0), min(int(np.percentile(xs, 98)) + 6, 255)
    print(f"ident: 차이 영역 x{x0}-{x1} y{y0}-{y1} (픽셀 {ys.size})")
    scale = 4
    cw, ch = (x1 - x0) * scale, (y1 - y0) * scale
    cols = 7
    sheet = Image.new("RGB", (cols * (cw + 10), ((len(frames) + cols - 1) // cols) * (ch + 24)), (30, 30, 30))
    d = ImageDraw.Draw(sheet)
    for i, (k, arr) in enumerate(sorted(frames.items())):
        crop = Image.fromarray(arr[y0:y1, x0:x1].astype(np.uint8)).resize((cw, ch), Image.NEAREST)
        x, y = (i % cols) * (cw + 10), (i // cols) * (ch + 24)
        sheet.paste(crop, (x, y + 20))
        d.text((x + 4, y + 4), f"host {k}", fill=(255, 255, 0))
    out = os.path.join(folder, "ident_bodies.png")
    sheet.save(out)
    print("ident montage:", out, sheet.size)


def possess(folder, window=0.3):
    near = {"possess": collections.Counter(), "lose": collections.Counter(), "switch": collections.Counter()}
    counts = collections.Counter()
    silent = collections.Counter()
    for path in sorted(glob.glob(os.path.join(folder, "possess_*.txt"))):
        sounds, changes, prev = [], [], None
        for line in read(path):
            m = H.match(line)
            if m:
                t, host = float(m.group(1)), int(m.group(2))
                if prev is not None and t > 3:
                    kind = "possess" if prev == 0 and host else "lose" if host == 0 else "switch"
                    changes.append((t, kind))
                prev = host
                continue
            m = Q.match(line)
            if m and int(m.group(2), 16) >> 8 == 2 and norm_sfx(int(m.group(2), 16)):
                sounds.append((float(m.group(1)), norm_sfx(int(m.group(2), 16))))
        for t, kind in changes:
            counts[kind] += 1
            hits = [n for st, n in sounds if t - window <= st <= t + window]
            if not hits:
                silent[kind] += 1
            for n in hits:
                near[kind][n] += 1
    for kind in near:
        print(f"{kind}: 전환 {counts[kind]}회, 전후 {window}s 무음 {silent[kind]}회, 겹친 소리 {dict(near[kind].most_common(6))}")


def attract(folder):
    path = os.path.join(folder, "attract.txt")
    last = None
    for line in read(path):
        m = Q.match(line)
        if not m:
            continue
        cmd = int(m.group(2), 16)
        if cmd >> 8 == 0 and cmd != last and float(m.group(1)) > 0.5:
            print(f"  {float(m.group(1)):7.2f}s bgm cmd={cmd:#06x} shot={m.group(6)} stage={m.group(4)}")
        if cmd >> 8 == 0:
            last = cmd


def main(folder, what):
    tasks = {"boss": boss, "melee": melee, "ident": ident, "possess": possess, "attract": attract}
    for name, fn in tasks.items():
        if what in (name, "all"):
            print(f"########## {name} ##########")
            fn(folder)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
