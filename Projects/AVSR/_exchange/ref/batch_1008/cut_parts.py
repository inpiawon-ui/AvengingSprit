# -*- coding: utf-8 -*-
"""부품 시트 → 게임 그림. 자리(시트 좌표)와 게임 크기를 손으로 적는다 — python cut_parts.py <시트키>"""
import os, sys
from PIL import Image
sys.path.insert(0, r'C:\won\UnityProject\AvengingSprit\Projects\AVSR\Tools')
import magenta_cut as mc

B = os.path.dirname(os.path.abspath(__file__))
UI = r'C:\won\UnityProject\AvengingSprit\Assets\BaseResource\InGameMainUI'
GOLEM = r'C:\won\UnityProject\AvengingSprit\Assets\BaseResource\Unit\golem'


def place(part, W, H, mode='fit', anchor='center'):
    """덩어리를 W×H 캔버스에 — fit: 비율 유지 최대, anchor bottom 이면 바닥에 앉힌다."""
    bb = part.getbbox()
    part = part.crop(bb)
    s = min(W / part.width, H / part.height)
    p = part.resize((max(1, round(part.width * s)), max(1, round(part.height * s))), Image.LANCZOS)
    out = Image.new('RGBA', (W, H))
    x = (W - p.width) // 2
    y = H - p.height if anchor == 'bottom' else (H - p.height) // 2
    out.paste(p, (x, y), p)
    return out


SHEETS = {
    'laser': ('parts_laser.png', [
        ((24, 20, 110, 158), 'obj_arc_emitter_on', 48, 72, 'bottom'),
        ((134, 20, 218, 158), 'obj_arc_emitter_off', 48, 72, 'bottom'),
        ((20, 176, 398, 224), 'obj_arc_rail', 192, 24, 'center'),
        ((18, 245, 402, 377), 'obj_arc_1', 192, 72, 'center'),
        ((418, 245, 800, 377), 'obj_arc_2', 192, 72, 'center'),
        ((18, 405, 402, 532), 'obj_arc_3', 192, 72, 'center'),
        ((418, 405, 800, 532), 'obj_arc_4', 192, 72, 'center'),
        ((18, 560, 402, 692), 'obj_arc_5', 192, 72, 'center'),
        ((418, 560, 800, 692), 'obj_arc_6', 192, 72, 'center'),
    ], UI),
    'hammer': ('parts_hammer.png', [
        ((28, 68, 307, 122), 'obj_gantry_rail', 144, 28, 'center'),
        ((332, 78, 467, 256), 'obj_gantry_bracket', 54, 72, 'center'),
        ((492, 30, 627, 162), 'obj_gantry_trolley', 64, 64, 'center'),
        ((656, 22, 684, 74), 'obj_gantry_chain', 12, 24, 'center'),
        ((28, 340, 308, 546), 'obj_gantry_weight', 144, 108, 'bottom'),
        ((332, 352, 612, 414), 'obj_gantry_shadow', 144, 40, 'center'),
    ], UI),
    'moat': ('parts_moat.png', [
        # (자리, 이름, 폭, 높이, 붙이기)
        ((18, 48, 327, 157), 'obj_missile_channel_h_1', 144, 72, 'center'),
        ((356, 48, 669, 157), 'obj_missile_channel_h_2', 144, 72, 'center'),
        ((18, 190, 327, 299), 'obj_missile_channel_h_3', 144, 72, 'center'),
        ((356, 190, 669, 299), 'obj_missile_channel_h_4', 144, 72, 'center'),
        ((20, 342, 129, 671), 'obj_missile_channel_v_1', 72, 144, 'center'),
        ((182, 342, 291, 671), 'obj_missile_channel_v_2', 72, 144, 'center'),
        ((346, 342, 455, 671), 'obj_missile_channel_v_3', 72, 144, 'center'),
        ((508, 342, 619, 671), 'obj_missile_channel_v_4', 72, 144, 'center'),
    ], UI),
    'rail': ('parts_rail.png', [
        ((16, 28, 873, 149), 'obj_rail_bed_h', 432, 64, 'center'),
        ((18, 174, 125, 1013), 'obj_rail_bed_v', 56, 432, 'center'),
        ((910, 4, 999, 157), 'obj_rail_stop', 44, 72, 'center'),
        ((868, 176, 1013, 331), 'obj_rail_carriage', 72, 72, 'center'),
    ], UI),
    'drop': ('parts_drop.png', [
        ((22, 58, 205, 255), 'obj_drop_debris', 108, 126, 'center'),
        ((250, 20, 427, 85), 'obj_drop_shadow', 90, 36, 'center'),
        ((444, 22, 683, 159), 'obj_drop_rubble', 120, 72, 'bottom'),
        ((702, 16, 747, 61), 'obj_drop_chip_1', 24, 24, 'center'),
        ((764, 16, 805, 63), 'obj_drop_chip_2', 24, 24, 'center'),
        ((826, 14, 877, 65), 'obj_drop_chip_3', 24, 24, 'center'),
    ], UI),
    'golem': ('parts_golem.png', [
        ((16, 22, 193, 231), 'unit_golem_blast_1', 96, 96, 'bottom'),
        ((260, 8, 481, 233), 'unit_golem_blast_2', 96, 96, 'bottom'),
        ((516, 28, 763, 233), 'fx_golem_ring', 216, 144, 'center'),
        ((794, 142, 1024, 237), 'fx_golem_dust', 216, 108, 'bottom'),
    ], None),
}


GOLEM_SCALE = None


def place_golem(part):
    """골렘 몸 그림 — 기존 걷기 그림과 같은 키(75px) · 같은 바닥선(87)에. 1단계 키로 배율을 정해 2단계도 같이 쓴다."""
    global GOLEM_SCALE
    part = part.crop(part.getbbox())
    if GOLEM_SCALE is None:
        GOLEM_SCALE = 75 / part.height
    s = GOLEM_SCALE
    p = part.resize((max(1, round(part.width * s)), max(1, round(part.height * s))), Image.LANCZOS)
    out = Image.new('RGBA', (96, 96))
    out.paste(p, (48 - p.width // 2, max(0, 87 - p.height)), p)
    return out


def main():
    k = sys.argv[1]
    sheet, items, outdir = SHEETS[k]
    im = mc.key(Image.open(os.path.join(B, sheet)))
    for box, name, W, H, anc in items:
        out = place_golem(im.crop(box)) if name.startswith('unit_golem') else place(im.crop(box), W, H, anchor=anc)
        d = outdir or (GOLEM if name.startswith('unit_') else UI)
        p = os.path.join(d, name + '.png')
        out.save(p)
        print(p, out.size, out.getbbox())


if __name__ == '__main__':
    main()
