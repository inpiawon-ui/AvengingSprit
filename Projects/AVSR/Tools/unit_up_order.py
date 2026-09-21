# -*- coding: utf-8 -*-
"""캐릭터 퀄업 **발주서를 찍어 내는 틀**.

37명 × 5방향 = 185장을 손으로 발주서 쓰는 것은 불가능하다. 유닛·방향만 주면
  ① 지금 게임 그림 8칸을 3×3 판으로 붙이고(+3배 확대본)
  ② 그 방향의 **실측 문제**(걷기가 모자라다 · 발이 미끄러진다 …)를 발주서에 박아 넣는다.
문제를 숫자로 적어 주지 않으면 같은 실수가 그대로 돌아온다.

쓰는 법:
  python unit_up_order.py <유닛키> <방향>      # 발주서 한 장
  python unit_up_order.py <유닛키>             # 다섯 방향 전부
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import unit_qa_all as qa   # 문턱·실측을 그대로 쓴다 — 자가 둘이면 안 된다

ROOT = qa.ROOT
UNITS = qa.UNITS
OUT = os.path.join(ROOT, "Projects", "AVSR", "_exchange", "ref", "char_up")
ORIGINAL = os.path.join(ROOT, "Projects", "AVSR", "Reference", "Original")

# 유닛 → 원작 시트. 없는 것은 원작에 없던 캐릭터다(우리가 만든 잡몹·보스 연출용).
SHEET = {
    "amazon": "Enemies - Amazon.png",
    "amazon_elite": "Enemies - Amazon Elite.png",
    "commando_grenade": "Enemies - Commando (Grenade).png",
    "commando_laser": "Enemies - Commando (Laser).png",
    "commando_mg": "Enemies - Commando (Machine Gun).png",
    "commando_missile": "Enemies - Commando (Missiles Pack).png",
    "dragon_blue": "Enemies - Dragon (Blue).png",
    "dragoon": "Enemies - Dragon (Red).png",
    "salamander": "Enemies - Dragon (Green).png",
    "gangster": "Enemies - Gangster (Gun).png",
    "thug": "Enemies - Gangster (Tommy Gun).png",
    "guru": "Enemies - Guru.png",
    "hopper": "Enemies - Hopper (Gun).png",
    "hopper_smg": "Enemies - Hopper (Sub-Machine Gun).png",
    "medium": "Enemies - Magician (Dark).png",
    "white_wizard": "Enemies - Magician (Light).png",
    "ninja_chain": "Enemies - Ninja (Chain).png",
    "ninja": "Enemies - Ninja (Shuriken).png",
    "robot": "Enemies - Robot.png",
    "baseball": "Enemies - Slugger.png",
    "snowwoman": "Enemies - SnowWoman.png",
    "vampire": "Enemies - Vampire.png",
    "ghost": "Playable Characters - Ghost.png",
    "crusher": "Bosses - Crusher.png",
    "guardian": "Bosses - Guardian.png",
    "kingpin": "Bosses - Kingpin.png",
    "python": "Bosses - Python.png",
    "robot_snakes": "Bosses - Robot Snakes.png",
    "sludge": "Bosses - Sludge.png",
}

VIEW = {
    "s":  "정면(보는 사람을 향함) — 두 눈이 다 보이고 양어깨가 좌우로 나란하다",
    "se": "오른쪽 아래 대각선 — 몸이 오른쪽으로 45도 돌아 얼굴 3/4 이 보인다",
    "e":  "오른쪽 옆모습 — 몸이 완전히 오른쪽을 향한다",
    "ne": "오른쪽 위 대각선 — 등을 45도 돌린 뒤태, 얼굴은 거의 안 보인다",
    "n":  "뒷모습 — 등이 보이고 얼굴은 안 보인다",
}


def build_grid(key, facing):
    """지금 게임 그림 8칸 + 빈 칸 하나를 3×3 으로 붙인다."""
    cell = None
    frames = []
    for act in qa.ACTS:
        p = os.path.join(UNITS, key, f"unit_{key}_{facing}{act}.png")
        if not os.path.exists(p):
            return None, None
        im = Image.open(p).convert("RGBA")
        cell = im.size
        frames.append(im)

    w, h = cell
    grid = Image.new("RGBA", (w * 3, h * 3), (255, 0, 255, 255))
    for i, im in enumerate(frames):
        grid.alpha_composite(im, ((i % 3) * w, (i // 3) * h))

    os.makedirs(OUT, exist_ok=True)
    base = os.path.join(OUT, f"src_{key}_{facing}")
    grid.convert("RGB").save(base + ".png")
    grid.convert("RGB").resize((w * 9, h * 9), Image.NEAREST).save(base + "_x3.png")
    return base + ".png", base + "_x3.png"


def problem_lines(key, facing):
    """이 방향의 실측 문제를 사람 말로. 숫자를 그대로 박는다."""
    bad, st = qa.check(key, facing)
    if not st:
        return ["(실측 실패 — 그림이 없다)"]
    lines = []
    if st["걷기"] < qa.WALK_MIN:
        lines.append(
            f"- **걷기 두 칸이 거의 같다**(차이 {st['걷기']}px, 통과선 {qa.WALK_MIN}). "
            f"지금 그림은 다리가 안 움직여 **미끄러지듯 걷는다.** "
            f"한 칸은 왼발이 크게 앞(발끝 사이 30px 이상 벌어짐), 다른 칸은 두 발이 모이고 몸이 3px 솟는다.")
    if st["피격"] < qa.HIT_MIN:
        lines.append(
            f"- **피격 칸이 대기와 거의 같다**(차이 {st['피격']}px, 통과선 {qa.HIT_MIN}). "
            f"몸이 뒤로 크게 젖혀지고 팔이 벌어져야 맞은 것으로 읽힌다.")
    if st["공격"] < qa.ATK_MIN:
        lines.append(
            f"- **공격 두 칸이 거의 같다**(차이 {st['공격']}px, 통과선 {qa.ATK_MIN}). "
            f"둘째 칸은 반동으로 팔·무기가 더 들리고 몸이 뒤로 밀려야 한다.")
    # 죽음(뼈)은 여기서 다루지 않는다 — **모든 발주서에 항상 들어가는 규칙**이다.
    # 색으로 «뼈»를 재면 금발·살색이 뼈로 잡혀 판정이 못 미덥다(아마존 2026-09-21).
    if st["발중심흔들림"] > qa.CENTER_TOL:
        lines.append(
            f"- **발이 칸마다 좌우로 밀린다**({st['발중심흔들림']:.1f}px). "
            f"여덟 칸 모두 **발 중심을 칸 가로 한가운데**에 맞춰라.")
    if st["발줄흔들림"] > qa.FOOT_TOL:
        lines.append(
            f"- **발밑 줄이 칸마다 다르다**({st['발줄흔들림']}px). 걸을 때 위아래로 떤다. "
            f"여덟 칸 모두 발끝이 **칸 아래에서 같은 높이**에 오게 하라.")
    if st["키차이"] > qa.HEIGHT_TOL:
        lines.append(
            f"- **칸마다 키가 다르다**({st['키차이']}px). 커졌다 작아졌다 한다. "
            f"쓰러지는 두 칸 말고는 **머리끝 높이를 맞춰라.**")
    return lines or ["- (숫자상 큰 문제는 없다. 밀도와 입체감만 올려라.)"]


TEMPLATE = """[작업] 게임 캐릭터 픽셀아트 퀄리티 업 — {key} / {view_name} 8자세 한 판

