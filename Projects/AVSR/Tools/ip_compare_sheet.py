# -*- coding: utf-8 -*-
"""원작 적 · 보스와 우리 잡몹을 **같은 배율로** 나란히 — IP 느낌 비교 시트 (PD 2026-10-22 「몹도 IP 를 벗어난 느낌」).

줄: 원작 적(게임에 들어간 원작 기준 그림) · 원작 보스(줄여서) · 지금 잡몹 14 · 2단계 새 모양 5 · 2단계 원소판.
칸 아래 이름 색 = 내 판단 — 초록 원작 결에 가깝다 · 노랑 중간 · 빨강 멀다.
쓰는 법: python ip_compare_sheet.py → Projects/AVSR/_exchange/ref/ip_compare/ip_compare_sheet.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from trash_cut import cells, bbox  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
OUT = os.path.join(EX, 'ref', 'ip_compare', 'ip_compare_sheet.png')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')

S = 2              # 96 칸 → 192
TILE = 96 * S
LABEL_H = 34
GREEN, YELLOW, RED, WHITE = (110, 230, 130), (245, 210, 90), (250, 110, 100), (235, 235, 235)

HOSTS = [('gangster', '갱스터'), ('thug', '폭력배'), ('hopper', '호퍼'), ('commando_mg', '코만도'),
         ('commando_missile', '미사일 코만도'), ('ninja', '닌자'), ('ninja_chain', '사슬 닌자'), ('amazon', '아마존'),
         ('baseball', '야구선수'), ('guru', '구루'), ('robot', '로봇'), ('vampire', '흡혈귀'),
         ('snowwoman', '설녀'), ('white_wizard', '마법사'), ('medium', '영매'), ('dragoon', '드라군'),
         ('dragon_blue', '청룡'), ('salamander', '샐러맨더')]
BOSSES = [('robot_snakes', '로봇 스네이크'), ('crusher', '크러셔'), ('python', '파이썬'),
          ('sludge', '슬러지'), ('guardian', '가디언'), ('kingpin', '킹핀')]
# 지금 잡몹 — 이름 · 판단
TRASH = [('skeleton', '해골', GREEN), ('bat', '박쥐', GREEN), ('mummy', '미라', GREEN),
         ('roadwarden', '순찰기', GREEN), ('coilwalker', '코일 보행기', GREEN), ('obj_turret', '십자 포탑', GREEN),
         ('scrapgunner', '고철 전갈', YELLOW), ('actor_enforcer', '돌 고릴라', YELLOW), ('mole', '드릴 두더지', YELLOW),
         ('spider', '알 거미', YELLOW), ('boar', '쇠뿔 멧돼지', RED), ('mantis', '외눈 사마귀', RED),
         ('armadillo', '갑옷 아르마딜로', RED), ('mushroom', '폭탄 버섯', RED)]
NEW = [('watcher_eye', '감시 눈', GREEN), ('lantern', '수호 등불', GREEN), ('mimic', '미믹 상자', YELLOW),
       ('shadow_cat', '그림자 살쾡이', YELLOW), ('saw_monkey', '톱날 원숭이', RED)]
ELEM = [('gorilla_fire', '불 고릴라', YELLOW), ('roadwarden_fire', '불 순찰기', GREEN),
        ('scrapgunner_ice', '얼음 전갈', YELLOW), ('coilwalker_ice', '얼음 코일', GREEN),
        ('skeleton_poison', '독 해골', GREEN), ('bat_poison', '독 박쥐', GREEN)]


def unit_tile(key, scale=S):
    p = os.path.join(UNIT, key, f'unit_{key}_s.png')
    if key == 'obj_turret':
        p = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI', 'obj_turret.png')
    if not os.path.exists(p):
        return None
    im = Image.open(p).convert('RGBA')
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def sheet_tile(key, body_h=64):
    """받은 동쪽 판의 첫 칸(대기)을 게임 배율로 — 아직 게임에 안 들어간 그림."""
    p = os.path.join(EX, 'in', f'trash_{key}_e.png')
    if not os.path.exists(p):
        return None
    t = cells(p)[0]
    x0, y0, x1, y1 = bbox(t)
    crop = Image.fromarray(t[y0:y1, x0:x1])
    k = min(body_h / crop.height, 92 / crop.width)
    crop = crop.resize((max(1, round(crop.width * k)), max(1, round(crop.height * k))), Image.NEAREST)
    canvas = Image.new('RGBA', (96, 96), (0, 0, 0, 0))
    canvas.alpha_composite(crop, ((96 - crop.width) // 2, 96 - 8 - crop.height))
    return canvas.resize((TILE, TILE), Image.NEAREST)


def row(title, items, font, small, note):
    # 칸 높이는 그 줄의 가장 큰 그림에 맞춘다 — 보스(256 칸)가 제목을 덮었다
    th = max([TILE] + [t.height for t, _, _ in items if t is not None])
    tw_ = max([TILE] + [t.width for t, _, _ in items if t is not None])
    w = max(1, len(items)) * tw_
    img = Image.new('RGBA', (max(w, 1600), 46 + th + LABEL_H + 26), (34, 36, 44, 255))
    d = ImageDraw.Draw(img)
    d.text((12, 8), title, font=font, fill=WHITE)
    d.text((12 + d.textlength(title, font=font) + 16, 16), note, font=small, fill=(170, 170, 180))
    for i, (tile, name, col) in enumerate(items):
        x = i * tw_
        if tile is not None:
            img.alpha_composite(tile, (x + (tw_ - tile.width) // 2, 46 + th - tile.height))
        tw = d.textlength(name, font=small)
        d.text((x + (tw_ - tw) / 2, 46 + th + 4), name, font=small, fill=col)
    return img


def main():
    font = ImageFont.truetype(FONT, 26)
    small = ImageFont.truetype(FONT, 18)
    rows = [
        row('원작 적', [(unit_tile(k), n, WHITE) for k, n in HOSTS[:9]], font, small,
            '밤의 도시 · 범죄 조직 · 군인 · 무술가 — 사람(빼앗는 몸)'),
        row('원작 적 (계속)', [(unit_tile(k), n, WHITE) for k, n in HOSTS[9:]], font, small,
            '오컬트 · 괴물 · 기계 — 흡혈귀 · 설녀 · 마법사 · 용 · 로봇'),
        row('원작 보스', [(unit_tile(k, 1), n, WHITE) for k, n in BOSSES], font, small,
            '거대 기계(로봇 스네이크 · 크러셔 · 가디언) · 괴물(파이썬 · 슬러지) · 조직 두목(킹핀) — 보스는 절반 배율'),
        row('지금 잡몹 14', [(unit_tile(k), n, c) for k, n, c in TRASH], font, small,
            '2026-09~10 창작분 — 「잡몹은 사람이 아닌 것」 규칙으로 짐승 · 괴물'),
        row('2단계 새 모양 5', [(sheet_tile(k), n, c) for k, n, c in NEW], font, small,
            '받은 동쪽 판 첫 칸 · 아직 게임 밖'),
        row('2단계 원소판', [(sheet_tile(k), n, c) for k, n, c in ELEM], font, small,
            '받은 동쪽 판 첫 칸(비어 있으면 재발주 중)'),
    ]
    legend = Image.new('RGBA', (rows[0].width, 40), (34, 36, 44, 255))
    d = ImageDraw.Draw(legend)
    x = 12
    for col, txt in ((GREEN, '초록 = 원작 결에 가깝다(기계 · 오컬트 망령)'), (YELLOW, '노랑 = 중간'),
                     (RED, '빨강 = 멀다(자연 짐승 · 던전 판타지)')):
        d.text((x, 8), txt, font=small, fill=col)
        x += d.textlength(txt, font=small) + 40
    W = max(r.width for r in rows)
    H = sum(r.height for r in rows) + legend.height
    out = Image.new('RGBA', (W, H), (34, 36, 44, 255))
    y = 0
    for r in rows:
        out.alpha_composite(r, (0, y))
        y += r.height
    out.alpha_composite(legend, (0, y))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    out.convert('RGB').save(OUT)
    print(OUT, out.size)


if __name__ == '__main__':
    main()
