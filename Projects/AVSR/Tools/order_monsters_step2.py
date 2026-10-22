# -*- coding: utf-8 -*-
"""2단계(몬스터 조합) 새 몬스터 생김새 시안 발주문을 만든다 — 라인업 한 장 · 원소판 한 장.

예전 잡몹 라인업(`ref/trash_v2/order_lineup_v2.txt`)과 같은 형식 · 같은 그림체 앵커.
통과하면 몬스터마다 방향 판(3 x 3 · 마젠타, `trash_cut.py`)을 받는다.

쓰는 법: python order_monsters_step2.py
실행  : powershell -File Tools/wa-game-harness-kit/files/Tools/harness/run_codex_orders.ps1 -Sheets mock_lineup_v3,mock_elements_v1 -Exchange Projects/AVSR/_exchange
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
P = 'C:/won/UnityProject/AvengingSprit/'

HEAD = ('이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. '
        '스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.')

STYLE = f"""■ 우리 그림체 (반드시 — 먼저 통과한 잡몹과 같게)
  · 통과한 잡몹 라인업 : {P}Projects/AVSR/_exchange/ref/trash_v2/mock_lineup_v2_labeled.png
  · 지금 게임 캐릭터 4배 : {P}Projects/AVSR/_exchange/ref/trash_v2/ref_game_sprites_x4.png
  · 실제 화면 크기 : {P}Projects/AVSR/_exchange/ref/trash_v2/ref_ingame_scale.png
  · 1991 아케이드 16비트 픽셀 아트. **96 x 96 픽셀 캔버스 안에 그린 크기** — 위 참조와 키 · 굵기가 같아야 한다.
  · 1 픽셀 짙은 외곽선, 한 마리 12~16색, 명암 2~3단 뚝뚝 끊기게(그라데이션 · 잔털 · 질감 노이즈 금지).
  · 밝고 채도 높은 색, 실루엣이 한눈에 읽히는 만화 비례. **사람 모양 금지**(사람 = 빼앗을 수 있는 몸이라 헷갈린다).
  · 몸에 빛 · 아우라 · 반짝이를 굽지 않는다(빛은 게임에서 이펙트로 따로 낸다).
  · 그린 뒤 **4배로 키워(가장 가까운 이웃)** 늘어놓는다. 글자 · 번호를 그리지 마라."""

LINEUP = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 새 잡몹 라인업 3차 시안 (6종, 생김새 정하기)

{STYLE}

■ 6종 — 하는 일이 몸에서 읽혀야 한다
  1 감시 눈 (떠다니는 기계 · 레이저) — 둥근 금속 몸에 **큰 붉은 렌즈 하나**, 짧은 안테나 · 작은 추진구. 렌즈에서 예고선 → 레이저를 쏜다. 바닥에서 조금 떠 있고 밑에 그림자.
  2 그림자 살쾡이 (근접 · 순간이동) — 검보라 그림자 몸의 고양잇과 요괴, 노란 눈 둘, 길고 휜 앞발톱. 꼬리 끝이 연기처럼 흩어진다(사라졌다 나타나는 놈).
  3 톱날 원숭이 (원거리 · 부메랑) — 고철 더미에서 사는 원숭이, 등에 녹슨 **톱날 원반**을 여러 장 메고 한 장을 손에 들었다. 던지면 돌아온다.
  4 미믹 상자 (근접 · 물건인 척) — 방에 있는 나무 상자와 **똑같은 겉모습** : {P}Assets/BaseResource/InGameMainUI/obj_crate_1.png · {P}Assets/BaseResource/InGameMainUI/obj_junkyard_crate_1.png
       두 칸으로 그린다 — (a) 가만히 있는 상자(방 상자와 구별 안 됨) (b) 깨어난 모습 : 뚜껑이 입처럼 벌어져 이빨 · 혀, 밑에 짧은 다리 넷.
  5 수호 등불 (지원 · 방패 걸기) — 푸른 불꽃이 든 낡은 랜턴이 떠다니는 정령. 작은 눈 둘. 곁의 적에게 방패를 씌우는 놈 — 겁 많고 뒤에 숨는 느낌.
  6 혼 사냥개 (근접 · 유령을 쫓음) — 마른 검은 개, 머리는 뼈가 드러난 해골, 눈 · 꼬리 끝이 푸른 혼불 색(빛 번짐 없이 도트 색으로만).

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_lineup_v3.png — 가로형(1672 x 941 쯤), 배경 어두운 회청 단색
  · 두 줄. 모두 오른쪽(동쪽)을 보는 옆모습 · 대기 자세. 미믹 상자는 (a)(b) 두 칸을 나란히.
  · 줄 끝에 크기 비교로 참조의 **해골 · 박쥐 · 갱스터를 같은 배율로** 세운다(새로 그리지 말고 같은 크기 · 같은 그림체).
  · 크기 : 감시 눈 · 수호 등불은 해골보다 조금 작게(떠 있음), 그림자 살쾡이 · 혼 사냥개는 해골 키쯤 · 길게, 톱날 원숭이는 해골쯤, 미믹 상자는 방 상자 크기 그대로.
끝나면 경로만 알려라.
"""

ELEMENTS = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 잡몹 원소판 시안 (같은 몸, 원소만 다르게)

{STYLE}

