# -*- coding: utf-8 -*-
"""3단계 무대별 방 물건 시안 — 쓰레기장(통과)과 같은 구도로 나머지 5무대 (코덱스 자유 시안, 2026-10-23).

PD 10-23 「무대마다 시안 먼저 받아서 보여줘」. 쓰레기장은 시안 B → 부품 3판 → 게임 칸 미리보기까지 통과.
나머지 무대도 같은 원작 방(1챕터 28번 — 막는 귀퉁이 · 가시 줄 · 물 속 섬)을 그 무대 물건으로 그린다.
⚠ 코덱스는 칸 자리를 못 지킨다(메모리 codex-cannot-keep-grid) — 시안은 **화풍 · 물건 종류**만 본다. 자리는 부품을 받은 뒤 내가 칸 그대로 붙인다.

쓰는 법: python order_stage_obj_mock.py
실행  : powershell -File Tools/wa-game-harness-kit/files/Tools/harness/run_codex_orders.ps1 -Sheets mock_stage_missile,mock_stage_street,mock_stage_rooftop,mock_stage_lab,mock_stage_refinery -Exchange Projects/AVSR/_exchange
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
P = 'C:/won/UnityProject/AvengingSprit/'

HEAD = ('먼저 ' + P + 'Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). '
        '이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. '
        '스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.')

# 무대 → (이름, 바닥 그림 무대 키, 막는 물건 예, 물, 가시, 울타리, 지금 물건 판 열 이름)
STAGES = {
    'missile': ('미사일 기지', '탄약 상자 · 미사일 거치대 · 연료 드럼 묶음 · 모래주머니 진지 · 레이더 접시 · 장갑판 더미',
                '새어 나온 냉각수 · 연료 웅덩이(콘크리트 배수 턱)', '대전차 고슴도치(체코 헤지호그) · 바닥 쇠말뚝 판',
                '철조망 바리케이드'),
    'street': ('밤거리', '자판기 · 대형 쓰레기통 · 소화전 + 표지판 · 넘어진 오토바이 · 신문 가판대 · 망가진 네온 간판 조각',
               '하수가 넘친 빗물 웅덩이(보도블록 턱)', '깨진 유리 · 못 박힌 판', '공사장 철제 펜스'),
    'rooftop': ('빌딩 옥상', '에어컨 실외기 · 물탱크 · 환풍기 통 · 안테나 기둥 밑동 · 태양광 패널 묶음 · 굴뚝 토막',
                '방수층 위 고인 빗물(낮은 시멘트 턱)', '비둘기 퇴치 가시 판', '옥상 쇠 난간'),
    'lab': ('비밀 연구소', '배양관 · 서버 랙 · 실험대 · 화학 드럼 · 냉동 캡슐 · 고장 난 로봇 팔',
            '쏟아진 녹색 화학 폐액(타일 바닥 턱)', '고압 전극판(바닥 전기 가시)', '강화 유리 칸막이'),
    'refinery': ('정유 공장', '파이프 묶음 · 밸브 달린 탱크 · 기름 드럼 · 펌프 · 보일러 · 철골 토막',
                 '원유 웅덩이(철판 바닥 턱)', '달군 쇠가시 판', '파이프 난간'),
}


def prompt(stage):
    name, blocks, water, spike, fence = STAGES[stage]
    return f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 「{name}」 무대 (원작 방 그대로 · 이 무대 물건으로)

■ 구도 · 칸 정답(쓰레기장 판, 통과) : {P}Projects/AVSR/_exchange/ref/room_tpl_mock/stage_mock_ref_junkyard.png
  · 이 화면과 **같은 구도 · 같은 칸 자리**로 그린다 — 좌우 귀퉁이 막는 물건 · 가시 줄 셋 · 아래 가운데 물 웅덩이와 그 안 섬 · 몬스터 자리.
  · 화면 폭 720 px 에 가로 11 칸(한 칸 약 65 px). **물건은 제 칸 안에만**, 빈 칸은 바닥이 보이게(특히 물 웅덩이 양옆의 빈 세로 줄은 지나가는 길).
  · 비교용으로 화면 아래쪽 빈 칸 두 곳(물 웅덩이 양옆 가시 줄 바깥 아래)에 **울타리 한 칸씩**만 더 놓는다.
■ 이 무대 바닥 : {P}Assets/BundleResource/RoomFloor/roomfloor_env_{stage}.png — 방 바닥 · 벽 · 문 · 아래 입구는 이 그림 그대로.
  지금 이 무대 물건(버릴 것 — 모양이 다른 무대와 같다) : {P}Projects/AVSR/_exchange/plan_content_expand/step3_research/room_objects.png 의 {stage} 줄
■ 쓰레기장 부품(화풍 · 굵기 · 시점 기준, 모양은 베끼지 않는다) : {P}Projects/AVSR/_exchange/in/parts_junkyard_block.png · {P}Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png · {P}Projects/AVSR/_exchange/in/parts_junkyard_water.png

■ 이 무대만의 물건 — 원작(궁수의 전설)은 테마마다 막는 물건 **모양이 통째로 다르다**. 이 무대도 쓰레기장과 겹치는 모양이 없어야 한다.
  · 막는 물건(몸 · 탄 막음) — 서로 다른 실루엣 **5~6가지**를 섞어서. 예(고르는 것은 네 몫) : {blocks}
  · 물(몸 막음 · 탄 통과) — {water}. 여러 칸이 이어지는 웅덩이 · 가장자리 둑. 빛 · 반짝이를 굽지 않는다.
  · 가시(밟으면 아프다 · 지나갈 수 있다) — {spike}. 바닥에 붙어 낮게.
  · 울타리(몸 막음 · 탄 통과) — {fence}. 틈이 보여야 한다.
■ 몬스터는 기준 화면 그림 그대로 · 같은 배율. HUD · 조이스틱 · 버튼은 기준 화면 그대로(챕터 이름 칸 글자는 그대로 둬도 된다).
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 물건에 빛 · 아우라를 굽지 않는다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/mock_stage_{stage}.png — 720 x 1280.
  네가 이 게임 아트 디렉터라면 「{name}」을 어떻게 꾸미겠나 — 칸 자리는 지키고 물건 모양 · 결 · 색은 자유.
끝나면 경로만 알려라.
"""


def write(name, text):
    ps = ('$env:OPENAI_API_KEY = $null\n'
          'Set-Location "C:\\won\\UnityProject\\AvengingSprit"\n'
          '$prompt = @"\n' + text.replace('"@', '" @') + '\n"@\n'
          'codex exec --sandbox workspace-write --skip-git-repo-check $prompt\n')
    path = os.path.join(EX, f'order_{name}.ps1')
    with open(path, 'w', encoding='utf-8-sig') as f:   # PowerShell 5.1 은 BOM 이 있어야 한글을 읽는다
        f.write(ps)
    print(path)


if __name__ == '__main__':
    for s in STAGES:
        write(f'mock_stage_{s}', prompt(s))
