"""육성 화면 시안 대조 — 데이터와 무관한 구역(머리 · 탭 · 판 틀 · 제목 · 하단 바)을 칸마다 비교한다.

사용: python growth_match.py   (스샷은 LobbyShotTool 한국어 컷 — growth_*_ko.png)
합격선은 로비(lobby_match.py)와 같다: MAD ≤ 14 · SSIM ≥ 0.80.
⚠ 레벨 · 별 · 이름 · 캐릭터 그림처럼 **데이터가 채우는 칸은 넣지 않는다** — 시안 숫자와 다를 수밖에 없다.
"""
import os
import sys
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from lobby_match import block_ssim, PASS_MAD, PASS_SSIM  # noqa: E402

REF = os.path.join(HERE, '..', 'Reference', 'Mockups', 'growth')
SHOT = os.path.join(os.environ['TEMP'], 'claude')
PAGES = {
    'ghost': ('growth_ghost_stats_v4.png', 'growth_ghost_ko.png', {
        'header': (0, 0, 720, 132), 'tabs': (16, 136, 706, 230), 'card_frame': (16, 232, 706, 250),
        'ghost_art': (24, 250, 284, 466), 'card_labels': (288, 248, 470, 312), 'path_title': (30, 800, 250, 846),
        'path_art': (300, 840, 640, 950), 'nav': (0, 1170, 720, 1280)}),
    'host': ('growth_host_stats_v4.png', 'growth_host_ko.png', {
        'header': (0, 0, 720, 132), 'tabs': (16, 136, 706, 230), 'stat_tabs': (20, 480, 702, 534),
        'stat_header': (30, 538, 250, 576), 'list_header': (16, 790, 706, 832), 'nav': (0, 1170, 720, 1280)}),
    'skill': ('growth_host_skill_v4.png', 'growth_skill_ko.png', {
        'header': (0, 0, 720, 132), 'tabs': (16, 136, 706, 230), 'skill_tabs': (20, 480, 702, 534),
        'skill_frames': (30, 540, 700, 580), 'nav': (0, 1170, 720, 1280)}),
}


def main():
    ok = total = 0
    for page, (mf, gf, regions) in PAGES.items():
        m = np.asarray(Image.open(os.path.join(REF, mf)).convert('RGB')).astype(np.int16)
        g = np.asarray(Image.open(os.path.join(SHOT, gf)).convert('RGB').resize((720, 1280), Image.LANCZOS)).astype(np.int16)
        gm = np.asarray(Image.fromarray(m.astype(np.uint8)).convert('L'))
        gg = np.asarray(Image.fromarray(g.astype(np.uint8)).convert('L'))
        for name, (x0, y0, x1, y1) in regions.items():
            mad = float(np.abs(m[y0:y1, x0:x1] - g[y0:y1, x0:x1]).mean())
            s = block_ssim(gm[y0:y1, x0:x1], gg[y0:y1, x0:x1], k=8)
            good = mad <= PASS_MAD and s >= PASS_SSIM
            ok += good; total += 1
            print(f"{'OK ' if good else 'NG '} {page:5s} {name:12s} MAD {mad:5.1f}  SSIM {s:.3f}")
    print(f'합격 {ok}/{total}')


if __name__ == '__main__':
    main()
