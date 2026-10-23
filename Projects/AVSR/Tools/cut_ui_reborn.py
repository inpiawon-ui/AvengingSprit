"""RE:BORN UI 리뉴얼(2026-10-23) — 다시 받은 판 · 버튼 · HUD 시트를 부품 낱장으로 자른다(4단계).

시트 칸 배치는 코덱스가 늘 지키지는 않는다(칸이 길면 비율을 바꿔 그린다 — rb_rows · rb_stat_tile).
그래서 칸 격자 대신 **투명도로 덩어리를 찾아** 줄 → 왼쪽부터 순서대로 이름을 붙인다.
크기: 발주 크기는 1080 폭 기준 — 게임은 720 폭이라 **가로를 발주 폭 × 2/3 로** 줄인다(매끈한 판이라 Lanczos).
9-slice 테두리: 줄인 그림에서 가운데 줄과 같은 열 · 행이 시작되는 자리를 재서 L · B · R · T 로 적는다(늘려도 안 깨지는 범위).

python cut_ui_reborn.py → Assets/BaseResource/ReBornKit/{Components,Hud}/rb_*.png + borders.json
"""
import json
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
KIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornKit')
SCALE = 2 / 3

# 시트: (폴더, [(이름, 발주 폭 · 1080 기준, 9-slice 여부)...]) — 줄 → 왼쪽부터
B, N = True, False
SHEETS = {
    'rb_btn_primary': ('Components', [('btn_primary', 320, B), ('btn_primary_pressed', 320, B), ('btn_primary_disabled', 320, B)]),
    'rb_btn_secondary': ('Components', [('btn_secondary', 320, B), ('btn_secondary_pressed', 320, B), ('btn_secondary_disabled', 320, B)]),
    'rb_btn_price': ('Components', [('btn_price', 320, B), ('btn_price_pressed', 320, B), ('btn_price_poor', 320, B), ('btn_price_disabled', 320, B)]),
    'rb_btn_danger': ('Components', [('btn_danger', 320, B), ('btn_danger_pressed', 320, B), ('btn_danger_disabled', 320, B)]),
    'rb_btn_max': ('Components', [('btn_max', 320, B), ('btn_max_pressed', 320, B)]),
    'rb_btn_chip': ('Components', [('btn_chip', 320, B), ('btn_chip_pressed', 320, B), ('btn_chip_disabled', 320, B)]),
    'rb_btn_icon_round': ('Components', [('btn_icon_round', 144, N), ('btn_icon_round_pressed', 144, N), ('btn_icon_round_disabled', 144, N)]),
    'rb_panels': ('Components', [('panel_base', 256, B), ('panel_hero', 400, B), ('modal', 400, B), ('popup_title_plate', 320, B)]),
    'rb_rows': ('Components', [('list_row', 400, B), ('list_row_selected', 400, B), ('info_chip', 220, B), ('currency_pill', 260, B),
                               ('tooltip', 320, B), ('toast', 400, B), ('toast_warning', 400, B), ('toast_reward', 520, B)]),
    'rb_stat_tile': ('Components', [('stat_tile', 508, B), ('stat_tile_pressed', 508, B), ('stat_tile_poor', 508, B), ('stat_tile_max', 508, B),
                                    ('stat_tile_holding', 508, B), ('stat_band', 140, B), ('stat_band_pressed', 140, B)]),
    'rb_host_frame': ('Components', [('host_frame_r', 200, B), ('host_frame_sr', 200, B), ('host_frame_ssr', 200, B), ('host_frame_random', 200, B),
                                     ('host_frame_ghost', 200, B), ('host_inner_glow', 200, N), ('host_select_ring', 212, B), ('host_name_band', 200, B)]),
    'rb_badges': ('Components', [('rarity_r', 88, N), ('rarity_sr', 88, N), ('rarity_ssr', 88, N), ('badge_best', 120, N),
                                 ('badge_today', 120, B), ('badge_new', 120, N), ('level_badge', 80, B), ('profile_frame', 116, N)]),
    'rb_card_option': ('Components', [('card_common', 400, B), ('card_rare', 400, B), ('card_epic', 400, B), ('card_top', 400, B),
                                      ('card_common_selected', 400, B), ('card_rare_selected', 400, B), ('card_epic_selected', 400, B), ('card_top_selected', 400, B)]),
    'rb_slot_item': ('Components', [('slot_common', 170, B), ('slot_rare', 170, B), ('slot_sr', 170, B), ('slot_ssr', 170, B),
                                    ('slot_glow_cyan', 160, N), ('slot_glow_gold', 160, N), ('slot_glow_purple', 160, N)]),
    'rb_tab_bar': ('Components', [('tab_bar_bg', 400, B), ('tab_center_ring', 184, N), ('tab_center_ring_selected', 184, N)]),
    'rb_controls': ('Components', [('seg_track', 320, B), ('seg_on', 320, B), ('toggle_off', 140, N), ('toggle_on', 140, N),
                                   ('toggle_knob', 64, N), ('stepper_minus', 144, B), ('stepper_plus', 144, B)]),
    'rb_bars': ('Components', [('bar_track', 160, B), ('bar_fill_cyan', 160, B), ('bar_fill_green', 160, B), ('bar_fill_yellow', 160, B), ('bar_fill_red', 160, B)]),
    'rb_pass_node': ('Components', [('pass_claim', 144, N), ('pass_claimed', 144, N), ('pass_current', 144, N), ('pass_locked', 144, N)]),
    'rb_summon_card': ('Components', [('summon_card_r', 200, B), ('summon_card_sr', 200, B), ('summon_card_ssr', 200, B)]),
    'rb_result_panels': ('Components', [('result_record_panel', 520, B), ('result_reward_row', 520, B), ('chest_reward_row', 520, B), ('awaken_banner', 520, B)]),
    'rb_misc': ('Components', [('fade_bottom', 128, N), ('dot_red', 32, N)]),
    'rb_hud_bars': ('Hud', [('hud_top_bar', 520, B), ('hud_mode_panel', 460, B), ('hud_boss_bar', 520, B), ('hud_warning_band', 520, B)]),
    'rb_hud_hp': ('Hud', [('hp_ghost_base', 320, B), ('hp_ghost_fill', 320, B), ('hp_ghost_trail', 320, B),
                          ('hp_host_base', 320, B), ('hp_host_fill', 320, B), ('hp_host_trail', 320, B),
                          ('xp_base', 320, B), ('xp_fill', 320, B), ('xp_trail', 320, B)]),
    'rb_hud_possess': ('Hud', [('btn_possess', 230, N), ('btn_possess_pressed', 230, N), ('btn_possess_ready', 230, N), ('btn_possess_cooldown', 230, N)]),
    'rb_hud_skill': ('Hud', [('btn_skill', 170, N), ('btn_skill_pressed', 170, N), ('btn_skill_ready', 170, N), ('btn_skill_cooldown', 170, N),
                             ('btn_ult', 170, N), ('btn_ult_pressed', 170, N), ('btn_ult_ready', 170, N), ('btn_ult_cooldown', 170, N)]),
    'rb_hud_misc': ('Hud', [('joystick_base', 280, N), ('joystick_knob', 112, N), ('btn_pause', 120, N), ('btn_pause_pressed', 120, N), ('ready_chip', 160, B)]),
}


