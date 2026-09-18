"""로비 시안 대조 검수 — 게임 스샷을 시안(lobby_hub_v2.png)과 칸마다 비교한다.

사용: python lobby_match.py <게임 스샷.png> [출력 폴더]
  - 스샷은 9:16 화면에서 찍는다(시안이 941×1672 = 9:16).
  - 칸별 차이 점수(0 = 같음, 평균 절대 차이 0~255)와, 차이를 붉게 칠한 그림을 만든다.
  - 합격선: 칸마다 MAD ≤ PASS_MAD 이고 구조 유사도(SSIM) ≥ PASS_SSIM.
"""
import sys, os, json
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, '..', 'Reference', 'Mockups', 'lobby_hub_v2.png')
W, H = 941, 1672

PASS_MAD = 14.0
PASS_SSIM = 0.80

# 칸 — 시안 좌표(941×1672)
REGIONS = {
    'logo': (15, 18, 248, 152),
    'gold_pill': (278, 16, 588, 85),
    'gem_pill': (605, 16, 833, 85),
    'mail': (845, 10, 932, 90),
    'season_pass': (500, 108, 705, 202),
    'event': (718, 108, 922, 202),
    'ghost_box': (26, 230, 340, 505),
    'ghost_scene': (340, 230, 941, 600),
    'reward_btn': (640, 495, 915, 592),
    'chest1': (27, 620, 318, 897),
    'chest2': (332, 620, 614, 897),
    'chest3': (626, 620, 914, 897),
    'mode_label': (20, 910, 215, 965),
    'card_left': (35, 965, 268, 1385),
    'card_center': (278, 945, 662, 1390),
    'card_right': (675, 965, 905, 1385),
    'arrows': (0, 1120, 941, 1200),
    'bottom_host': (30, 1485, 310, 1620),
    'bottom_play': (310, 1480, 632, 1622),
    'bottom_shop': (635, 1485, 912, 1620),
}


def ssim(a, b):
    a = a.astype(np.float64); b = b.astype(np.float64)
    c1, c2 = (0.01 * 255) ** 2, (0.03 * 255) ** 2
    ma, mb = a.mean(), b.mean()
    va, vb = a.var(), b.var()
    cov = ((a - ma) * (b - mb)).mean()
    return ((2 * ma * mb + c1) * (2 * cov + c2)) / ((ma * ma + mb * mb + c1) * (va + vb + c2))


def block_ssim(a, b, k=16):
    """작은 칸(k×k)마다 SSIM 을 내서 평균 — 전체 한 번보다 모양 차이에 민감하다."""
    h, w = a.shape
    vals = []
    for y in range(0, h - k + 1, k):
        for x in range(0, w - k + 1, k):
            vals.append(ssim(a[y:y + k, x:x + k], b[y:y + k, x:x + k]))
    return float(np.mean(vals)) if vals else 1.0


def main():
    shot = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.dirname(os.path.abspath(shot))
    m = np.asarray(Image.open(MOCK).convert('RGB').resize((W, H), Image.LANCZOS)).astype(np.int16)
    g = np.asarray(Image.open(shot).convert('RGB').resize((W, H), Image.LANCZOS)).astype(np.int16)
    diff = np.abs(m - g).mean(axis=2)

    gm = np.asarray(Image.fromarray(m.astype(np.uint8)).convert('L'))
    gg = np.asarray(Image.fromarray(g.astype(np.uint8)).convert('L'))

    report = {}
    for name, (x0, y0, x1, y1) in REGIONS.items():
        mad = float(diff[y0:y1, x0:x1].mean())
        s = block_ssim(gm[y0:y1, x0:x1], gg[y0:y1, x0:x1])
        report[name] = dict(mad=round(mad, 1), ssim=round(s, 3),
                            ok=mad <= PASS_MAD and s >= PASS_SSIM)

    # 차이 그림 — 게임 스샷 위에 차이를 붉게, 칸 테두리에 합불 색
    heat = np.clip(diff * 3, 0, 255).astype(np.uint8)
    base = Image.fromarray(g.astype(np.uint8)).convert('RGB')
    red = Image.new('RGB', (W, H), (255, 0, 0))
    img = Image.composite(red, base, Image.fromarray(heat))
    d = ImageDraw.Draw(img)
    for name, (x0, y0, x1, y1) in REGIONS.items():
        d.rectangle((x0, y0, x1, y1), outline=(0, 255, 0) if report[name]['ok'] else (255, 200, 0), width=2)
    img.save(os.path.join(out, 'lobby_match_heat.png'))

    side = Image.new('RGB', (W * 2 + 10, H), 'white')
    side.paste(Image.fromarray(m.astype(np.uint8)), (0, 0))
    side.paste(Image.fromarray(g.astype(np.uint8)), (W + 10, 0))
    side.save(os.path.join(out, 'lobby_match_side.png'))

    total_ok = sum(1 for r in report.values() if r['ok'])
    for k, v in report.items():
        print(f"{'OK ' if v['ok'] else 'NG '} {k:14s} MAD {v['mad']:5.1f}  SSIM {v['ssim']:.3f}")
    print(f'합격 {total_ok}/{len(report)}  전체 MAD {diff.mean():.1f}')
    json.dump(report, open(os.path.join(out, 'lobby_match.json'), 'w'), ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()
