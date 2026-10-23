# -*- coding: utf-8 -*-
"""3단계 방 물건 부품 발주 — 쓰레기장 무대(2026-10-23). 시안 mock_room_obj_b.png 화풍 그대로, **한 칸 안에** 그린다.

PD 10-22 「장애물 오브젝트들이 특징도 없고 다 비슷비슷해 · 원작은 다양했다」 · 10-23 「칸 맞춰서」 → 시안 화풍 통과, 부품 발주 「응 그렇게 진행해줘」.
코덱스는 칸 배치를 못 지킨다(메모리 codex-cannot-keep-grid) — 그래서 배치 그림이 아니라 **낱칸 부품 판**만 받는다.
게임 규격: 한 칸 그림 72 x 102(아래 72 x 72 가 발자국 · 위 30 은 솟은 윗면) — 지금 `obj_junkyard_block_1.png` 와 같다.
받으면 판을 칸대로 잘라 `Assets/BaseResource/InGameMainUI/obj_junkyard_*.png` 로(자르는 도구는 받은 뒤).

단계(발주는 단계로): ① 막는 물건 6 · ② 가시 · 울타리 4 → 통과하면 ③ 이어지는 물 타일.
쓰는 법: python order_room_obj_parts.py
실행  : powershell -File Tools/wa-game-harness-kit/files/Tools/harness/run_codex_orders.ps1 -Sheets parts_junkyard_block,parts_junkyard_spike_fence -Exchange Projects/AVSR/_exchange
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
P = 'C:/won/UnityProject/AvengingSprit/'
REF = P + 'Projects/AVSR/_exchange/ref/room_obj_parts/'

HEAD = ('먼저 ' + P + 'Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). '
        '이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. '
        '스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.')

STYLE = f"""■ 화풍 정답(PD 통과) : {P}Projects/AVSR/_exchange/in/mock_room_obj_b.png — 이 화면의 쓰레기장 물건과 **같은 결 · 같은 색 · 같은 굵기**.
  그 화면에서 자른 앵커 : {REF}ref_junk_topleft.png(폐차 · 드럼통) · {REF}ref_junk_topright.png(컨테이너 · 냉장고 · 타이어) ·
  {REF}ref_junk_bottomleft.png · {REF}ref_junk_bottomright.png(타이어 · 드럼통 · 고철 더미)
■ 지금 게임 물건(크기 · 시점 기준) : {P}Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점(지금 방 물건과 같게).
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 발밑 그림자를 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). 칸끼리 마젠타 여백으로 떨어뜨린다.
  **물건은 제 칸 안에만** — 칸 밖으로 한 픽셀도 나가지 않는다. 칸마다 물건 하나, 칸 바닥에 붙여 가운데."""

BLOCK = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 쓰레기장 「막는 물건」 부품 6종 (한 칸짜리)

{STYLE}

■ 하는 일 — 원작(궁수의 전설)에서 옮긴 방의 「막는 칸」(몸 · 탄 모두 막는다)에 하나씩 놓인다. 옆 칸끼리 붙으면 이어진 벽 · 귀퉁이 덩어리가 된다.
  원작은 테마마다 막는 물건 **모양이 통째로 다르다** — 쓰레기장도 서로 다른 실루엣 6가지가 섞여 보여야 한다(같은 네모를 색만 바꾸지 않는다).
■ 6종(칸 차례대로)
  1 압착한 고철 큐브(네모난 압축 덩어리 · 삐져나온 파이프)   2 폐타이어 세 개 쌓은 더미
  3 찌그러진 냉장고(문이 살짝 열림)                         4 녹슨 드럼통 둘을 묶은 것(위험 줄무늬 하나)
  5 폐차 앞부분 토막(보닛 · 깨진 전조등)                      6 부서진 컨테이너 조각(골판 철판 · 녹)
  · 모두 같은 크기 칸 안 — 발자국(아래 72 x 72 칸)을 꽉 채우고 위로 30 만큼 솟는다. 키가 그보다 크면 안 된다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/parts_junkyard_block.png — 가로 3 칸 x 세로 2 칸 판, 칸 하나 288 x 408(게임 72 x 102 의 4배),
  칸 사이 마젠타 여백 32. 위 줄 1 · 2 · 3, 아래 줄 4 · 5 · 6.
끝나면 경로만 알려라.
"""

SPIKE_FENCE = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 쓰레기장 「가시 판 · 울타리」 부품 4종 (한 칸짜리)

{STYLE}

■ 하는 일
  · 가시(원작 가시 칸) — **늘 솟아 있는** 쇠가시. 밟으면 아프다 · 몸도 탄도 지나간다. 바닥에 붙은 판이라 **위로 솟는 높이 없이 72 x 72 발자국 안**에.
    한눈에 「밟으면 아프다」로 읽혀야 한다. 시안의 녹슨 판 위 쇠가시와 같은 것 : {REF}ref_spike.png
  · 울타리(원작 울타리 칸) — 몸은 막고 **탄은 지나간다** — 그래서 틈이 보이는 낮은 철망 · 쇠창살이어야 한다(꽉 막힌 벽처럼 보이면 안 된다). 72 x 102 칸.