■ 하는 일 : 같은 잡몹의 **원소판**. 생김새 · 자세 · 크기는 원래 몸과 같고 **색 · 작은 부속만** 바뀐다(한눈에 「불 붙은 고릴라」로 읽히게).
  원래 몸 (게임 그림, 동쪽) :
  · 돌 고릴라 : {P}Assets/BaseResource/Unit/actor_enforcer/unit_actor_enforcer_e.png
  · 고철 전갈 : {P}Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_e.png
  · 해골      : {P}Assets/BaseResource/Unit/skeleton/unit_skeleton_e.png

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_elements_v1.png — 가로형(1672 x 941 쯤), 배경 어두운 회청 단색
  · 세 줄. 줄마다 왼쪽에 **원래 몸**(위 그림을 그대로 4배), 오른쪽에 원소판.
    1줄 돌 고릴라 → **불** : 돌 몸 틈이 용암처럼 주황 · 빨강으로 갈라지고 어깨에 도트 불꽃 몇 개.
    2줄 고철 전갈 → **얼음** : 고철 껍데기에 서리 · 고드름, 꼬리 끝 포신이 얼음 결정.
    3줄 해골 → **독** : 뼈가 연두 · 보라로 물들고 갈비뼈 사이에 독 거품, 발밑에 독 방울 두어 개.
  · 원소판은 원래 몸과 **실루엣이 같아야** 한다(같은 몹이라는 게 읽혀야 한다).
끝나면 경로만 알려라.
"""


LINEUP_V4 = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 잡몹 5종 **겉모습 바꾸기** 시안 (원작 결로)

PD 2026-10-22 「몹도 IP 를 벗어난 느낌」 — 비교 시트 {P}Projects/AVSR/_exchange/ref/ip_compare/ip_compare_sheet.png 의 빨강 5종
(쇠뿔 멧돼지 · 외눈 사마귀 · 갑옷 아르마딜로 · 폭탄 버섯 · 톱날 원숭이)이 원작에 없는 「숲 · 던전 짐승」으로 읽힌다.
**하는 일은 그대로 두고 몸만 원작 결(기계)로 바꾼다.**

■ 원작 결 앵커(반드시 본다)
  · 원작 적 로봇 : {P}Assets/BaseResource/Unit/robot/unit_robot_s.png
  · 원작 보스 크러셔 · 로봇 스네이크 : {P}Assets/BaseResource/Unit/crusher/unit_crusher_s.png · {P}Assets/BaseResource/Unit/robot_snakes/unit_robot_snakes_s.png
  · 원작 느낌으로 통과한 우리 잡몹(순찰기 · 코일 보행기 · 고철 전갈) : {P}Assets/BaseResource/Unit/roadwarden/unit_roadwarden_s.png · {P}Assets/BaseResource/Unit/coilwalker/unit_coilwalker_s.png · {P}Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png
  → 1990년 아케이드의 **굵고 밝은 쇠 · 둥근 장갑 · 붉은 경광등 · 줄무늬 경고 띠** 결. 사람 모양 · 짐승 얼굴 · 털 · 버섯 · 곤충 금지.

{STYLE}

■ 5종 — 하는 일이 몸에서 읽혀야 한다
  1 돌진 쇄석기 (옛 쇠뿔 멧돼지 자리) — 낮고 긴 몸 · 앞에 쇠뿔 모양 파쇄 날 · 무한궤도 · 뒤에 배기통. 평타 없이 **일직선 돌진**만, 벽에 박으면 휘청.
  2 삼각대 저격기 (옛 외눈 사마귀 자리) — 가는 세 다리 · 긴 저격 총열 · 붉은 **외눈 조준 렌즈** 하나. 멀리서 조준선을 긋고 한 발. 감시 눈(둥근 비행 드론)과 다르게 **땅에 서는 키 큰 기계**.
  3 방패 로봇 (옛 갑옷 아르마딜로 자리) — 낮고 넓은 몸 · 정면에 두꺼운 **방패판**(앞에서 친 것을 막는다) · 등 뒤는 얇다. 몸을 접으면 **둥근 쇠공**이 되어 구른다(그 모습도 한 칸).
  4 폭탄 드럼통 (옛 폭탄 버섯 자리) — 녹슨 드럼통 몸 · 짧은 다리 · 머리 위 붉은 경광등 · 위험 줄무늬. 다가와 **깜빡이다 자폭**.
  5 톱날 투척기 (옛 톱날 원숭이 자리) — 고철 몸통 · 한 팔이 **톱날 원반 발사대**, 등에 예비 톱날 둘. 톱날을 던지면 돌아와 다시 끼운다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_lineup_v4.png — 가로형(1672 x 941 쯤), 배경 어두운 회청 단색
  · 위 줄 : 5종 대기 자세(오른쪽을 보는 옆모습), 방패 로봇은 접은 쇠공 모습을 옆에 한 칸 더.
  · 아래 줄 : 크기 · 결 비교로 원작 로봇 · 우리 순찰기 · 코일 보행기 · 해골을 **같은 배율**로(새로 그리지 말고 위 그림 그대로 4배).
  · 크기 : 돌진 쇄석기 · 방패 로봇은 해골보다 낮고 길게, 삼각대 저격기는 해골보다 조금 크게, 폭탄 드럼통 · 톱날 투척기는 해골쯤.
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
    if len(sys.argv) > 1 and sys.argv[1] == 'v4':
        write('mock_lineup_v4', LINEUP_V4)
    else:
        write('mock_lineup_v3', LINEUP)
        write('mock_elements_v1', ELEMENTS)
