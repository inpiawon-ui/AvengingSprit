"""RE:BORN 아웃게임 연출 검수(2026-10-24) — 게임 캡처(fx_*) → 연출별 GIF + 시안과 나란히 비교판.

python rbfx_review.py → _exchange/ref/reborn_fx/game/{연출}.gif · compare_game.png
비교판 열: 코덱스 시안 힘주는 순간 | 시안 머무는 장면 | 게임 힘주는 순간 | 게임 머무는 장면(같은 높이).
"""
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
PLAY = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'ui_reborn_1023', 'play')
MOCK = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'reborn_fx', 'game')

# 연출: (게임 장면들, 게임 힘주는 장, 게임 머무는 장, 시안 이름, 이름)
FX = [
    ('newbody', ['fx_newbody_%d' % i for i in range(1, 5)], 'fx_newbody_2', 'fx_newbody_4', 'newbody', '새 몸'),
    ('awaken6', ['fx_awaken6_%d' % i for i in range(1, 5)], 'fx_awaken6_1', 'fx_awaken6_4', 'awaken6', '6성 각성'),
    ('awaken10', ['fx_awaken10_%d' % i for i in range(1, 5)], 'fx_awaken10_2', 'fx_awaken10_4', 'awaken10', '10성 각성'),
    ('star', ['fx_star_%d' % i for i in range(1, 4)], 'fx_star_1', 'fx_star_3', 'starfill', '별 채움'),
    ('upgrade', ['fx_upgrade_%d' % i for i in range(1, 4)], 'fx_upgrade_1', 'fx_upgrade_3', 'upgrade', '강화 틱'),
    ('shine', ['fx_shine_%d' % i for i in range(1, 4)], 'fx_shine_2', 'fx_shine_3', 'button', '버튼 빛 띠'),
    ('chest_silver', ['fx_chestsilver_%d' % i for i in range(0, 3)], 'fx_chestsilver_2', 'fx_chestsilver_0', 'chest_silver', '은 상자'),
    ('chest_gold', ['fx_chestgold_%d' % i for i in range(0, 7)], 'fx_chestgold_3', 'fx_chestgold_0', 'chest_gold', '금 상자'),
    ('chest_platinum', ['fx_chestplatinum_%d' % i for i in range(0, 4)], 'fx_chestplatinum_3', 'fx_chestplatinum_0', 'chest_platinum', '백금 상자'),
    ('reward', ['fx_reward_%d' % i for i in range(1, 6)], 'fx_reward_2', 'fx_reward_5', 'reward', '보상 칸 · 금화'),
]
H = 480


def load(path, h=H):
    im = Image.open(path).convert('RGB')
    return im.resize((round(im.width * h / im.height), h))


def font(size):
    for f in ('C:/Windows/Fonts/malgun.ttf', 'C:/Windows/Fonts/gulim.ttc'):
        if os.path.exists(f):
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def main():
    os.makedirs(OUT, exist_ok=True)
    rows = []
    for key, shots, peak, idle, mock, label in FX:
        frames = [load(os.path.join(PLAY, s + '.png'), 640) for s in shots if os.path.exists(os.path.join(PLAY, s + '.png'))]
        if frames:
            frames[0].save(os.path.join(OUT, key + '.gif'), save_all=True, append_images=frames[1:], duration=350, loop=0)
        cells = []
        for p in (os.path.join(MOCK, 'mock_rbfx_%s_free_peak.png' % mock), os.path.join(MOCK, 'mock_rbfx_%s_free_idle.png' % mock),
                  os.path.join(PLAY, peak + '.png'), os.path.join(PLAY, idle + '.png')):
            cells.append(load(p) if os.path.exists(p) else Image.new('RGB', (270, H), (60, 60, 60)))
        rows.append((label, cells))
    f = font(28)
    w = max(sum(c.width for c in cells) + 10 * len(cells) for _, cells in rows) + 200
    sheet = Image.new('RGB', (w, len(rows) * (H + 20) + 50), (24, 26, 34))
    d = ImageDraw.Draw(sheet)
    for i, t in enumerate(['시안 · 힘주는 순간', '시안 · 머무는 장면', '게임 · 힘주는 순간', '게임 · 머무는 장면']):
        d.text((200 + i * 290, 10), t, fill=(230, 230, 230), font=f)
    y = 50
    for label, cells in rows:
        d.text((10, y + H // 2 - 14), label, fill=(255, 210, 90), font=f)
        x = 200
        for c in cells:
            sheet.paste(c, (x, y))
            x += c.width + 10
        y += H + 20
    sheet.save(os.path.join(OUT, 'compare_game.png'))
    print('GIF', len(FX), '· 비교판 →', os.path.join(OUT, 'compare_game.png'))


if __name__ == '__main__':
    main()