■ 4종(칸 차례대로)
  1 가시 판 A — 녹슨 사각 판 위 쇠가시 세 줄          2 가시 판 B — 같은 판, 가시 배치만 조금 다르게(되풀이가 덜 보이게)
  3 울타리 A — 낮은 철망 한 칸(양 끝 쇠기둥)           4 울타리 B — 녹슨 쇠창살 한 칸(위에 철조망 한 줄)
  · 1 · 2 는 칸 아래 72 x 72(4배 288 x 288) 안에만, 그 위 30 줄은 비운다. 3 · 4 는 칸 전체(72 x 102) 안에.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png — 가로 4 칸 x 세로 1 칸 판, 칸 하나 288 x 408(게임 72 x 102 의 4배),
  칸 사이 마젠타 여백 32.
끝나면 경로만 알려라.
"""


WATER = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 쓰레기장 「물(오수 웅덩이)」 타일 한 벌 — 칸끼리 이어 붙는다

{STYLE}

■ 하는 일 — 원작(궁수의 전설)의 물 칸. **몸은 못 건너고 탄은 지나간다.** 방마다 물 칸이 여러 개 이어져 웅덩이 · 강 · 십자 모양이 된다.
  게임은 물 칸마다 「수면」 한 장을 깔고, 옆 칸이 물이 아닌 쪽에만 「가장자리」를 얹고, 두 쪽이 다 땅인 귀퉁이에 「귀퉁이」를 얹는다.
  그래서 수면은 **위 · 아래 · 왼쪽 · 오른쪽 어느 쪽으로 이어 붙여도 이음매가 안 보여야** 한다.
■ 화풍 정답 — 시안의 기름 섞인 오수 : {P}Projects/AVSR/_exchange/in/mock_room_obj_a.png 가운데 웅덩이(어두운 수면 · 녹슨 흙 가장자리 · 기름 무지개 몇 점).
  바닥보다 한 단 낮게 보이게. 수면 반짝임 · 빛은 그리지 않는다(나중에 이펙트로).

■ 10칸(모두 같은 크기 — 게임 한 칸 72 x 72 의 4배 288 x 288)
  1 수면 A — 칸을 꽉 채운 오수 수면(가장자리 없음) · 네 변이 어디로 이어도 맞게
  2 수면 B — 같은 수면, 기름 무늬 · 떠 있는 작은 쓰레기 자리만 다르게
  3 가장자리 위 — 칸 **위쪽 변에만** 녹슨 흙 둑(칸 높이의 약 1/4), 나머지는 마젠타
  4 가장자리 아래 — 아래쪽 변에만          5 가장자리 왼쪽 — 왼쪽 변에만          6 가장자리 오른쪽 — 오른쪽 변에만
  7 귀퉁이 왼쪽 위 — 왼쪽 위 모서리에만(위 · 왼쪽 둑이 만나는 둥근 모서리), 나머지는 마젠타
  8 귀퉁이 오른쪽 위   9 귀퉁이 왼쪽 아래   10 귀퉁이 오른쪽 아래
  · 3~10 의 둑은 수면 위에 얹힌다 — 둑 안쪽 경계가 수면으로 자연스럽게 내려가는 턱으로 보이게. 둑은 같은 두께 · 같은 색.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/parts_junkyard_water.png — 가로 4 칸 x 세로 3 칸 판(1~4 / 5~8 / 9 · 10), 칸 288 x 288, 칸 사이 마젠타 여백 32.
  · **칸 안을 꽉 채우는 것은 1 · 2 수면뿐**이다. 3~10 은 지정한 변 · 모서리에만 그리고 나머지는 마젠타로 비운다.
끝나면 경로만 알려라.
"""


def write(name, prompt):
    ps = ('$env:OPENAI_API_KEY = $null\n'
          'Set-Location "C:\\won\\UnityProject\\AvengingSprit"\n'
          '$prompt = @"\n' + prompt.replace('"@', '" @') + '\n"@\n'
          'codex exec --sandbox workspace-write --skip-git-repo-check $prompt\n')
    path = os.path.join(EX, f'order_{name}.ps1')
    with open(path, 'w', encoding='utf-8-sig') as f:   # PowerShell 5.1 은 BOM 이 있어야 한글을 읽는다
        f.write(ps)
    print(path)


if __name__ == '__main__':
    import sys
    if len(sys.argv) > 1 and sys.argv[1] == 'water':
        write('parts_junkyard_water', WATER)
    else:
        write('parts_junkyard_block', BLOCK)
        write('parts_junkyard_spike_fence', SPIKE_FENCE)
