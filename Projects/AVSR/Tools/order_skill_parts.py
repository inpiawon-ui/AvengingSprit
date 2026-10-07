"""스킬 연출 퀄업 13종 — 게임에 넣을 부품(프레임) 시트 발주문 (2026-10-07).

PD 가 13종 시안을 전부 통과(in/mock_skill_{key}_v1.png). 시안과 똑같이 가려고 부품을 시안을 앵커로 발주한다.
시트 규격은 모두 같다 — 1024 x 512 마젠타, 칸 256 x 256 이 4열 x 2줄. 줄마다 부품 하나(프레임 시간 순, 왼쪽부터).
자르기는 Tools/cut_skill_parts.py 가 같은 표(PARTS)를 읽어 fx_{이름}_{n}.png 로 낸다.
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

# key: [(부품 이름, 장 수, 설명)] — 줄 1 · 줄 2 (한 줄에 4칸까지, 둘이 한 줄을 나눠 쓸 때는 ('a',2),('b',2))
PARTS = {
    'guru': [
        [('gdring', 4, '구루 발밑에서 퍼지는 황금빛 원(탑뷰 바닥 타원, 지름 약 120→200 px, 얇은 금빛 테 + 작은 불티)')],
        [('gddome', 2, '구루 몸을 감싸는 반투명 황금 결계 막(얇은 육각 무늬, 지름 약 150 px, 가운데는 비침) — 2장이 은은히 번갈아'),
         ('gdrim', 2, '결계 지속 표시 — 얇은 황금 테두리 원만(지름 약 130 px, 아주 얇게) — 2장 번갈아')],
    ],
    'ninja_chain': [
        [('cbchain', 1, '쇠사슬 한 토막(가로로 이어 붙일 수 있게 좌우 끝이 맞물리는 사슬 고리 6~7개, 칸 가운데 가로 약 220 px · 높이 약 22 px)'),
         ('cbsnap', 3, '사슬이 적에게 감기는 순간 작은 쇳빛 섬광(지름 약 50 px) 1 작게 → 2 최대 → 3 사라짐')],
        [('cbwrap', 3, '적 몸통에 사슬이 감겨 조이는 모습 — 가로 고리가 2~3바퀴(너비 약 90 px · 높이 약 60 px), 1 느슨 → 2 감김 → 3 조임'),
         ('cbhold', 1, '묶인 상태 표시 — 사슬 고리 몇 개만 남은 작은 고리(너비 약 70 px)')],
    ],
    'baseball': [
        [('rfring', 4, '슬러거 몸 둘레 하얀-적황 반사 원(탑뷰 타원, 지름 약 120 px, 얇은 테 + 작은 불티) — 1 생김 → 2 · 3 지속(번갈아) → 4 옅음')],
        [('rfswing', 4, '배트로 탄을 쳐내는 타격 섬광 — 반달 모양 아닌 둥근 섬광 + 작은 불티(지름 약 70 px) 1 → 4 사라짐')],
    ],
    'salamander': [
        [('vncloud', 4, '샐러맨더 둘레로 퍼지는 독 안개(탑뷰, 바닥에 낮게 깔린 초록-보라 도트 구름 고리, 지름 약 160→300 px) 1 → 4 옅게 걷힘')],
        [('vngather', 2, '입 앞에 모이는 초록-보라 빛(지름 약 40 px) 1 → 2 수축'),
         ('vndrip', 2, '중독된 적 머리 위 작은 독 방울 표시(초록-보라 방울 2~3개, 약 30 px) 2장 번갈아')],
    ],
    'dragoon': [
        [('ffbloom', 4, '적 발밑에 원형으로 확 피어나는 불바다(탑뷰, 주황 · 자홍 도트 불꽃, 지름 약 160 px) 1 점화 → 4 최대')],
        [('ffburn', 4, '불바다 지속 — 낮은 불꽃이 원형으로 일렁임(높게 치솟지 않게, 지름 약 160 px) 4장 반복')],
    ],
    'dragon_blue': [
        [('dsbolt', 3, '적에서 적으로 튕기는 번개 한 줄기 — 가로로 뻗은 지그재그 도트 번개(청백 · 연보라, 칸 가운데 가로 약 230 px · 굵기 약 16 px) 3장(모양만 다르게, 번갈아)')],
        [('dscoil', 2, '청룡 몸에 감기는 청백 전기(지름 약 110 px, 몸 둘레 전기 줄기) 2장 번갈아'),
         ('dsspark', 2, '맞은 적 몸의 작은 전기 잔광(약 50 px) 1 → 2 사라짐')],
    ],
    'white_wizard': [
        [('wfgather', 4, '지팡이 앞에 모여 수축하는 흰-연보라 빛 구슬들(작은 구슬 6~8개가 가운데로, 지름 약 90→30 px)')],
        [('wfpop', 4, '광탄이 맞은 자리의 작은 빛 조각 흩어짐(흰-연보라, 약 50 px) — 별 · 십자 반짝이 모양 금지, 둥근 조각')],
    ],
    'medium': [
        [('mgrune', 4, '바닥에 그려지는 보라 룬 원(탑뷰 타원, 지름 약 170 px, 룬 문자 도트) 1 그려지기 시작 → 4 완성 빛남')],
        [('mgrise', 4, '룬 원에서 골렘이 솟을 때 터지는 돌 파편 · 보라 빛(골렘은 그리지 않는다, 바닥 중심 지름 약 160 px) 1 → 4 흩어짐')],
    ],
    'snowwoman': [
        [('isgather', 2, '설녀 둘레에 모이는 냉청 서리(지름 약 120 px) 1 → 2'),
         ('isprison', 2, '설녀를 가둔 반투명 얼음 결정(세로로 선 육각 결정, 너비 약 110 · 높이 약 160 px, 안이 비쳐 캐릭터가 보이게) 2장 번갈아 — 안에 하늘색 회복 빛')],
        [('isbreak', 4, '얼음 결정이 깨져 작은 얼음 조각이 흩어짐(약 160 px) 1 → 4 사라짐')],
    ],
    'vampire': [
        [('vmcircle', 4, '흡혈귀 발밑 붉은-자홍 망토 기운 원(탑뷰 타원, 지름 약 130 px) 4장 반복')],
        [('vmorb', 2, '적에게서 빨려 드는 작은 붉은 피 구슬(약 24 px, 꼬리 짧게) 2장 번갈아'),
         ('vmglow', 2, '흡혈귀 몸에 잠깐 감도는 붉은 빛(지름 약 100 px) 1 → 2 사라짐')],
    ],
    'death': [
        [('rpaura', 4, '사신 발밑 보라-검은 기운 원(탑뷰 타원, 지름 약 120 px, 얇게) 4장 반복')],
        [('rpslash', 2, '낫을 한 번 휘두른 짧은 보라 호(약 140 px) 1 → 2 사라짐'),
         ('rpskull', 2, '맞은 적 머리 위 작은 보라 해골 표식(약 36 px) 2장 번갈아')],
    ],
}

TEMPLATE = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 **스킬 연출 부품 시트** — PD 가 통과시킨 시안 Projects/AVSR/_exchange/in/mock_skill_{key}_v1.png 와
**똑같은 모양 · 색 · 도트 그림체**로 그린다(시안 1 발동 · 2 최대 · 3 끝 칸에서 해당 부분을 그대로 따라).

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄. 각 그림은 칸 가운데, 칸 밖으로 넘치지 않게.
{rows}
- 캐릭터 · 적은 그리지 않는다(연출 부품만). 그림자 · 마젠타/분홍 색 금지(배경과 섞인다) · 별 · 표창 · 십자 반짝이 모양 금지.
- 빈 칸은 마젠타로 비워 둔다.
[저장] Projects/AVSR/_exchange/in/skill_parts_{key}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''


def rows_text(rows):
    out = []
    for r, parts in enumerate(rows):
        col = 1
        for name, n, desc in parts:
            cells = '칸 %d' % col if n == 1 else '칸 %d~%d' % (col, col + n - 1)
            out.append('- %d줄 %s: %s' % (r + 1, cells, desc))
            col += n
    return '\n'.join(out)


if __name__ == '__main__':
    for key, rows in PARTS.items():
        path = os.path.join(EX, 'order_skill_parts_%s.ps1' % key)
        with open(path, 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(key=key, rows=rows_text(rows)))
        print(key)
