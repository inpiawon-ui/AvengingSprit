"""빙의 파티 창 3종(선택 · 사망 · 시너지 설명) 빠진 부품 앵커 — 시안에서 잘라 3배로 키운다.

PD 2026-10-29 「시안을 컨펌했는데 너무 대충 적용」 — 창 판 · 줄만 받고 안의 부품(태그 칩 · 남은 HP 칸 · 추천 딱지 ·
피 튄 제목 판 · 사슬 · 말풍선 판 · 단계 줄 · 칸 딱지)을 안 받았다. 이 앵커로 부품을 받는다.
결과: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_*.png
"""
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'party_ui_1029', 'anchor')

# (시안, 이름, 왼 · 위 · 오른 · 아래) — 720 x 1280 시안 좌표
CROPS = [
    ('F_choice', 'p4_choice_header', (40, 560, 690, 690)),     # 큰 얼굴 · 이름 · 태그 칩 · 세로 구분선 · 보조 자동 스킬
    ('F_choice', 'p4_choice_tags', (280, 636, 466, 676)),      # 금 칩(무기) · 붉은 칩(갱단)
    ('F_choice', 'p4_choice_skillbox', (486, 616, 552, 682)),  # 스킬 아이콘 칸
    ('F_choice', 'p4_choice_row', (50, 690, 672, 786)),        # 줄 — 얼굴 칸 · 남은 HP 칸 · 시너지 바뀜
    ('F_choice', 'p4_choice_hppill', (156, 736, 360, 780)),    # 남은 유령 HP 칸
    ('F_choice', 'p4_choice_best', (50, 892, 672, 988)),       # 추천 줄 + 추천 딱지
    ('F_choice', 'p4_choice_badge', (574, 910, 664, 948)),     # 추천 딱지
    ('F_death', 'p4_death_title', (230, 220, 500, 320)),       # 피 튄 제목 판
    ('F_death', 'p4_death_rows', (10, 510, 690, 780)),         # 얇은 줄 셋 · 추천 육각 · 사슬 · 자물쇠
    ('F_death', 'p4_death_chain', (470, 690, 690, 780)),       # 사슬
    ('F_syndetail', 'p4_syn_popover', (110, 160, 550, 520)),   # 말풍선 판(왼쪽 꼬리) 전체
    ('F_syndetail', 'p4_syn_tiers', (126, 248, 540, 340)),     # 켜진 단계 줄(금) · 꺼진 단계 줄 · 「1명 더」 칩
    ('F_syndetail', 'p4_syn_members', (126, 350, 540, 466)),   # 몸 칸(금 · 보라 · 회색) · M · 1 딱지
]


def main():
    os.makedirs(OUT, exist_ok=True)
    for mock, name, box in CROPS:
        im = Image.open(os.path.join(IN, f'mock_partyui_{mock}.png')).convert('RGB').crop(box)
        im = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
        im.save(os.path.join(OUT, name + '.png'))
        print(name, im.size)


if __name__ == '__main__':
    main()
