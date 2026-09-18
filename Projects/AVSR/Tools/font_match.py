"""시안 글자와 폰트 굵기 대조 — 각 굵기로 같은 글자를 찍어 시안 글자 모양과 겹쳐 본다."""
import os, json, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'Assets', 'BaseResource', 'Fonts'))
MOCK = os.path.join(HERE, '..', 'Reference', 'Mockups', 'lobby_hub_v2.png')
SPEC = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'Assets', 'BaseResource', 'LobbyV3', 'lobby_v3_spec.json'))

SAMPLES = {
    'GoldText': '125,680', 'SeasonPassTitleText': '시즌 패스', 'GhostSearchTitleText': '유령 수색',
    'GhostSearchTimerText': '04:32:18', 'ModeCenterTitleText': '시나리오 모드', 'HostButtonTitleText': 'HOST',
    'GhostSearchClaimText': '보상 받기', 'ModePlayButtonText': '플레이하기', 'GameModeLabel': '게임 모드',
}


def glyph_mask(img, color):
    a = np.asarray(img.convert('RGB')).astype(np.int16)
    c = np.array([int(color[i:i + 2], 16) for i in (1, 3, 5)])
    return (np.abs(a - c).sum(axis=2) < 150)


def main():
    spec = json.load(open(SPEC, encoding='utf-8'))
    M = Image.open(MOCK).convert('RGB')
    res = {}
    for key, text in SAMPLES.items():
        t = spec['texts'][key]
        x0, y0, x1, y1 = t['box']
        target = glyph_mask(M.crop((x0, y0, x1, y1)), t['color'])
        best = []
        for wname in ['Bold', 'ExtraBold', 'Black']:
            f = os.path.join(FONTS, f'NotoSansKR-{wname}.ttf')
            # 글자 높이를 시안 칸 높이에 맞춘다
            for size in range(12, 80):
                font = ImageFont.truetype(f, size)
                bb = font.getbbox(text)
                if bb[3] - bb[1] >= (y1 - y0): break
            im = Image.new('L', (bb[2] - bb[0], bb[3] - bb[1]), 0)
            ImageDraw.Draw(im).text((-bb[0], -bb[1]), text, font=font, fill=255)
            im = im.resize((x1 - x0, y1 - y0), Image.LANCZOS)
            mine = np.asarray(im) > 128
            iou = (mine & target).sum() / max(1, (mine | target).sum())
            ink_ratio = mine.sum() / max(1, target.sum())
            best.append((wname, size, round(float(iou), 3), round(float(ink_ratio), 2)))
        res[key] = best
        print(key, best)


if __name__ == '__main__':
    main()
