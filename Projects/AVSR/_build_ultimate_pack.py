"""얼티밋 아이콘 12종 앵커 팩.

지난번 이 아이콘들만 유독 코드 도형(다각형+원)으로 납품됐다. 원인은 하나였다 —
**다른 에셋과 달리 앵커(잘라낸 참조 이미지)가 없었다.** 앵커가 없으면 텍스트
프롬프트만 남고, 그러면 도형이 나온다.

그때와 지금이 다른 점: **이미 제대로 그려진 얼티밋 아이콘이 9장 있다.**
스탯 아이콘 같은 대용품이 아니라 목표 그 자체이므로 화풍 앵커로 이보다 나은 것이 없다.
여기에 캐릭터 정체성(우리 유닛 정면)과 색 규율(원작 팔레트)을 붙인다.

  01_style/  이미 완성된 얼티밋 아이콘 9종 — 화풍·밀도·외곽선·조명의 기준
  02_host/   그 호스트의 우리 정면 idle + 원작 팔레트 — 누구의 기술인지
  03_slot/   실제 UI 에서 아이콘이 놓이는 카드 — 크기감·주변 대비

사용: python _build_ultimate_pack.py
산출: _exchange/out/04_ultimate/
"""
import io
import os
import shutil
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _orig_map import SHEET  # noqa: E402

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PANEL = os.path.join(ROOT, 'Assets', 'BaseResource', 'HostSelectPanel')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
ORIG = os.path.join(HERE, '_exchange', 'out', '15_original')
OUT = os.path.join(HERE, '_exchange', 'out', '04_ultimate')

# 이미 잘 나온 아이콘 — 이것이 화풍 기준이다
DONE = ['gangster', 'thug', 'salamander', 'guru', 'white_wizard',
        'ninja', 'robot', 'baseball', 'vampire']

# 이번에 그릴 12종
TODO = ['amazon', 'amazon_elite', 'hopper', 'hopper_smg',
        'commando_mg', 'commando_laser', 'commando_grenade',
        'dragoon', 'dragon_blue', 'medium', 'ninja_chain', 'snowwoman']


def fresh(name):
    p = os.path.join(OUT, name)
    shutil.rmtree(p, ignore_errors=True)
    os.makedirs(p, exist_ok=True)
    return p


def main():
    style = fresh('01_style')
    host = fresh('02_host')
    slot = fresh('03_slot')

    # ── 01 화풍 ─────────────────────────────────────────
    got = []
    for k in DONE:
        src = os.path.join(PANEL, f'ultimateicon_{k}.png')
        if not os.path.exists(src):
            print(f'  화풍 없음 {k}')
            continue
        im = Image.open(src).convert('RGBA')
        im.resize((im.width * 4, im.height * 4), Image.NEAREST).save(
            os.path.join(style, f'{k}_x4.png'))
        got.append((k, im))

    # 한 장에 모아 놓으면 "이 아홉 장이 한 세트로 보이는가" 를 눈으로 잴 수 있다
    if got:
        z, per = 3, 5
        cell = 64 * z
        rows = (len(got) + per - 1) // per
        sheet = Image.new('RGB', (per * (cell + 10) + 10, rows * (cell + 26) + 10),
                          (24, 24, 32))
        d = ImageDraw.Draw(sheet)
        for i, (k, im) in enumerate(got):
            big = im.resize((cell, cell), Image.NEAREST)
            x = (i % per) * (cell + 10) + 10
            y = (i // per) * (cell + 26) + 10
            sheet.paste(big, (x, y), big)
            d.text((x, y + cell + 4), k, fill=(210, 210, 226))
        sheet.save(os.path.join(style, '00_style_sheet.png'))

    # ── 02 정체성 ───────────────────────────────────────
    for k in TODO:
        src = os.path.join(UNIT, k, f'unit_{k}_s.png')
        if os.path.exists(src):
            im = Image.open(src).convert('RGBA')
            im.resize((im.width * 3, im.height * 3), Image.NEAREST).save(
                os.path.join(host, f'{k}__idle_x3.png'))
        else:
            print(f'  정면 없음 {k}')

        pal = os.path.join(ORIG, f'{SHEET.get(k, "")}__palette.png')
        if os.path.exists(pal):
            shutil.copy2(pal, os.path.join(host, f'{k}__palette.png'))
        else:
            print(f'  팔레트 없음 {k}')

    # ── 03 놓이는 자리 ──────────────────────────────────
    card = os.path.join(PANEL, 'ultimatecard.png')
    if os.path.exists(card):
        im = Image.open(card).convert('RGBA')
        im.resize((im.width * 3, im.height * 3), Image.NEAREST).save(
            os.path.join(slot, 'ultimatecard_x3.png'))

    print(f'화풍 {len(got)}장 · 대상 {len(TODO)}종 → {OUT}')


if __name__ == '__main__':
    main()