API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성·편집 기능만 써라.
스크립트로 픽셀을 복사·필터·확대해서 때우지 마라. 손으로 그린 픽셀아트로 다시 그려라.

입력(먼저 다 열어 확인해라, 못 열면 멈추고 알려라):
1. {src}     지금 게임 그림 8자세를 3×3 으로 붙인 것.
2. {src_x3}  같은 것 3배 확대(도트 보기용).
3. {anchor}  **이미 통과한 퀄업 기준**(갱스터). 화풍 · 명암 단계 · 외곽선 처리를 여기에 맞춰라.
{sheet_line}
칸 차례(왼쪽 위에서 오른쪽으로, 3칸씩 세 줄):
  1 대기   2 걷기1   3 걷기2
  4 공격1  5 공격2   6 피격
  7 쓰러짐1 8 쓰러짐2  9 **빈 칸**(마젠타만)

⚠⚠ 가장 중요 — **여덟 칸 모두 같은 시점이다: {view_name}.**
   다른 방향으로 그리면 그 납품은 통째로 버린다.
   여덟 칸의 인물은 **같은 사람**이어야 한다 — 복장 · 색 · 체형 · 머리 모양이 칸마다 달라지면 안 된다.
   입력 1번(지금 게임 그림)의 **복장과 색을 그대로 지켜라.** 바꾸는 것은 밀도와 입체감뿐이다.

