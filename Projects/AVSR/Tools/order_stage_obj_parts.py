# -*- coding: utf-8 -*-
"""3단계 무대별 방 물건 부품 발주 — 쓰레기장(통과)과 같은 판 형식으로 나머지 5무대 (2026-10-23).

PD 10-23 무대 시안(mock_stage_*.png) 보고 「응 그렇게 진행해줘」 · 옥상 · 정유 공장 가시는 쓰레기장과 같아 모양을 바꿔 주문.
판 형식 · 칸 규격 · 자르는 도구는 쓰레기장과 같다(order_room_obj_parts.py · cut_room_obj_parts.py <무대>).
  막는 물건 6(3 x 2 · 칸 288 x 408) · 가시 2 + 울타리 2(4 x 1) · 물 타일 10(수면 2 · 둑 4 · 귀퉁이 4, 4 x 3 · 칸 288 x 288)

쓰는 법: python order_stage_obj_parts.py [무대 …]   (빼면 5무대 전부)
실행  : run_codex_orders.ps1 -Sheets parts_<무대>_block,parts_<무대>_spike_fence,parts_<무대>_water …
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
P = 'C:/won/UnityProject/AvengingSprit/'

HEAD = ('먼저 ' + P + 'Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). '
        '이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. '
        '스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.')

# 무대 → 이름 · 막는 물건 6 · 가시 A/B · 울타리 A/B · 물
STAGES = {
    'missile': dict(name='미사일 기지',
                    blocks=['탄약 상자 더미(군용 녹색 · 스텐실 글자 없이)', '미사일 거치대(누운 미사일 한 발)', '연료 드럼 묶음(노랑 띠)',
                            '모래주머니 진지(낮게 쌓은 것)', '레이더 접시(작은 받침대 위)', '장갑판 더미(철판 겹친 것)'],
                    spike=['대전차 고슴도치(X 자 철골 하나)', '쇠말뚝 판(콘크리트 판에 박힌 짧은 쇠말뚝)'],
                    fence=['철조망 바리케이드(나무 X 틀 + 철조망)', '쇠창살 검문 바리케이드'],
                    water='새어 나온 냉각수 · 연료 웅덩이 — 청록빛 도는 어두운 물, 콘크리트 배수 턱 둑'),
    'street': dict(name='밤거리',
                   blocks=['자판기', '대형 쓰레기통(뚜껑 닫힘)', '소화전 + 쓰러진 표지판', '넘어진 오토바이',
                           '신문 가판대', '망가진 네온 간판 조각(불 꺼진 것 — 빛을 굽지 않는다)'],
                   spike=['깨진 유리 조각 더미', '못이 촘촘히 박힌 나무 판'],
                   fence=['공사장 철망 펜스', '접이식 철제 바리케이드'],
                   water='하수가 넘친 빗물 웅덩이 — 검푸른 물에 기름 무늬, 보도블록 턱 둑'),
    'rooftop': dict(name='빌딩 옥상',
                    blocks=['에어컨 실외기', '둥근 물탱크(짧은 다리)', '환풍기 통(지붕 버섯 모양)', '안테나 기둥 밑동(받침 콘크리트)',
                            '태양광 패널 묶음(비스듬히)', '굴뚝 토막(벽돌)'],
                    spike=['비둘기 퇴치 가는 철침 줄(긴 받침대에 가는 바늘이 빽빽이 — 쓰레기장 가시 판과 다른 모양)',
                           '같은 철침 줄 두 줄 엇갈림'],
                    fence=['옥상 쇠 난간', '낮은 유리 난간(테두리 쇠)'],
                    water='방수층 위 고인 빗물 — 흐린 회청색 물, 낮은 시멘트 턱 둑'),
    'lab': dict(name='비밀 연구소',
                blocks=['배양관(빈 관 — 빛 없이)', '서버 랙', '실험대(비커 몇 개)', '화학 드럼(위험 표시)',
                        '냉동 캡슐(닫힘)', '고장 난 로봇 팔(받침 위에 꺾여 멈춤)'],
                spike=['고압 전극판(바닥 판에 솟은 전극 핀 — 전기 빛은 굽지 않는다)', '같은 전극판, 핀 배치만 다르게'],
                fence=['강화 유리 칸막이(쇠 테두리)', '레이저 차단기 기둥 둘(빔은 그리지 않는다 — 기둥 사이가 비어 있다)'],
                water='쏟아진 녹색 화학 폐액 — 탁한 녹색, 흰 타일 바닥 턱 둑(빛 · 형광을 굽지 않는다)'),
    'refinery': dict(name='정유 공장',
                     blocks=['파이프 묶음(굽은 관 셋)', '밸브 달린 작은 탱크', '기름 드럼(검은 얼룩)', '펌프(바퀴 달린 손잡이)',
                             '보일러 토막(리벳)', '철골 토막(H 빔 쌓은 것)'],
                     spike=['달군 쇠가시 판(쇠가시 끝이 주황빛으로 달아오른 색 — 빛 번짐 없이 도트 색만, 쓰레기장 가시 판과 다른 모양)',
                            '바닥 격자 사이로 솟은 뜨거운 철침'],
                     fence=['파이프 난간', '철망 안전 펜스(노랑 띠)'],
                     water='원유 웅덩이 — 검은 기름에 무지개 몇 점, 철판 바닥 턱 둑'),
}

FORMAT = f"""■ 판 형식 정답(쓰레기장 — 통과) : {P}Projects/AVSR/_exchange/in/parts_junkyard_block.png · {P}Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png ·
  {P}Projects/AVSR/_exchange/in/parts_junkyard_water.png — **칸 수 · 칸 크기 · 칸 안 자리 · 마젠타 여백**을 이것과 똑같이. 모양 · 색은 이 무대 것으로.
