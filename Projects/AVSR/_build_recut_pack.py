"""재제작 대상 앵커 팩 — 목업에서 그 요소가 있는 자리를 자동으로 찾아 잘라준다.

지금까지의 실패는 전부 같은 이유였다. **앵커 없이 말로만 지시하면 도형이 나온다.**
그래서 이번에도 앵커를 붙인다. 다만 이번 대상은 '목업에는 제대로 있는데 잘라온 게
어긋난' 것들이라, 목업 어디를 봐야 하는지를 정확히 짚어줘야 한다.

현재 에셋을 목업 배율로 줄여 **템플릿 매칭**으로 위치를 찾는다. 잘린 에셋이라도
남아 있는 부분은 목업과 일치하므로 자리는 정확히 찾힌다. 찾은 자리 둘레로
여유를 크게 두고 잘라주면, 잘려 나간 아랫부분이 그 안에 들어온다.
"""
import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource')
MOCK = os.path.join(HERE, 'Reference', 'Mockups')
OUT = os.path.join(HERE, '_exchange', 'out', '05_recut')

# 화면 → (목업 파일, 목업 대비 에셋 배율)
SCREEN = {
    'LobbyMainUI':     ('lobby_hub.jpeg', 720 / 683),
    'HostSelectPanel': ('host_select.jpeg', 720 / 683),
    'InGameMainUI':    ('ingame_hd_scene.jpeg', 720 / 576),
}

# 잘림 — 그림이 이미지 가장자리에 닿아 있다(잘려 나갔다)
CLIPPED = [
    ('LobbyMainUI', 'missiontabicon'), ('LobbyMainUI', 'friendstabicon'),
    ('LobbyMainUI', 'friendstablock'), ('LobbyMainUI', 'progressrewardchest'),
    ('LobbyMainUI', 'staminaicon'), ('LobbyMainUI', 'bossportrait'),
    ('HostSelectPanel', 'hostslotlockicon'), ('HostSelectPanel', 'ownedhostchesticon'),
    ('HostSelectPanel', 'hostupgradeicon'), ('HostSelectPanel', 'possessghosticon'),
    ('HostSelectPanel', 'staticon_dash'), ('HostSelectPanel', 'headerportaldeco'),
]
# 뭉갬 — 목업 자체가 작아서 확대 보간으로 죽이 됐다. 다시 잘라도 안 된다.
BLURRED = [
    ('InGameMainUI', 'unit_boss'),
    ('InGameMainUI', 'pausebutton'), ('LobbyMainUI', 'rankingtabicon'),
    ('LobbyMainUI', 'mailbutton'),
]
MARGIN = 14      # 목업 좌표 기준 여유. 잘려 나간 부분이 이 안에 들어온다


def load_layout():
    """레이아웃 JSON 의 캔버스 좌표를 이름(소문자)으로 찾을 수 있게 모은다."""
    import json
    spec = os.path.join(ROOT, 'Assets', 'Scripts', 'Editor', 'UISpec')
    out = {}
    for f in ('Lobby', 'HostSelect', 'InGame'):
        d = json.load(open(os.path.join(spec, f'_layout_{f}.json'), encoding='utf-8'))
        for i in d['items']:
            out[i['name'].lower()] = (i['x'], i['y'])
    return out


# 레이아웃에 없는 것들 — 런타임 생성이라 JSON 에 안 잡힌다. 목업 좌표를 직접 준다.
MANUAL = {'unit_boss': (193, 80)}


def seed_points(layout, name, scale, mock_h):
    """캔버스 좌표 → 목업 좌표. 세로는 상단·하단 두 기준 모두 후보로 낸다."""
    if name in MANUAL:
        return [MANUAL[name]]
    if name not in layout:
        return []
    x, y = layout[name]
    xm = round(x / scale)
    return [(xm, round(y / scale)),                      # 상단(HUD) 기준
            (xm, round(mock_h - (1280 - y) / scale))]    # 하단(콘텐츠) 기준


def _scan(m, t, mask, step, x0, y0, x1, y1):
    """[x0,x1)×[y0,y1) 범위를 step 간격으로 훑어 가장 잘 맞는 자리를 찾는다."""
    th, tw = t.shape[:2]
    best, bxy = None, (x0, y0)
    for y in range(y0, min(y1, m.shape[0] - th), step):
        for x in range(x0, min(x1, m.shape[1] - tw), step):
            s = float((np.abs(m[y:y + th, x:x + tw] - t) * mask).sum())
            if best is None or s < best:
                best, bxy = s, (x, y)
    return bxy


