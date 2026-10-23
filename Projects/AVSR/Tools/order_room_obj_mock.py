# -*- coding: utf-8 -*-
"""3단계 방 물건 시안 — 원작(궁수의 전설)에서 옮긴 방 둘을 쓰레기장 무대 완성 화면으로 (코덱스 자유 시안, 2026-10-22).

PD 10-22 「장애물 오브젝트들이 특징도 없고 다 비슷비슷해 · 원작은 다양했다 · 기믹에 맞춰 다양한 오브젝트를」.
원작 조사(`step3_research/origin_obstacles.md`): 원작은 규칙 6가지(막는 블록 · 물 · 울타리 · 가시 · 레일 톱 · 풀)뿐이고,
테마마다 **막는 물건 모양이 통째로 다르다**(초원 나무 · 그루터기 · 상자 / 사막 사암 · 선인장 · 야자수 …). 물은 이어지는 타일.
우리는 무대 6곳이 같은 모양 11가지를 색만 바꿔 쓴다(`room_objects.png`) — 이것을 버리고 쓰레기장만의 모양을 새로 정한다.

쓰는 법: python order_room_obj_mock.py
실행  : powershell -File Tools/wa-game-harness-kit/files/Tools/harness/run_codex_orders.ps1 -Sheets mock_room_obj_a,mock_room_obj_b -Exchange Projects/AVSR/_exchange
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
P = 'C:/won/UnityProject/AvengingSprit/'

HEAD = ('먼저 ' + P + 'Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). '
        '이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. '
        '스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.')

COMMON = f"""■ 하는 일 — 방 구조(칸)는 궁수의 전설 원작 그대로 옮겨 왔다. **네 몫은 이 방을 쓰레기장 무대의 물건으로 보이게 하는 것뿐**이다.
  · 칸 하나 = 물건 하나. 화면 폭 720 px 에 가로 11 칸(한 칸 약 65 px). 아래 글자 그림의 칸을 한 칸도 옮기지 않는다.
  · # 막는 물건 — 몸 · 탄 모두 막는다. **쓰레기장만의 서로 다른 모양 4~6 가지**를 섞어 놓는다(같은 네모를 색만 바꾸지 않는다).
      예를 고르는 것은 네 몫이다 — 압착한 고철 덩어리 · 폐타이어 더미 · 찌그러진 냉장고 · 드럼통 묶음 · 폐차 앞부분 · 부서진 컨테이너 조각 같은 것.
      한 칸짜리 물건끼리 붙으면 이어진 벽처럼 보여도 좋다(원작 귀퉁이 덩어리처럼).
  · ~ 물 — 몸은 못 건너고 탄은 지나간다. **여러 칸이 이어지는 웅덩이**(기름 섞인 오수 · 녹슨 가장자리) — 가장자리 · 귀퉁이가 이어지는 타일 모양.
      바닥보다 한 단 낮게, 수면은 어둡게. 빛 · 반짝이를 굽지 않는다(반짝임은 나중에 이펙트로).
  · ^ 가시 — 밟으면 아프다(몸 · 탄 모두 통과). 바닥에 붙은 낮은 쇠가시 판 · 철조망. 한눈에 「밟으면 아프다」로 읽히게.
  · o 기타 — 작은 막는 소품 하나(소화전 · 표지판 같은 것).
  · r 원거리 · m 근접 · f 비행 몬스터 자리 — 몬스터는 이 그림 그대로, 같은 배율:
      고철 전갈(원거리) {P}Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png ·
      해골(근접) {P}Assets/BaseResource/Unit/skeleton/unit_skeleton_s.png · 박쥐(비행) {P}Assets/BaseResource/Unit/bat/unit_bat_s.png
      플레이어(갱스터)는 맨 아래 가운데.

■ 바탕 — 지금 게임 화면(HUD · 조이스틱 · 버튼 · 방 바닥 · 벽은 그대로 둔다) : {P}Projects/AVSR/_exchange/ref/room_tpl_mock/base_room_now.png
■ 참고 — 원작이 테마마다 물건 모양을 얼마나 다르게 썼나(모양 · 결을 베끼지 말고 「다양함」만 본다) : {P}Projects/AVSR/_exchange/plan_content_expand/step3_research/origin_obstacles.png
■ 버릴 것 — 지금 우리 물건(무대마다 같은 모양, 색만 다름) : {P}Projects/AVSR/_exchange/plan_content_expand/step3_research/room_objects.png
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 물건에 빛 · 아우라를 굽지 않는다. 지금 화면 몬스터 · 바닥과 같은 결."""

TPL_A = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 A 「물 웅덩이 방」(원작 방 그대로 · 쓰레기장 무대)

{COMMON}

■ 이 방(글자 그림 — 위가 문, 아래가 입구. 세로 21 칸이라 화면에는 아래쪽 15 칸쯤만 보여도 된다 — 카메라가 따라 올라간다)
  ~.........~
  ~.f.....f.~
  ~.........~
  ...........
  o....f....r
  ...........
  ...........
  ...........
  ...~~~~~...
  ...~~~~~...
  ...~~r~~...
  ...~~~~~...
  ...~~~~~...
  ...........
  ...........
  ...........
  f....f....o
  ...........
  ~.........~
  ~.........~
  ~.........~
  · 가운데 큰 웅덩이 속 섬(r)에 고철 전갈이 서서 건너편으로 쏜다 — 「물로 갈라 원거리끼리 쏘게」가 이 방의 재미다. 섬이 한눈에 보여야 한다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_room_obj_a.png — 720 x 1280, 바탕 화면과 같은 구도 · 같은 픽셀 크기.
  네가 이 게임 아트 디렉터라면 쓰레기장을 어떻게 꾸미겠나 — 칸은 지키고 물건 모양 · 결 · 깊이 · 그림자는 자유.
끝나면 경로만 알려라.
"""