■ 게임 크기 기준 : {P}Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png (72 x 102 — 아래 72 x 72 발자국 + 위 30 윗면)
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 위에서 비스듬히 내려다본 시점.
  **물건에 빛 · 아우라 · 반짝이를 굽지 않는다.** 그림자는 그리지 않는다(게임이 따로 깐다).
■ 판 규칙 — 배경은 **마젠타 #FF00FF 단색**(검정 · 회색 바탕 금지). **물건은 제 칸 안에만**. 칸마다 하나, 칸 바닥에 붙여 가운데."""


def style(stage):
    s = STAGES[stage]
    return (f"■ 화풍 정답(PD 통과) : {P}Projects/AVSR/_exchange/in/mock_stage_{stage}.png — 「{s['name']}」 시안의 물건과 **같은 결 · 같은 색 · 같은 굵기**.\n"
            f"  무대 바닥 : {P}Assets/BundleResource/RoomFloor/roomfloor_env_{stage}.png\n{FORMAT}")


def block(stage):
    s = STAGES[stage]
    items = '\n'.join(f'  {i + 1} {b}' for i, b in enumerate(s['blocks']))
    return f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 「{s['name']}」 「막는 물건」 부품 6종 (한 칸짜리)

{style(stage)}

■ 하는 일 — 원작(궁수의 전설)에서 옮긴 방의 막는 칸(몸 · 탄 모두 막음)에 하나씩. 옆 칸끼리 붙으면 이어진 벽 · 귀퉁이 덩어리가 된다.
  **서로 다른 실루엣 6가지** — 쓰레기장(고철 · 타이어 · 냉장고 · 드럼통 · 폐차 · 컨테이너)과 겹치는 모양이 없어야 한다.
{items}
  · 모두 발자국(아래 72 x 72)을 꽉 채우고 위로 30 만큼만 솟는다.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/parts_{stage}_block.png — 가로 3 x 세로 2 판, 칸 288 x 408, 칸 사이 마젠타 32. 위 줄 1 · 2 · 3, 아래 줄 4 · 5 · 6.
