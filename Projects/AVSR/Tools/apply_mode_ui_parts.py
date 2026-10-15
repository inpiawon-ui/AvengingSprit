"""추가 모드 UI 부품을 Assets 로 옮긴다 — 유니티가 비었을 때 한 번 (2026-10-15 준비).

원본: _exchange/out/mode_ui/*.png (cut_mode_ui_parts.py 결과, 게임 크기 · 투명)
목록 · 9분할 · 임포트 설정의 정본: wireframes/AVSR_AssetManifest_Modes.md (이 표와 같다)
옮긴 뒤 유니티에서:
  1) 에디터 스크립트 _exchange/out/mode_ui/ModeUiImport.cs.txt 를 Assets/Scripts/Editor/ModeUiImport.cs 로 복사 → 컴파일
  2) 메뉴 Tools/Game/모드 UI/임포트 설정 (Sprite · Point · 밉맵 끔 · Read/Write 끔 · 9분할 테두리 · 큰 바탕은 ASTC 4x4)
  3) 아틀라스: TowerEntryPanel · NightEntryPanel · NightResultPopup · ModeCommon 폴더 → Atlas/ 같은 이름(안드로이드 ASTC 4x4 · Point · 여백 4)
  4) python Projects/AVSR/Tools/resource_audit.py — 새 파일 경고 0
python apply_mode_ui_parts.py [--dry]
"""
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'out', 'mode_ui')
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')

# 납품 이름 → (폴더, 게임 이름, 9분할 L,B,R,T 또는 None)
MAP = {
    # 인게임 — InGameMainUI 아틀라스에 같이 들어간다
    'tower_plate':         ('InGameMainUI', 'tower_plate', None),
    'night_plate':         ('InGameMainUI', 'night_plate', None),
    'mode_chip':           ('InGameMainUI', 'mode_rule_chip', (23, 0, 23, 0)),
    'mode_band':           ('InGameMainUI', 'mode_intro_band', None),
    'bighost_plate':       ('InGameMainUI', 'bighost_plate', None),
    # 두 입장 패널이 같이 쓰는 것
    'mode_frame':          ('ModeCommon', 'mode_frame', (16, 16, 16, 16)),
    'entry_pip_1':         ('ModeCommon', 'mode_entry_pip_on', None),
    'entry_pip_2':         ('ModeCommon', 'mode_entry_pip_off', None),
    # 탑 입장 패널
    'tower_ladder_cell_1': ('TowerEntryPanel', 'tower_ladder_cell_past', (24, 24, 24, 24)),
    'tower_ladder_cell_2': ('TowerEntryPanel', 'tower_ladder_cell_next', (24, 24, 24, 24)),
    'tower_ladder_cell_3': ('TowerEntryPanel', 'tower_ladder_cell_now', (26, 26, 26, 26)),
    'tower_ladder_cell_4': ('TowerEntryPanel', 'tower_ladder_cell_boss', (24, 24, 24, 24)),
    'tower_badge_go':      ('TowerEntryPanel', 'tower_badge_go', (12, 0, 12, 0)),
    'tower_floor_card':    ('TowerEntryPanel', 'tower_floor_card', (24, 24, 24, 24)),
    # 밤 입장 패널
    'night_day_cell_1':    ('NightEntryPanel', 'night_day_cell', None),
    'night_day_cell_2':    ('NightEntryPanel', 'night_day_cell_today', None),
    'night_today_card':    ('NightEntryPanel', 'night_today_card', (40, 40, 40, 40)),
    'night_today_art':     ('NightEntryPanel', 'night_today_art', None),
    # 결과 · 로비
    'night_result_frame':  ('NightResultPopup', 'night_result_frame', None),
    'modeart_tower':       ('LobbyMainUI', 'modeart_tower', None),
    # 큰 바탕 — 아틀라스 밖(ASTC 4x4 · 최대 2048)
    'tower_panel_bg':      ('ModeBackdrop', 'tower_panel_bg', None),
    'night_panel_bg':      ('ModeBackdrop', 'night_panel_bg', None),
}


def main(dry):
    missing = [k for k in MAP if not os.path.exists(os.path.join(SRC, k + '.png'))]
    if missing:
        print('아직 없는 부품:', missing)
        return 1
    for k, (folder, name, _border) in MAP.items():
        dst_dir = os.path.join(BASE, folder)
        dst = os.path.join(dst_dir, name + '.png')
        print(('[dry] ' if dry else '') + f'{k}.png → Assets/BaseResource/{folder}/{name}.png')
        if not dry:
            os.makedirs(dst_dir, exist_ok=True)
            shutil.copyfile(os.path.join(SRC, k + '.png'), dst)
    return 0


if __name__ == '__main__':
    sys.exit(main('--dry' in sys.argv))
