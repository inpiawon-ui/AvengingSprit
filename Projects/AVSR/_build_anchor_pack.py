"""재생성용 앵커 팩 + 목업 복원본 생성.

교훈 (측정으로 확인):
  앵커를 붙인 자산 → 성공 (호스트 초상 24종: 대칭 10~17%, 16색)
  텍스트 프롬프트만 → 코드 도형 (대칭 100%, 2~3색)
따라서 **모든 자산에 영역 앵커를 붙인다.**

산출:
  01_anchors/{에셋명}.png       목업에서 잘라낸 해당 영역 (원본 해상도)
  01_anchors/{에셋명}_x3.png    ×3 확대 (GPT가 디테일을 읽을 수 있게)
  02_restored/{에셋명}.png      목업에서 직접 복원한 완성본 (재생성 불필요)
"""
import io, os, json
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
MOCK = os.path.join(HERE, 'Reference', 'Mockups')
OUT = os.path.join(HERE, '_regen')

# ── 목업 내 영역 좌표 (x, y, w, h) — 격자 오버레이로 실측 ──────────────
LOBBY = {
    # 상단 HUD
    'ghostwidget':          (8, 14, 166, 78),
    'ghostportraiticon':    (14, 20, 56, 62),
    'staminacounter':       (176, 24, 112, 40),
    'goldcounter':          (292, 24, 116, 40),
    'gemcounter':           (410, 24, 114, 40),
    'staminaicon':          (188, 30, 28, 28),
    'goldicon':             (306, 30, 28, 28),
    'gemicon':              (424, 30, 28, 28),
    'plusbutton':           (256, 32, 26, 26),
    'mailbutton':           (570, 28, 42, 38),
    'settingsbutton':       (628, 28, 40, 38),
    'notifybadge':          (596, 22, 20, 20),
    # 챕터 카드
    'chaptercard':          (14, 290, 222, 344),
    'bossportrait':         (130, 348, 84, 84),
    'progressrewardchest':  (186, 486, 42, 44),
    'continuebutton':       (24, 552, 198, 74),
    'progressbarbg':        (24, 506, 154, 20),
    # 우측 프로모
    'battlepasscard':       (455, 288, 215, 100),
    'battlepassart':        (590, 292, 74, 88),
    'battlepassbadge':      (466, 344, 40, 38),
    'battlepassbarbg':      (512, 352, 78, 14),
    'eventcard':            (455, 394, 215, 92),
    'eventart':             (574, 412, 72, 66),
    'dailylogincard':       (455, 494, 215, 92),
    'dailyloginart':        (576, 508, 66, 58),
    'dailylogincheck':      (462, 556, 26, 26),
    # 탭바
    'featuretabbarbackground': (30, 626, 622, 78),
    'missiontabicon':       (94, 632, 38, 40),
    'achievementtabicon':   (202, 632, 40, 40),
    'rankingtabicon':       (310, 632, 42, 40),
    'inventorytabicon':     (418, 632, 42, 40),
    'friendstabicon':       (528, 632, 42, 40),
    'friendstablock':       (546, 646, 22, 22),
    # 하단 3버튼
    'hostbutton':           (14, 706, 210, 152),
    'hostbuttonart':        (26, 712, 186, 130),
    'chapterbutton':        (232, 702, 218, 158),
    'chapterbuttonart':     (238, 708, 206, 136),
    'shopbutton':           (458, 706, 210, 152),
    'shopbuttonart':        (464, 712, 196, 130),
    'logolockup':           (248, 916, 194, 102),
    'ghostavatar':          (256, 294, 176, 220),
    'portalring':           (256, 600, 176, 52),
}

# 목업에서 그대로 복원 가능한 것 (사각 일러스트 — 알파 불필요)
RESTORE = {
    'hostbuttonart':    (202, 150),
    'chapterbuttonart': (216, 150),
    'shopbuttonart':    (202, 150),
    'battlepassart':    (90, 80),
    'dailyloginart':    (70, 64),
    'eventart':         (70, 64),
}


def quantize(im, colors=16):
    return im.convert('RGB').quantize(colors=colors, method=Image.MEDIANCUT,
                                      dither=Image.NONE).convert('RGB')


def main():
    for d in ('01_anchors', '02_restored'):
        os.makedirs(os.path.join(OUT, d), exist_ok=True)
    src = Image.open(os.path.join(MOCK, 'lobby_hub.jpeg')).convert('RGB')

    for name, (x, y, w, h) in LOBBY.items():
        c = src.crop((x, y, x + w, y + h))
        c.save(os.path.join(OUT, '01_anchors', f'{name}.png'))
        c.resize((w * 3, h * 3), Image.NEAREST).save(
            os.path.join(OUT, '01_anchors', f'{name}_x3.png'))

    for name, tgt in RESTORE.items():
        x, y, w, h = LOBBY[name]
        q = quantize(src.crop((x, y, x + w, y + h)), 16)
        if q.size != tgt:
            q = q.resize(tgt, Image.NEAREST)
        q.save(os.path.join(OUT, '02_restored', f'{name}.png'))

    print(f'앵커 {len(LOBBY)}종 / 복원본 {len(RESTORE)}종')


if __name__ == '__main__':
    main()