끝나면 경로만 알려라.
"""


def spike_fence(stage):
    s = STAGES[stage]
    return f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 「{s['name']}」 「가시 · 울타리」 부품 4종 (한 칸짜리)

{style(stage)}

■ 하는 일
  · 가시 — **늘 솟아 있다** · 밟으면 아프다 · 몸도 탄도 지나간다. 바닥에 붙어 위로 안 솟고 72 x 72 발자국 안에. 한눈에 「밟으면 아프다」.
  · 울타리 — 몸은 막고 **탄은 지나간다** — 틈이 보여야 한다(꽉 막힌 벽처럼 보이면 안 된다). 72 x 102 칸.
■ 4종(칸 차례대로)
  1 가시 A — {s['spike'][0]}
  2 가시 B — {s['spike'][1]}
  3 울타리 A — {s['fence'][0]}
  4 울타리 B — {s['fence'][1]}
  · 1 · 2 는 칸 아래 288 x 288 안에만(위 30 줄 몫은 비운다), 3 · 4 는 칸 전체 안에.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/parts_{stage}_spike_fence.png — 가로 4 x 세로 1 판, 칸 288 x 408, 칸 사이 마젠타 32.
끝나면 경로만 알려라.
"""


def water(stage):
    s = STAGES[stage]
    return f"""{HEAD}

AVENGING SPIRIT: RE:BORN — 「{s['name']}」 「물」 타일 한 벌 — 칸끼리 이어 붙는다

{style(stage)}

■ 하는 일 — 원작의 물 칸. **몸은 못 건너고 탄은 지나간다.** 칸이 여럿 이어져 웅덩이 · 강 · 십자가 된다.
  게임은 물 칸마다 「수면」을 깔고, 옆 칸이 물이 아닌 쪽에만 「가장자리」를, 두 쪽이 다 땅인 모서리에 「귀퉁이」를 얹는다 —
  그래서 수면은 **어느 쪽으로 이어 붙여도 이음매가 안 보여야** 하고, 칸 테두리를 밝게 그리지 않는다.
  이 무대 물 : {s['water']}. 바닥보다 한 단 낮게. 빛 · 반짝이를 굽지 않는다.
■ 10칸(칸 288 x 288) — 쓰레기장 물 판과 **같은 차례 · 같은 자리**
  1 수면 A(꽉 채움) · 2 수면 B(무늬만 다르게) · 3 가장자리 위(위 변에만 둑) · 4 가장자리 아래 · 5 가장자리 왼쪽 · 6 가장자리 오른쪽 ·
  7 귀퉁이 왼쪽 위(그 모서리에만) · 8 귀퉁이 오른쪽 위 · 9 귀퉁이 왼쪽 아래 · 10 귀퉁이 오른쪽 아래 — 3~10 은 그 변 · 모서리 밖은 마젠타.

■ 만들 것 : {P}Projects/AVSR/_exchange/in/parts_{stage}_water.png — 가로 4 x 세로 3 판(1~4 / 5~8 / 9 · 10), 칸 288 x 288, 칸 사이 마젠타 32.
끝나면 경로만 알려라.
"""


def write(name, text):
    ps = ('$env:OPENAI_API_KEY = $null\n'
          'Set-Location "C:\\won\\UnityProject\\AvengingSprit"\n'
          '$prompt = @"\n' + text.replace('"@', '" @') + '\n"@\n'
          'codex exec --sandbox workspace-write --skip-git-repo-check $prompt\n')
    with open(os.path.join(EX, f'order_{name}.ps1'), 'w', encoding='utf-8-sig') as f:   # PowerShell 5.1 은 BOM 이 있어야 한글을 읽는다
        f.write(ps)
    return name


if __name__ == '__main__':
    stages = sys.argv[1:] or list(STAGES)
    names = []
    for st in stages:
        names += [write(f'parts_{st}_block', block(st)), write(f'parts_{st}_spike_fence', spike_fence(st)),
                  write(f'parts_{st}_water', water(st))]
    print(','.join(names))
