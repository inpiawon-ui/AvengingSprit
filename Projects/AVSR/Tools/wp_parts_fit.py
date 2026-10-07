"""무기 속성 7종 퀄업 부품 앉히기 (2026-10-07) — 설계 Projects/AVSR/_exchange/spec_skillfx_weapon.md.

코덱스가 마젠타 바탕에 그려 온 부품 시트(in/wp_*.png 등)를 칸으로 나누고, 바탕을 빼고, 게임 크기로 줄여
Assets/BaseResource/InGameMainUI/fx_{이름}_{번호}.png 로 둔다 — ingamemainui 아틀라스(폴더째)에 들어가고
게임은 `FxFrames("{이름}")` 으로 꺼낸다. 그림은 고치지 않는다(바탕 빼기 · 나누기 · 줄이기 · 자리 잡기만).

예광탄(wptracer)은 탄도선(wpbeam) 셋째 장을 정사각 칸 가운데에 눕혀 둔 것이다 — 탄 상자가 정사각이라
가로 그림을 그대로 넣으면 눌려 보인다.
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lz_parts_fit import key_out  # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

# 이름 → (원본 파일, 칸 수, 칸 하나의 출력 크기(가로, 세로))
PARTS = {
    'wpbeam':  ('wp_beam.png', 4, (1024, 128)),
    'wpcut':   ('wp_cut.png', 4, (192, 192)),
    'wppulse': ('wp_pulse.png', 4, (192, 192)),
    'wpstate': ('wp_state.png', 4, (192, 192)),
    'amland':  ('am_land.png', 4, (256, 128)),
    'hsinv':   ('hs_inv.png', 6, (192, 192)),
    'cmwall':  ('cm_wall.png', 8, (192, 192)),
    'gamark':  ('ga_mark.png', 6, (192, 192)),
    'gaexec':  ('ga_exec.png', 3, (192, 192)),
    'hopaim':  ('hop_aim.png', 8, (192, 192)),
}

TRACER_SIZE = 128


def despill(im):
    """가장자리에 남은 마젠타 기운(보라 테두리)을 걷는다.

    무기 부품은 청록 · 황금 · 담황백뿐이라 「빨강과 파랑이 둘 다 초록보다 훨씬 큰」 픽셀은 바탕이 섞인 것이다.
    빨강 · 파랑을 초록 가까이로 눌러 회색 쪽으로 돌리고, 그만큼 옅게 한다.
    """
    import numpy as np
    a = np.asarray(im).astype(np.float32)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    m = np.minimum(r, b) - g
    tint = (al > 0) & (m > 30)
    cap = g + 30
    a[..., 0] = np.where(tint, np.minimum(r, cap), r)
    a[..., 2] = np.where(tint, np.minimum(b, cap), b)
    a[..., 3] = np.where(tint, al * np.clip(1 - (m - 30) / 120, 0.25, 1), al)
    return Image.fromarray(a.astype(np.uint8), 'RGBA')


def cells(name):
    src, n, size = PARTS[name]
    path = os.path.join(IN, src)
    if not os.path.exists(path):
        return None
    im = despill(key_out(Image.open(path)))
    w, h = im.size
    cw = w // n
    return [im.crop((i * cw, 0, (i + 1) * cw, h)) for i in range(n)], size


def tracer(beam_cells):
    """탄도선 셋째 장(앞이 굵고 밝은 테이퍼)을 정사각 칸 가운데에 눕힌다."""
    cell = beam_cells[2]
    box = cell.getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()
    if box is None:
        return None
    ink = cell.crop(box)
    s = TRACER_SIZE * 0.96 / ink.width
    ink = ink.resize((max(1, round(ink.width * s)), max(2, round(ink.height * s))), Image.LANCZOS)
    out = Image.new('RGBA', (TRACER_SIZE, TRACER_SIZE), (0, 0, 0, 0))
    out.paste(ink, ((TRACER_SIZE - ink.width) // 2, (TRACER_SIZE - ink.height) // 2), ink)
    return out


def main(names):
    for name in names:
        got = cells(name)
        if got is None:
            print('없음', name)
            continue
        cs, size = got
        for i, c in enumerate(cs):
            c.resize(size, Image.LANCZOS).save(os.path.join(OUT, f'fx_{name}_{i + 1}.png'))
        print(name, len(cs), '장', size)
        if name == 'wpbeam':
            t = tracer(cs)
            if t is not None:
                t.save(os.path.join(OUT, 'fx_wptracer_1.png'))
                print('wptracer 1 장', t.size)


if __name__ == '__main__':
    main(sys.argv[1:] or list(PARTS))
