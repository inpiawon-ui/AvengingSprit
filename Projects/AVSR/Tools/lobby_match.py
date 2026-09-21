"""로비 시안 대조 검수 — 게임 스샷을 시안(lobby_hub_v3_jp.png)과 칸마다 비교한다.

사용: python lobby_match.py <게임 스샷.png> [출력 폴더]
  - 스샷은 9:16 화면에서 찍는다(시안이 941×1672 = 9:16).
  - 칸별 차이 점수(0 = 같음, 평균 절대 차이 0~255)와, 차이를 붉게 칠한 그림을 만든다.
  - 합격선: 칸마다 MAD ≤ PASS_MAD 이고 구조 유사도(SSIM) ≥ PASS_SSIM.
"""
import sys, os, json
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, '..', 'Reference', 'Mockups', 'lobby_hub_v3_jp.png')   # 로비 v4 (2026-09-21)
W, H = 941, 1672

PASS_MAD = 14.0
PASS_SSIM = 0.80

# 칸 — 시안 좌표(941×1672)
REGIONS = {
    'logo': (20, 15, 295, 165),
    'gold_pill': (305, 18, 548, 80),
    'gem_pill': (560, 18, 772, 80),
    'mail_settings': (785, 5, 935, 85),
    'season_pass': (488, 100, 700, 198),
    'event': (713, 100, 918, 198),
    'ghost_box': (33, 248, 333, 510),
    'ghost_scene': (340, 210, 941, 630),
    'chest1': (37, 652, 320, 902),
    'chest2': (332, 652, 610, 902),
    'chest3': (622, 652, 905, 902),
    'mode_label': (28, 918, 280, 970),
    'scenario': (28, 968, 912, 1208),
    'survival': (32, 1212, 465, 1410),
    'defense': (474, 1212, 910, 1410),
    'dots': (400, 1412, 540, 1442),
    'bottom_host': (5, 1474, 301, 1668),
    'bottom_play': (305, 1471, 636, 1670),
    'bottom_shop': (640, 1474, 938, 1668),
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