def locate(asset, mock, scale, seeds):
    """레이아웃 좌표를 역변환한 자리 둘레에서만 정밀하게 맞춘다.

    목업 전체를 훑는 방식은 실패했다. 잘려서 남은 조각이 작다 보니
    배경의 아무 데나 더 잘 맞아버렸다. 대신 **이 요소가 화면 어디에 놓이는지**를
    레이아웃 JSON 이 이미 알고 있으므로, 그 좌표를 목업 좌표로 되돌려 씨앗으로 쓴다.

    세로는 상단 기준(hud)과 하단 기준(content) 두 가지 변환이 있고 어느 쪽인지
    JSON 에 안 남아 있다. 후보가 둘뿐이니 둘 다 재보고 더 잘 맞는 쪽을 고른다.
    """
    a = asset.convert('RGB')
    tw, th = max(1, round(a.width / scale)), max(1, round(a.height / scale))
    t = np.asarray(a.resize((tw, th), Image.LANCZOS)).astype(np.float32)
    m = np.asarray(mock).astype(np.float32)

    mk = np.asarray(asset.getchannel('A').resize((tw, th), Image.LANCZOS)).astype(np.float32)
    mask = (mk > 200).astype(np.float32)[:, :, None]
    if mask.sum() < 12:
        mask = np.ones_like(t[:, :, :1])

    def cost(x, y):
        if not (0 <= x <= m.shape[1] - tw and 0 <= y <= m.shape[0] - th):
            return 1e18
        return float((np.abs(m[y:y + th, x:x + tw] - t) * mask).sum()) / max(1.0, mask.sum())

    best, bxy = None, seeds[0]
    for sx, sy in seeds:
        got = _scan(m, t, mask, 1, max(0, sx - 16), max(0, sy - 16), sx + 17, sy + 17)
        c = cost(*got)
        if best is None or c < best:
            best, bxy = c, got
    return bxy, (tw, th), best


def main():
    for d in ('01_clipped', '02_blurred', '03_style'):
        p = os.path.join(OUT, d)
        os.makedirs(p, exist_ok=True)
        for f in os.listdir(p):
            os.remove(os.path.join(p, f))

    mocks = {k: Image.open(os.path.join(MOCK, v[0])).convert('RGB')
             for k, v in SCREEN.items()}

    layout = load_layout()

    for group, items in (('01_clipped', CLIPPED), ('02_blurred', BLURRED)):
        for folder, name in items:
            mock = mocks[folder]
            scale = SCREEN[folder][1]
            a = Image.open(os.path.join(BASE, folder, name + '.png')).convert('RGBA')
            seeds = seed_points(layout, name, scale, mock.height)
            if not seeds:
                print(f'  {folder[:4]}/{name:<22} 레이아웃에 없음 — 건너뜀')
                continue
            (x, y), (tw, th), err = locate(a, mock, scale, seeds)

            box = (max(0, x - MARGIN), max(0, y - MARGIN),
                   min(mock.width, x + tw + MARGIN), min(mock.height, y + th + MARGIN))
            src = mock.crop(box)
            k = 8
            src.resize((src.width * k, src.height * k), Image.NEAREST).save(
                os.path.join(OUT, group, f'{name}__mockup_x{k}.png'))
            a.resize((a.width * 6, a.height * 6), Image.NEAREST).save(
                os.path.join(OUT, group, f'{name}__current_x6.png'))
            print(f'  {folder[:4]}/{name:<22} 목업 ({x},{y}) {tw}x{th}  오차 {err:.0f}')

    # 화풍 기준 — 잘려 있지도 뭉개지도 않은, 이 화면들의 좋은 아이콘
    for folder, name in (('HostSelectPanel', 'staticon_hp'), ('HostSelectPanel', 'staticon_atk'),
                         ('HostSelectPanel', 'staticon_spd'), ('HostSelectPanel', 'tipicon'),
                         ('LobbyMainUI', 'achievementtabicon'), ('LobbyMainUI', 'inventorytabicon'),
                         ('LobbyMainUI', 'goldicon'), ('LobbyMainUI', 'gemicon'),
                         ('LobbyMainUI', 'notifybadge'), ('LobbyMainUI', 'plusbutton')):
        a = Image.open(os.path.join(BASE, folder, name + '.png')).convert('RGBA')
        a.resize((a.width * 6, a.height * 6), Image.NEAREST).save(
            os.path.join(OUT, '03_style', f'{name}_x6.png'))

    print(f'\n앵커 팩 → {OUT}')


if __name__ == '__main__':
    main()
