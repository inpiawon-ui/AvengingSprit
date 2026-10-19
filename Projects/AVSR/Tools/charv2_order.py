# -*- coding: utf-8 -*-
"""char_v2 — 원작 기준 다시 그리기 발주서 찍기(구루 샘플 통과 2026-10-19 방식).

호스트마다 원작 메모(`charv2_notes.py` 의 NOTES)를 발주서에 박고, 5방향 판을 한 장씩 코덱스에 보낸다.
  입력 판: 지금 게임 그림 3×3(칸 차례 · 방향만 참고) · 원작 시트 + 3배 확대(몸 · 동작의 정답) · 통과한 구루 판(화풍 기준)
  납품:   Projects/AVSR/_exchange/in/charv2_{키}_{방향}.png
쓰는 법:
  python charv2_order.py <키> [<키> ...]     # 발주서 · 판 · 실행기(run_charv2_{키}.ps1) 만들기
"""
import io
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from charv2_notes import NOTES, SHEET   # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
REF = os.path.join(EX, 'ref', 'char_v2')
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
ORIG = os.path.join(ROOT, 'Projects', 'AVSR', 'Reference', 'Original')
STATES = ['', '_walk1', '_walk2', '_atk1', '_atk2', '_hit', '_die1', '_die2']
VIEW = {
    's': '정면 — 얼굴이 보이고 몸이 화면 쪽을 본다',
    'se': '오른쪽 아래 비스듬히 — 얼굴 3/4 와 몸 오른쪽이 보인다',
    'e': '오른쪽 옆모습 — 얼굴 옆선, 오른쪽을 본다',
    'ne': '오른쪽 위 비스듬히 — 등 3/4, 뒤통수와 오른쪽 어깨가 보인다',
    'n': '뒷모습 — 등이 보이고 얼굴은 안 보인다',
}


def boards(key):
    os.makedirs(REF, exist_ok=True)
    o = Image.open(os.path.join(ORIG, SHEET[key])).convert('RGB')
    o.resize((o.width * 3, o.height * 3), Image.NEAREST).save(os.path.join(REF, f'orig_{key}_x3.png'))
    for d in VIEW:
        cell = 96
        sheet = Image.new('RGBA', (cell * 3, cell * 3), (255, 0, 255, 255))
        for i, st in enumerate(STATES):
            p = os.path.join(UNIT, key, f'unit_{key}_{d}{st}.png')
            if os.path.exists(p):
                im = Image.open(p).convert('RGBA')
                if im.width != cell:
                    im = im.crop(((im.width - cell) // 2, (im.height - cell) // 2,
                                  (im.width + cell) // 2, (im.height + cell) // 2))
                sheet.alpha_composite(im, ((i % 3) * cell, (i // 3) * cell))
        sheet.save(os.path.join(REF, f'src_{key}_{d}.png'))


def order(key, d):
    n = NOTES[key]
    extra = f'5. {n["extra_ref"]}\n' if n.get('extra_ref') else ''
    return f'''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
[작업] 게임 캐릭터 픽셀아트 다시 그리기 — {key}({n["name"]}) / {VIEW[d]} 8자세 한 판

API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성·편집 기능만 써라.
스크립트로 픽셀을 복사·필터·확대해서 때우지 마라. 손으로 그린 픽셀아트로 다시 그려라.

입력(먼저 다 열어 확인해라, 못 열면 멈추고 알려라):
1. Projects/AVSR/Reference/Original/{SHEET[key]}   1991년 아케이드 원작 시트 — **몸 · 얼굴 · 옷 · 색 · 동작의 정답.**
2. Projects/AVSR/_exchange/ref/char_v2/orig_{key}_x3.png   원작 시트 3배 확대(도트 보기용). 칸 이름(Move · Attack · Hurt · Dead 등)이 적혀 있다.
3. Projects/AVSR/_exchange/ref/char_v2/src_{key}_{d}.png   지금 게임 그림 8자세(3×3). **칸 차례 · 방향만** 참고한다 — 몸 · 동작은 따라 그리지 마라(원작과 다르게 그려져 있다).
4. Projects/AVSR/_exchange/in/charv2_guru_{d}.png   PD 가 통과시킨 같은 방향 판(구루) — **화풍 기준**: 명암 단계 · 검은 외곽선 두께 · 도트 밀도 · 칸 속 몸 크기 비율을 이것과 맞춘다.
{extra}
⚠ PD: 「원작을 무시하면 안 된다 · 원본을 참조해 줘」 — 지금 게임 그림은 원작과 다르게 그려졌다. 원작대로 다시 그린다.

■ 원작 몸 · 옷 · 색
{n["body"]}

■ 칸별 동작 (원작 칸을 정답으로)
- 1칸 대기: {n["idle"]}
- 2 · 3칸 이동: {n["move"]}
- 4 · 5칸 공격: {n["attack"]}
- 6칸 맞음: {n["hit"]}
- 7 · 8칸 쓰러짐: 이 게임의 죽음은 «뼈만 남는다» — 7칸 살이 해지며 해골로 바뀌며 무너지는 도중, 8칸 바닥에 흩어진 뼈 무더기 + 옷 · 장비 조각.{n.get("die_extra", "")}

■ 시점 — **여덟 칸 모두 같은 방향: {VIEW[d]}.** 다른 방향이 섞이면 통째로 버린다. 여덟 칸 모두 같은 사람 · 같은 키.
■ 여덟 칸이 서로 확실히 달라야 한다(두 칸이 거의 같으면 버린다).
■ 칸 안에 다 들어가게 — 손 · 발 · 무기 · 꼬리 · 머리 장식이 칸 가장자리에 닿거나 잘리면 안 된다. 칸마다 사방 여백.
■ 총구 불꽃 · 탄 · 불길 · 빛 · 이펙트는 그리지 마라 — 자세만 그린다(이펙트는 게임이 따로 낸다).

칸 차례(왼쪽 위에서 오른쪽으로, 3칸씩 세 줄):
  1 대기   2 이동1   3 이동2
  4 공격1  5 공격2   6 맞음
  7 쓰러짐1 8 쓰러짐2  9 빈 칸(마젠타만)

넣지 마라: 바닥 그림자, 배경, 글자, 칸 구분선, 테두리.
크기 · 배경: 정사각 한 장, 3×3, 한 칸이 정확히 1/3. 배경은 마젠타 #FF00FF 단색, 반투명 헤일로 금지.
※ 바탕이 정확히 #FF00FF 가 아니어도(마젠타 계열 단색이면) · 크기가 달라도(정사각이면) **고치려 하지 말고 그대로 저장하라** — 바탕 빼기 · 크기 맞추기는 우리가 한다. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/charv2_{key}_{d}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''


def main():
    keys = sys.argv[1:]
    for key in keys:
        boards(key)
        for d in VIEW:
            io.open(os.path.join(EX, f'order_charv2_{key}_{d}.ps1'), 'w', encoding='utf-8-sig').write(order(key, d))
        run = f'''Set-Location "C:\\won\\UnityProject\\AvengingSprit"
foreach ($d in @('e','s','se','ne','n')) {{
    $k = "charv2_{key}_$d"
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") {{ "skip $k"; continue }}
    for ($try = 0; $try -lt 2; $try++) {{
        powershell -NoProfile -ExecutionPolicy Bypass -Command "& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'"
        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") {{ break }}
    }}
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") {{ "ok $k" }} else {{ "MISSING $k" }}
}}
'''
        io.open(os.path.join(EX, f'run_charv2_{key}.ps1'), 'w', encoding='utf-8-sig').write(run)
        print('ok', key)


if __name__ == '__main__':
    main()