TPL_B = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 B 「가시 고리 방」(원작 방 그대로 · 쓰레기장 무대)

{COMMON}

■ 이 방(글자 그림 — 위가 문, 아래가 입구. 세로 21 칸 — 화면에는 아래쪽 15 칸쯤만 보여도 된다)
  ###..r..###
  #.........#
  #.........#
  ^.r^.^.^r.^
  ...........
  .....r.....
  ...........
  ..f.^^^.f..
  ...........
  ..^.~~~.^..
  ..^.~r~.^..
  ..^.~~~.^..
  ...........
  ..f.^^^.m..
  ...........
  ...........
  ...........
  #.^.^.^.^.#
  #.........#
  #.........#
  ###.....###
  · 네 귀퉁이 ㄱ 자 덩어리(#) · 가시 줄과 가시 고리 · 가운데 작은 물 섬 — 「가시로 길을 좁히고 섬 위에서 쏜다」가 이 방의 재미다.
  · 막는 물건 4~6 가지가 이 방에서 다 보이게 섞어 놓는다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_room_obj_b.png — 720 x 1280, 바탕 화면과 같은 구도 · 같은 픽셀 크기.
  네가 이 게임 아트 디렉터라면 쓰레기장을 어떻게 꾸미겠나 — 칸은 지키고 물건 모양 · 결 · 깊이 · 그림자는 자유.
끝나면 경로만 알려라.
"""


# PD 10-23 「여기 캐릭터가 지나갈 수 있어? 간격이 애매해 보이는데」 → 「칸 맞춰서 다시 받아줘」.
# B 는 가시를 물 웅덩이에 바짝 붙이고 칸 밖으로 넓게 그려 원작의 빈 줄(지나가는 길)이 사라졌다.
# 화풍은 B 그대로 두고, 칸 격자 가이드(`room_grid_guide.py`)에 맞춰서만 다시 그린다.
TPL_B2 = f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 B 다시 「가시 고리 방」 — **칸 격자에 딱 맞춰서**

■ 화풍 정답(통과) : {P}Projects/AVSR/_exchange/in/mock_room_obj_b.png — 물건 모양 · 색 · 결은 **이것 그대로**
  (막는 물건 = 폐차 · 드럼통 · 타이어 더미 · 냉장고 · 컨테이너, 가시 = 녹슨 판 위 쇠가시, 물 = 기름 섞인 오수).
■ 이번에 고칠 것 — 칸 : {P}Projects/AVSR/_exchange/ref/room_tpl_mock/guide_ch1_room28.png
  · 가이드는 실제 게임 배율이다 — 화면 폭 720 px 에 가로 11 칸(한 칸 약 65 px). 이 칸을 **한 칸도 옮기지 않는다.**
  · 빨강 「가시」 칸 = 가시 판 하나(그 칸 안에만), 파랑 「물」 칸 = 오수(이어지는 웅덩이), 동그라미 = 몬스터 자리
    (원 = 고철 전갈 {P}Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png · 비 = 박쥐 {P}Assets/BaseResource/Unit/bat/unit_bat_s.png ·
     근 = 해골 {P}Assets/BaseResource/Unit/skeleton/unit_skeleton_s.png, 같은 배율 · 아래를 본다).
  · **모든 물건은 제 칸 안에만** 그린다 — 칸 밖으로 한 픽셀도 삐져나오지 않는다. 가시 판 둘이 나란히 있어도 칸 경계가 보이게.
  · **빈 칸은 바닥이 그대로 보인다.** 특히 물 웅덩이와 양옆 가시 줄 사이의 빈 세로 줄(왼쪽 셋째 · 오른쪽 일곱째 칸)은
    캐릭터가 지나가는 길이다 — 한눈에 「여기로 지나갈 수 있다」로 읽혀야 한다.
  · 가이드의 격자선 · 글자 · 색 상자 · 동그라미는 지운다(게임 그림만 남긴다).
  · 바탕 화면의 군더더기(빨간 피해 숫자 · 쓰러진 해골 · 갱스터 · 모래주머니)도 지우고 깨끗한 바닥으로. 플레이어(갱스터)는 아래 입구 난간 바로 위 가운데에 하나.
  · 화면 위 HUD · 아래 조이스틱 · 버튼 셋 · 방 벽 · 입구 난간은 그대로.
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 물건에 빛 · 아우라를 굽지 않는다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_room_obj_b2.png — 720 x 1280, 가이드와 같은 구도 · 같은 픽셀 크기.
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
    if len(sys.argv) > 1 and sys.argv[1] == 'b2':
        write('mock_room_obj_b2', TPL_B2)
    else:
        write('mock_room_obj_a', TPL_A)
        write('mock_room_obj_b', TPL_B)
