# 인게임 배경 심플화 — 적용 대기 그림(bg_apply)을 끼운 채 실제 방을 게임 규칙대로 그려 본다 (2026-10-29)
# 그리는 규칙은 room_stage_preview.py 를 그대로 쓰고, 그림 찾는 곳만 bg_apply 가 먼저 오게 바꾼다.
# 블록 4~6번은 적용 때 빼므로 없는 것으로 친다.
# 쓰는 법: python Projects/AVSR/Tools/bg_simple_preview.py <챕터> [방 번호 …]
#   결과: _exchange/bg_apply/preview_ch<챕터>.png
import os
import shutil
import sys

import room_stage_preview as rsp
from PIL import Image

APPLY = os.path.join(rsp.EX, 'bg_apply')
_tall_floor = rsp.tall_floor


def tall_floor(stage, height_px):
    new = os.path.join(APPLY, stage, f'roomfloor_env_{stage}.png')
    if not os.path.exists(new):
        return _tall_floor(stage, height_px)
    # 원래 함수는 Assets 의 바닥을 연다 — 같은 자르기 규칙으로 새 바닥을 쓰게 잠깐 바꿔 끼운다
    real_open = rsp.Image.open
    rsp.Image.open = lambda p, *a, **k: real_open(new if p.endswith(f'roomfloor_env_{stage}.png') else p, *a, **k)
    try:
        return _tall_floor(stage, height_px)
    finally:
        rsp.Image.open = real_open


def part_loader(stage):
    tag = os.environ.get('BG_TAG', stage)   # 7~12챕터 변형은 BG_TAG=<무대>v2
    dirs = [os.path.join(APPLY, tag), os.path.join(APPLY, stage), rsp.GAME_ART]

    def load(name):
        if name.startswith('block_') and int(name.split('_')[1]) > (5 if tag.endswith('v2') else 3):
            return None
        for d in dirs:
            for nm in (f'obj_{tag}_{name}.png', f'obj_{stage}_{name}.png'):
                p = os.path.join(d, nm)
                if os.path.exists(p):
                    return Image.open(p).convert('RGBA')
        return None
    return load


rsp.tall_floor = tall_floor
rsp.part_loader = part_loader

if __name__ == '__main__':
    ch = int(sys.argv[1])
    # 원래 도구는 room_tpl_mock/stage_ch<N>_rooms.png 에 쓴다 — 남의 파일이라 잠깐 비켜 두었다가 되돌린다
    src = os.path.join(rsp.EX, 'ref', 'room_tpl_mock', f'stage_ch{ch}_rooms.png')
    keep = src + '.keep'
    if os.path.exists(src):
        shutil.move(src, keep)
    try:
        rsp.main(ch, [int(a) for a in sys.argv[2:]])
        shutil.move(src, os.path.join(APPLY, f'preview_ch{ch}.png'))
    finally:
        if os.path.exists(keep):
            shutil.move(keep, src)
