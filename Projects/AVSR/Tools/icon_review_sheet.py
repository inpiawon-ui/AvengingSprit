"""납품 아이콘 검수 시트 (2026-10-22 카드 아이콘 22장 + 제단 2장).

분홍(#FF00FF)을 뚫어 `_exchange/in/<이름>_alpha.png` 로 두고, 어두운 바탕에 게임 크기(카드 160 · 제단 96)와
2배 크기로 늘어놓은 시트를 만든다. 분홍 찌꺼기(가장자리 분홍 픽셀) 수 · 그림이 칸을 채우는 비율도 같이 잰다.
쓰기: python Projects/AVSR/Tools/icon_review_sheet.py
"""
import os
from PIL import Image, ImageDraw, ImageFont
import numpy as np

ROOT = r'C:\won\UnityProject\AvengingSprit'
IN = os.path.join(ROOT, r'Projects\AVSR\_exchange\in')
OUT = os.path.join(ROOT, r'Projects\AVSR\_exchange\plan_content_expand\step1_check')
NAMES = [f'card_icon_c{i:03d}' for i in range(14, 37)] + ['shrine_card_pick', 'shrine_card_up']
LABEL = {
    'c014': '관통탄', 'c015': '벽 반사', 'c016': '유도탄', 'c017': '냉기 각인', 'c018': '저주 각인', 'c019': '화상 번짐',
    'c020': '상태이상 번짐', 'c021': '둔화탄', 'c022': '갑옷 분쇄', 'c023': '연속 압박', 'c024': '마무리 본능',
    'c025': '보스 사냥', 'c026': '폭발 메아리', 'c027': '범위 확대', 'c028': '장판 지속', 'c029': '유령 포대',
    'c030': '영혼 복제', 'c031': '유령 갑주', 'c032': '흡혈', 'c033': '위기 방벽', 'c034': '가속', 'c035': '사거리',
    'c036': '스킬 쿨 감소', 'shrine_card_pick': '제단 · 카드 고르기', 'shrine_card_up': '제단 · 카드 벼리기',
}


def cut(path):
    a = np.array(Image.open(path).convert('RGBA'))
    r, g, b = (a[..., i].astype(int) for i in range(3))
    pink = (r > 190) & (b > 190) & (g < 90)
    a[pink, 3] = 0
    # 남은 분홍기(가장자리에 섞인 분홍) — 0 이 정상
    # 분홍은 빨강 · 파랑이 둘 다 높고 거의 같다 — 보라(파랑이 큼) · 빨강(파랑이 낮음)은 세지 않는다
    # 저주 기운 밝은 보라(218, 90, 252)가 걸려 좁혔다(2026-10-22) — 바탕 분홍은 초록이 거의 0 이고 빨강 = 파랑
    left = ((r > 200) & (b > 200) & (g < 70) & (np.abs(r - b) < 20) & (a[..., 3] > 0)).sum()
    ys, xs = np.nonzero(a[..., 3] > 0)
    fill = 0.0
    if len(xs):
        fill = max(xs.max() - xs.min() + 1, ys.max() - ys.min() + 1) / a.shape[1]
    return Image.fromarray(a), int(left), fill


def shape(img):
    """그림 덩어리의 가로세로비와 짧은 변 비율 — 규격은 가로세로비 1.0 ~ 1.3(09 규칙 2-1절)."""
    al = np.asarray(img.getchannel('A')) > 24
    ys, xs = np.nonzero(al)
    if not len(xs):
        return 0.0, 0.0
    w, h = xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
    return w / h, min(w, h) / max(w, h)


def font(size):
    for f in (r'C:\Windows\Fonts\malgun.ttf', r'C:\Windows\Fonts\gulim.ttc'):
        if os.path.exists(f):
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def main():
    # 정사각 재발주(_sq) > _v2 > 첫 판 — 지금 쓰는 판을 본다
    pick = {}
    for n in NAMES:
        for v in ('_sq', '_v2', '_raw', ''):
            if os.path.exists(os.path.join(IN, n + v + '.png')):
                pick[n] = n + v
                break
    have = [n for n in NAMES if n in pick]
    cell_w, cell_h = 420, 300
    cols = 5
    rows = (len(have) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cell_w, rows * cell_h), (20, 24, 36, 255))
    d = ImageDraw.Draw(sheet)
    f, fs = font(20), font(15)
    report = []
    for i, n in enumerate(have):
        img, left, fill = cut(os.path.join(IN, pick[n] + '.png'))
        img.save(os.path.join(IN, n + '_alpha.png'))
        ratio, tall = shape(img)
        game = 96 if n.startswith('shrine') else 160
        small = img.resize((game, game), Image.NEAREST)
        big = img.resize((game * 2 if game < 120 else 200, game * 2 if game < 120 else 200), Image.NEAREST)
        x, y = (i % cols) * cell_w, (i // cols) * cell_h
        sheet.alpha_composite(big, (x + 10, y + 40))
        sheet.alpha_composite(small, (x + 230, y + 40))
        key = n.replace('card_icon_', '')
        ver = pick[n][len(n):]
        d.text((x + 10, y + 8), f'{key}{ver}  {LABEL.get(key, "")}', font=f, fill=(230, 236, 245))
        warn = []
        if left > 30: warn.append(f'분홍 {left}')
        if fill < 0.7: warn.append(f'작음 {fill:.0%}')
        if ratio > 1.3 or ratio < 1 / 1.3: warn.append(f'비율 {ratio:.2f}')
        d.text((x + 230, y + 210), f'채움 {fill:.0%} · 비율 {ratio:.2f}', font=fs, fill=(150, 160, 180))
        if warn: d.text((x + 230, y + 232), ' · '.join(warn), font=fs, fill=(255, 110, 90))
        report.append(f'{key}{ver}\t비율 {ratio:.2f}\t짧은 변 {tall:.0%}\t분홍 {left}' + ('\t⚠' if warn else ''))
    out = os.path.join(OUT, 'icon_review_sheet.png')
    sheet.save(out)
    print('\n'.join(report))
    print('missing', [n for n in NAMES if n not in have])
    print(out)


if __name__ == '__main__':
    main()
