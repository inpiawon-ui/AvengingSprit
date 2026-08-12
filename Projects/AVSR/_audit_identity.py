"""캐릭터 22종 — **프레임끼리 같은 사람인가**를 눈으로 보기 위한 대조 시트.

지금까지 검수기가 못 잡은 구멍이다. 캔버스·앵커·팔레트·구멍은 다 봤는데
"idle 과 atk 가 같은 캐릭터인가" 는 한 번도 안 봤다. 실제로 아마존은
idle·walk 에 있던 분홍 팔목보호대가 atk 에서 사라지고 hit 은 체구가 다르다.

수치로는 안 잡힌다 —
 · 팔레트: 색은 그대로 쓰고 몸만 다르게 그리므로 통과한다
 · 몸집: 공격 자세는 팔을 뻗어 원래 커진다. 정상 변화와 구분되지 않는다
그래서 **한 줄에 8프레임을 늘어놓고 사람이 본다.** 이 스크립트는 그 판을 깐다.

곁들여 수치 보조자료도 같이 찍는다(면적비·색수). 눈으로 본 것을 뒷받침할 때 쓴다.

사용: python _audit_identity.py
산출: _exchange/out/18_identity/{슬러그}.png  + 00_summary.md
"""
import io
import os
import shutil
import sys

from PIL import Image, ImageDraw

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
OUT = os.path.join(HERE, '_exchange', 'out', '18_identity')

FRAMES = [None, 'walk1', 'walk2', 'atk1', 'atk2', 'hit', 'die1', 'die2']
ZOOM = 3
BOSSES = {'robot_snakes', 'demolisher', 'python'}   # 캔버스가 달라 따로 본다


def stats(im):
    px = im.load()
    n = 0
    cols = set()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a >= 250:
                n += 1
                cols.add((r, g, b))
    return n, len(cols)


def main():
    shutil.rmtree(OUT, ignore_errors=True)
    os.makedirs(OUT, exist_ok=True)

    lines = ['# 프레임 일관성 대조 — 수치 보조자료', '',
             '눈으로 본 판정이 본체다. 아래는 뒷받침용이다.', '',
             '| 캐릭터 | idle 화소 | walk1 | walk2 | atk1 | atk2 | hit | idle 색수 |',
             '|---|--:|--:|--:|--:|--:|--:|--:|']

    for key in sorted(os.listdir(UNIT)):
        d = os.path.join(UNIT, key)
        base = os.path.join(d, f'unit_{key}_s.png')
        if not os.path.isdir(d) or not os.path.exists(base):
            continue

        idle = Image.open(base).convert('RGBA')
        size = idle.width
        bn, bc = stats(idle)
        cell = size * ZOOM

        # 보스는 프레임 이름이 다르다(walk 대신 move)
        frames = ([None, 'move1', 'move2', 'atk1', 'atk2', 'hit', 'die1', 'die2']
                  if key in BOSSES else FRAMES)

        sheet = Image.new('RGB', (len(frames) * (cell + 6) + 6, cell + 34), (24, 24, 32))
        dr = ImageDraw.Draw(sheet)
        dr.text((6, 6), key, fill=(255, 220, 120))

        ratios = []
        for i, f in enumerate(frames):
            p = base if f is None else os.path.join(d, f'unit_{key}_s_{f}.png')
            x = i * (cell + 6) + 6
            if not os.path.exists(p):
                dr.text((x, cell // 2), '없음', fill=(200, 90, 90))
                if f in ('walk1', 'walk2', 'move1', 'move2', 'atk1', 'atk2', 'hit'):
                    ratios.append(0.0)
                continue
            im = Image.open(p).convert('RGBA')
            n, _ = stats(im)
            big = im.resize((cell, cell), Image.NEAREST)
            sheet.paste(big, (x, 22), big)
            dr.text((x, cell + 24), f or 'idle', fill=(210, 210, 226))
            if f in ('walk1', 'walk2', 'move1', 'move2', 'atk1', 'atk2', 'hit'):
                ratios.append(n / max(bn, 1))

        sheet.save(os.path.join(OUT, f'{key}.png'))
        r = (ratios + [0] * 5)[:5]
        lines.append(f'| {key} | {bn} | ' + ' | '.join(f'{v:.2f}' for v in r) + f' | {bc} |')
        print(f'{key:<18} {len(frames)}프레임 → {key}.png')

    io.open(os.path.join(OUT, '00_summary.md'), 'w', encoding='utf-8').write(
        '\n'.join(lines) + '\n')
    print(f'\n→ {OUT}')


if __name__ == '__main__':
    main()
