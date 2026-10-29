# 인게임 배경 심플화 — 적용 전 · 후 안드로이드 텍스처 용량 표 (2026-10-29)
# 전 = git 기준 커밋(기본 HEAD)의 그림 · 메타, 후 = 지금 작업 폴더.
# 아틀라스 밖(방 바닥 · 옆벽 · 방 밖)은 그림마다 「가로 x 세로 x 형식 비트」, 아틀라스 칸 그림은 면적 합 x 아틀라스 형식 x 여백 1.3.
# 쓰는 법: python Projects/AVSR/Tools/bg_simple_size.py [기준 커밋]
import os
import re
import subprocess
import sys
import struct

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
REF = sys.argv[1] if len(sys.argv) > 1 else 'HEAD'
RF = 'Assets/BundleResource/RoomFloor'
UI = 'Assets/BaseResource/InGameMainUI'
STAGES = ['junkyard', 'missile', 'street', 'rooftop', 'lab', 'refinery']
BOSSES = ['robot_snakes', 'crusher', 'python', 'sludge', 'guardian', 'kingpin']
BPP = {48: 8.0, 49: 5.12, 50: 3.56, 51: 2.0, 4: 32.0, -1: 32.0}   # TextureImporterFormat → 비트/점
PARTS = [f'water_{n}' for n in ('1', '2', 'edge_n', 'edge_s', 'edge_w', 'edge_e',
                                'corner_nw', 'corner_ne', 'corner_sw', 'corner_se')] + ['spike_1', 'spike_2', 'fence_1', 'fence_2']


def blob(path, ref):
    if ref is None:
        p = os.path.join(ROOT, path)
        return open(p, 'rb').read() if os.path.exists(p) else None
    r = subprocess.run(['git', '-C', ROOT, 'show', f'{ref}:{path}'], capture_output=True)
    return r.stdout if r.returncode == 0 else None


def size(png):
    return struct.unpack('>II', png[16:24])


def android_bpp(meta):
    m = meta.decode('utf-8', 'ignore')
    blk = m.split('buildTarget: Android')
    if len(blk) > 1 and re.search(r'overridden: 1', blk[1].split('- serializedVersion')[0]):
        return BPP.get(int(re.search(r'textureFormat: (-?\d+)', blk[1]).group(1)), 32.0)
    return 32.0


def big_files():
    names = [f'roomfloor_env_{s}' for s in STAGES] + [f'roomside_{s}' for s in STAGES] + [f'roomapron_{s}' for s in STAGES]
    names += [f'roomfloor_{b}' for b in BOSSES] + [f'roomapron_boss_{b}' for b in BOSSES]
    names += [f'{k}_{s}v2' for s in STAGES for k in ('roomfloor_env', 'roomside', 'roomapron')]
    return [f'{RF}/{n}.png' for n in names]


def cell_files(ref):
    names = []
    for s in STAGES:
        names += [f'obj_{s}_block_{i}' for i in range(1, 7)] + [f'obj_{s}_{p}' for p in PARTS]
        names += [f'obj_{s}v2_block_{i}' for i in range(1, 6)] + [f'obj_{s}v2_{p}' for p in PARTS]
    return [f'{UI}/{n}.png' for n in names]


def measure(ref):
    big_mb, big_n, peak = 0.0, 0, 0.0
    for p in big_files():
        png, meta = blob(p, ref), blob(p + '.meta', ref)
        if png is None or meta is None:
            continue
        w, h = size(png)
        mb = w * h * android_bpp(meta) / 8 / 1048576
        big_mb += mb
        big_n += 1
        if '/roomfloor_' in p:
            peak = max(peak, mb)
    area, cn = 0, 0
    for p in cell_files(ref):
        png = blob(p, ref)
        if png is None:
            continue
        w, h = size(png)
        area += w * h
        cn += 1
    cell_mb = area * 8 / 8 * 1.3 / 1048576   # 인게임 아틀라스 = ASTC 4x4(8 비트/점)
    return big_n, big_mb, peak, cn, cell_mb


if __name__ == '__main__':
    for label, ref in (('적용 전(' + REF + ')', REF), ('적용 후(작업 폴더)', None)):
        n, mb, peak, cn, cmb = measure(ref)
        print(f'{label}\t큰 그림 {n}장 {mb:.2f}MB\t바닥 한 장 최대 {peak:.2f}MB\t아틀라스 칸 그림 {cn}장 약 {cmb:.2f}MB')