할 일: 같은 인물 · 같은 복장 · 같은 색으로 두고 **밀도와 입체감만** 올린다.
- 명암 4~5단계. 빛은 왼쪽 위, 그늘은 오른쪽 아래, 밝은 쪽에 얇은 테두리 빛.
- 외곽선은 검정 하나로만 두르지 말고 빛 쪽 · 그늘 쪽 색을 나눈다.
- 얼굴과 무기는 또렷하게. 잔무늬로 지저분하게 만들지 마라.

■ 여덟 칸이 **서로 확실히 달라야 한다** (되풀이되는 실수다 — 매번 여기서 걸린다)
- **4칸(공격1)은 대기와 달라야 한다.** «주먹을 들고 자세만 잡은» 그림은 공격이 아니다.
  팔이나 무기가 **몸 밖으로 뻗어 나가야** 한다(팔꿈치가 펴지고 어깨가 따라 돈다).
  대기와 겹쳐 봤을 때 팔 위치가 눈에 띄게 달라야 한다.
- **5칸(공격2)은 4칸과 달라야 한다.** 반동으로 팔·무기가 더 들리고 몸이 뒤로 밀린다.
- **2·3칸(걷기)은 서로 달라야 한다.** 한 칸은 한쪽 발이 크게 앞, 다른 칸은 두 발이 모이고 몸이 솟는다.
- **6칸(피격)은 대기와 달라야 한다.** 몸이 뒤로 젖혀지고 팔이 벌어진다.

■ 죽음은 **«뼈만 남는다»** — 이 게임의 규칙이다 (7 · 8칸, 예외 없다)
  입력 1번(지금 게임 그림)의 7 · 8칸을 **반드시 열어 보라.** 거기가 정답이다.
  7칸: 살이 해지고 얼굴이 해골로 바뀌며 무릎이 꺾여 쓰러지는 도중.
  8칸: 바닥에 흩어진 **뼈 무더기** — 해골 하나 · 갈비뼈 · 팔다리뼈, 그 사이에 옷·장비 조각.
  ⚠ **그냥 쓰러진 몸을 그리면 그 납품은 못 쓴다.** 되풀이해서 틀리는 자리다.

■ 이 방향에서 **반드시 고쳐야 할 것** (지금 그림을 숫자로 재서 나온 문제다)
{problems}

넣지 마라: 바닥 그림자, 배경 그림, 글자, 칸 구분선, 테두리 상자, 다른 방향 추가.

크기 · 배경
- **정사각 한 장**, 3×3 배치. 한 칸이 정확히 전체의 1/3 이 되게. 크기는 정사각이면 아무거나 좋다(내가 줄인다).
- 배경은 **마젠타 #FF00FF 단색**. 외곽에 반투명 테두리(헤일로)를 남기지 마라.
- 칸마다 인물의 발밑은 칸 아래에서 약 8%, 발 중심은 칸 가로 한가운데.

저장: {dst}
다 되면 저장 경로만 알려라.
"""


def write_order(key, facing):
    src, src_x3 = build_grid(key, facing)
    if src is None:
        print(f"{key} {facing}: 그림이 없다 — 건너뛴다")
        return None

    sheet = SHEET.get(key)
    sheet_line = ""
    if sheet and os.path.exists(os.path.join(ORIGINAL, sheet)):
        sheet_line = (f"4. Projects/AVSR/Reference/Original/{sheet}  "
                      f"1991년 아케이드 원작 시트(느낌의 기준 — 자세는 따라 하지 말고 "
                      f"도트 굵기 · 색 · 외곽선 처리만 참고).\n")

    dst = f"Projects/AVSR/_exchange/ref/char_up/out_{key}_{facing}_raw.png"
    text = TEMPLATE.format(
        key=key,
        view_name=VIEW[facing],
        src=os.path.relpath(src, ROOT).replace("\\", "/"),
        src_x3=os.path.relpath(src_x3, ROOT).replace("\\", "/"),
        anchor="Projects/AVSR/_exchange/ref/char_up/up_front_idle.png",
        sheet_line=sheet_line,
        problems="\n".join(problem_lines(key, facing)),
        dst=dst,
    )
    path = os.path.join(OUT, f"order_{key}_{facing}.txt")
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print(os.path.relpath(path, ROOT).replace("\\", "/"))
    return path


def main():
    key = sys.argv[1]
    facings = [sys.argv[2]] if len(sys.argv) > 2 else qa.FACINGS
    for facing in facings:
        write_order(key, facing)


if __name__ == "__main__":
    main()
