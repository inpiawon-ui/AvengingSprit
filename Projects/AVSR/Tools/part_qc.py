"""코덱스가 만든 로비 부품 검수 — 시안에서 잘라 준 원본(src)과 결과(out)를 대조한다.

사용: python part_qc.py <src.png> <out.png> [--text x0,y0,x1,y1 ...] [--sheet 경로]
  --text : 지우라고 한 글자 칸(부품 좌표). 이 칸 밖은 원본과 같아야 한다.

판정 (빠꾸 기준)
  1. 크기가 원본과 같을 것
  2. 불투명한 곳(알파 ≥ 200) 중 글자 칸 밖의 색 차이 MAD ≤ 10 — 부품 모양·색을 바꾸면 안 된다
  3. 글자 칸 안에 원본 글자 색이 남아 있지 않을 것 — 글자와 비슷한 밝은 점 비율 ≤ 2%
  4. 투명 처리: 네 모서리 알파가 0 에 가까울 것
"""
import sys, argparse
import numpy as np
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('src'); ap.add_argument('out')
    ap.add_argument('--text', nargs='*', default=[])
    ap.add_argument('--sheet', default=None)
    ap.add_argument('--no-alpha-check', action='store_true')
    a = ap.parse_args()

    s = Image.open(a.src).convert('RGBA')
    o = Image.open(a.out).convert('RGBA')
    fails = []
    if s.size != o.size:
        fails.append(f'크기 다름 {o.size} ≠ {s.size}')
        o = o.resize(s.size, Image.LANCZOS)

    S = np.asarray(s).astype(np.int16); O = np.asarray(o).astype(np.int16)
    h, w = S.shape[:2]
    text = np.zeros((h, w), bool)
    for t in a.text:
        x0, y0, x1, y1 = map(int, t.split(','))
        text[y0:y1, x0:x1] = True

    opaque = O[..., 3] >= 200
    keep = opaque & ~text
    mad = float(np.abs(S[..., :3] - O[..., :3]).mean(axis=2)[keep].mean()) if keep.any() else 999
    if mad > 10: fails.append(f'글자 밖 색 차이 MAD {mad:.1f} > 10 (모양·색이 바뀜)')

    if text.any():
        # 원본에서 글자 칸의 가장 밝은 10% 를 글자 색으로 본다
        lum_s = S[..., :3].mean(axis=2); lum_o = O[..., :3].mean(axis=2)
        thr = np.percentile(lum_s[text], 90)
        left = float(((lum_o >= thr) & text).sum()) / text.sum()
        if left > 0.02: fails.append(f'글자 칸에 밝은 점 {left*100:.1f}% 남음 (글자가 덜 지워짐)')
    else:
        left = 0.0

    corners = [O[0, 0, 3], O[0, w - 1, 3], O[h - 1, 0, 3], O[h - 1, w - 1, 3]]
    if not a.no_alpha_check and max(corners) > 20:
        fails.append(f'모서리가 투명하지 않음 (알파 {list(map(int, corners))})')
    cover = float(opaque.mean())

    if a.sheet:
        bg = Image.new('RGBA', s.size, (255, 0, 255, 255))
        z = 3
        sheet = Image.new('RGB', (w * z * 3 + 20, h * z), 'white')
        sheet.paste(s.resize((w * z, h * z), Image.NEAREST), (0, 0))
        sheet.paste(Image.alpha_composite(bg, o).convert('RGB').resize((w * z, h * z), Image.NEAREST), (w * z + 10, 0))
        diff = np.clip(np.abs(S[..., :3] - O[..., :3]).mean(axis=2) * 3, 0, 255).astype(np.uint8)
        sheet.paste(Image.fromarray(diff).convert('RGB').resize((w * z, h * z), Image.NEAREST), (w * z * 2 + 20, 0))
        sheet.save(a.sheet)

    print(f'MAD(글자 밖) {mad:.1f} · 글자 잔여 {left*100:.1f}% · 불투명 {cover*100:.0f}% · 모서리 알파 {list(map(int, corners))}')
    print('합격' if not fails else '빠꾸: ' + ' / '.join(fails))
    sys.exit(0 if not fails else 1)


if __name__ == '__main__':
    main()