# 칸 격자를 지켜 그렸는데 빛 번짐이 이웃 칸까지 이어져 덩어리로 못 가르는 시트 — 칸대로 자른다(열, 줄)
GRID = {'rb_panels': (4, 1), 'rb_card_option': (4, 2)}
# 칸 줄과 다른 높이에 그린 시트(가는 바 5줄) — 진한 덩어리 순서대로
BLOB_ONLY = {'rb_bars'}


def _order_grid():
    """발주문(order_ui_reborn.py)의 칸 크기 · 열 · 줄 — 시트 이름 → (칸 폭, 칸 높이, 열, 줄)."""
    import order_ui_reborn as O
    return {k: (v[1], v[2], v[3], v[4]) for g in (O.UI, O.HUD) for k, v in g.items()}


ORDER_GRID = _order_grid()


def grid_boxes(im, cols, rows):
    """칸마다 — **중심이 그 칸 안에 있는 덩어리만** 모은다. 코덱스가 부품을 칸 밖으로 살짝 넘겨 그려
    칸 경계로 자르면 이웃 부품 조각이 끼어 들어왔다. 진한 덩어리(알파 > 40)를 모은 뒤 옅은 빛(알파 > 8)까지
    그 칸 안에서 넓힌다. 진한 곳이 없는 빛뿐인 부품(안쪽 빛 · 칸 빛)은 칸 안 옅은 경계를 쓴다."""
    cw, ch = im.width // cols, im.height // rows
    blobs = _boxes(im, 40, 2)
    out = []
    for r in range(rows):
        for c in range(cols):
            cell = (c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)
            mine = [b for b in blobs if cell[0] <= (b[0] + b[2]) / 2 < cell[2] and cell[1] <= (b[1] + b[3]) / 2 < cell[3]]
            if mine:
                x0 = min(b[0] for b in mine); y0 = min(b[1] for b in mine)
                x1 = max(b[2] for b in mine); y1 = max(b[3] for b in mine)
                # 옅은 빛 · 그림자를 되찾되(최대 10 px) **이웃 덩어리와의 중간**을 넘지 않는다 —
                # 가는 바는 줄 사이가 좁아 위 · 아래 바의 옅은 빛이 따라 들어왔다
                room = [10, 10, 10, 10]   # 왼 · 위 · 오른 · 아래
                for o in blobs:
                    if o in mine:
                        continue
                    if o[1] < y1 and y0 < o[3]:
                        if o[2] <= x0: room[0] = min(room[0], (x0 - o[2]) // 2)
                        if o[0] >= x1: room[2] = min(room[2], (o[0] - x1) // 2)
                    if o[0] < x1 and x0 < o[2]:
                        if o[3] <= y0: room[1] = min(room[1], (y0 - o[3]) // 2)
                        if o[1] >= y1: room[3] = min(room[3], (o[1] - y1) // 2)
                out.append([max(0, x0 - room[0]), max(0, y0 - room[1]), min(im.width, x1 + room[2]), min(im.height, y1 + room[3])])
                continue
            bb = im.crop(cell).getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()
            if bb:
                out.append([cell[0] + bb[0], cell[1] + bb[1], cell[0] + bb[2], cell[1] + bb[3]])
    return out


def alpha_of(im):
    """투명 바탕이 아니라 마젠타로 오면 뚫는다(발주문이 허락한 대체)."""
    im = im.convert('RGBA')
    px = im.load()
    corner = px[0, 0]
    if corner[3] > 200 and corner[0] > 200 and corner[1] < 60 and corner[2] > 200:
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, a = px[x, y]
                if r > 180 and g < 90 and b > 180:
                    px[x, y] = (0, 0, 0, 0)
    return im


def boxes(im):
    """진한 부분(알파 > 80)으로 덩어리를 찾고 — 옅은 빛 번짐은 이웃끼리 이어 붙어 한 덩어리가 된다 —
    그 뒤 사방을 이웃과의 중간까지(최대 14 px) 넓혀 빛 · 그림자를 되찾는다."""
    core = _boxes(im, 80, 2)
    w, h = im.size
    out = []
    for i, b in enumerate(core):
        x0, y0, x1, y1 = b
        room = {'l': 14, 'r': 14, 't': 14, 'b': 14}
        for j, o in enumerate(core):
            if i == j:
                continue
            if o[1] < y1 and y0 < o[3]:          # 같은 줄
                if o[2] <= x0: room['l'] = min(room['l'], (x0 - o[2]) // 2)
                if o[0] >= x1: room['r'] = min(room['r'], (o[0] - x1) // 2)
            if o[0] < x1 and x0 < o[2]:          # 같은 열
                if o[3] <= y0: room['t'] = min(room['t'], (y0 - o[3]) // 2)
                if o[1] >= y1: room['b'] = min(room['b'], (o[1] - y1) // 2)
        out.append([max(0, x0 - room['l']), max(0, y0 - room['t']), min(w, x1 + room['r']), min(h, y1 + room['b'])])
    return out


def _boxes(im, thresh, gap):
    """투명도 덩어리의 바깥 상자. 가까운 것은 하나로 합친다."""
    a = im.getchannel('A').point(lambda v: 255 if v > thresh else 0)
    w, h = a.size
    small = a.resize((max(1, w // 4), max(1, h // 4)), Image.BOX).point(lambda v: 255 if v > 0 else 0)
    sw, sh = small.size
    seen = bytearray(sw * sh)
    data = small.load()
    out = []
    for y in range(sh):
        for x in range(sw):
            if data[x, y] == 0 or seen[y * sw + x]:
                continue
            stack = [(x, y)]
            seen[y * sw + x] = 1
            x0, y0, x1, y1 = x, y, x, y
            while stack:
                cx, cy = stack.pop()
                x0, y0, x1, y1 = min(x0, cx), min(y0, cy), max(x1, cx), max(y1, cy)
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < sw and 0 <= ny < sh and data[nx, ny] and not seen[ny * sw + nx]:
                        seen[ny * sw + nx] = 1
                        stack.append((nx, ny))
            out.append([x0 * 4, y0 * 4, (x1 + 1) * 4, (y1 + 1) * 4])
    merged = True
    while merged:
        merged = False
        for i in range(len(out)):
            for j in range(i + 1, len(out)):
                p, q = out[i], out[j]
                if p[0] - gap < q[2] and q[0] - gap < p[2] and p[1] - gap < q[3] and q[1] - gap < p[3]:
                    out[i] = [min(p[0], q[0]), min(p[1], q[1]), max(p[2], q[2]), max(p[3], q[3])]
                    out.pop(j)
                    merged = True
                    break
            if merged:
                break
    out = [b for b in out if (b[2] - b[0]) * (b[3] - b[1]) > 400]
    return out


def reading_order(bs):
    """줄로 묶고(세로 가운데가 겹치면 같은 줄) 왼쪽부터."""
    bs = sorted(bs, key=lambda b: (b[1] + b[3]) / 2)
    rows = []
    for b in bs:
        cy = (b[1] + b[3]) / 2
        if rows and abs(cy - rows[-1][0]) < (b[3] - b[1]) * 0.5:
            rows[-1][1].append(b)
        else:
            rows.append([cy, [b]])
    return [b for _, r in rows for b in sorted(r, key=lambda b: b[0])]


def border(im):
    """가운데 열 · 행과 같은 픽셀이 시작되는 자리 = 9-slice 테두리."""
    w, h = im.size
    px = im.load()

    def col(x):
        return [px[x, y] for y in range(h)]

    def row(y):
        return [px[x, y] for x in range(w)]

    def close(a, b):
        return all(abs(p[i] - q[i]) <= 6 for p, q in zip(a, b) for i in range(4))

    mc, mr = col(w // 2), row(h // 2)
    left = next((x for x in range(w // 2) if close(col(x), mc)), w // 2)
    right = next((w - 1 - x for x in range(w // 2) if close(col(w - 1 - x), mc)), w // 2)
    top = next((y for y in range(h // 2) if close(row(y), mr)), h // 2)
    bottom = next((h - 1 - y for y in range(h // 2) if close(row(h - 1 - y), mr)), h // 2)
    # 끝 한 줄은 여유로 더 준다(경계 픽셀이 늘어나지 않게)
    l, b, r, t = left + 1, h - bottom, w - right, top + 1
    # 세로 그라데이션은 같은 줄이 없어 위 · 아래가 반씩 먹는다 — 가운데 1 px 은 남겨 늘릴 자리를 둔다
    if l + r >= w: l = r = max(0, (w - 1) // 2)
    if t + b >= h: t = b = max(0, (h - 1) // 2)
    return [l, b, r, t]


def main():
    report, borders = [], {}
    for sheet, (folder, names) in SHEETS.items():
        path = os.path.join(IN, sheet + '.png')
        if not os.path.exists(path):
            report.append('%s 없음' % sheet)
            continue
        im = alpha_of(Image.open(path))
        # 발주 시트 크기 그대로 왔으면 칸 격자를 지킨 것 — 칸대로 자른다(덩어리 찾기는 빛 번짐에 이름 순서가 꼬였다)
        grid = ORDER_GRID.get(sheet)
        if sheet in BLOB_ONLY:
            bs = reading_order(_boxes(im, 40, 2))
        elif grid and im.size == (grid[0] * grid[2], grid[1] * grid[3]):
            bs = grid_boxes(im, grid[2], grid[3])[:len(names)]
        else:
            bs = reading_order(boxes(im))
        if len(bs) != len(names) and sheet in GRID:
            bs = grid_boxes(im, *GRID[sheet])
        if len(bs) != len(names):
            report.append('%s 덩어리 %d ≠ 이름 %d — 손으로 볼 것' % (sheet, len(bs), len(names)))
            continue
        os.makedirs(os.path.join(KIT, folder), exist_ok=True)
        for (name, width, sliced), b in zip(names, bs):
            piece = im.crop(b)
            s = width * SCALE / piece.width
            piece = piece.resize((max(1, round(piece.width * s)), max(1, round(piece.height * s))), Image.LANCZOS)
            out = os.path.join(KIT, folder, 'rb_%s.png' % name)
            piece.save(out)
            if sliced:
                borders['%s/rb_%s' % (folder, name)] = border(piece)
        report.append('%s %d장' % (sheet, len(names)))
    with open(os.path.join(KIT, 'borders.json'), 'w', encoding='utf-8') as f:
        json.dump(borders, f, ensure_ascii=False, indent=1)
    print('\n'.join(report))


if __name__ == '__main__':
    main()
