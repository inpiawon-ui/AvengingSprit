"""레퍼런스 그림판 — 다른 게임 영상에서 실제 연출 순간을 캡처해 번호 · 게임 · 볼 점을 붙인다 (2026-10-10).

PD 「이미지가 하나도 안 보여서 선택을 못 하겠어, 이미지로 보여줘」 — 썸네일(유튜버 얼굴 · 제목)은 고를 수가 없었다.
캡처: 내장 브라우저에서 영상을 그 초로 옮겨 멈추고 화면을 찍었다(유튜브 영상은 받지 않음).
python make_refboard_shots.py <캡처 폴더> → Projects/AVSR/_exchange/ref/refboard/shots_{A,B,C}.png + cap/ 사본
"""
import os
import shutil
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(HERE, '..', '_exchange', 'ref', 'refboard')
CAP = os.path.join(REF, 'cap')
BOLD = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 24)
REG = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 19)
TITLE = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 34)

CR, BS, VS, A2 = '클래시 로얄', '브롤스타즈', '뱀파이어 서바이버즈(도트)', '아처로 2'

# (번호, 캡처 파일 꼬리, 게임, 영상 · 초, 볼 점, 수동 자르기(x0, y0, x1, y1) 또는 None = 검은 테 자동 제거)
BOARDS = [
    ('A', 'A. 결과창 보상 — 등급이 오를수록 무엇이 더해지나', [
        (1, '6nd2h4', CR, 'Vd_MMH5CgR0 · 0:02', '일반 — 바탕 + 작은 반짝 몇 개뿐', (198, 0, 602, 720)),
        (2, 'ciermd', CR, 'Vd_MMH5CgR0 · 0:18', '레어 — 바탕색이 바뀌고 위에서 빛줄기', (198, 0, 602, 720)),
        (3, 'zhlqcr', CR, 'Vd_MMH5CgR0 · 0:34', '에픽 — 등급색 고리가 퍼져 나감', (198, 0, 602, 720)),
        (4, 'envg65', CR, 'Vd_MMH5CgR0 · 0:50', '전설 — 무지개 바탕 + 흰 고리', (198, 0, 602, 720)),
        (5, 'dqelvf', CR, 'Vd_MMH5CgR0 · 0:51', '전설 터짐 — 고리 두 겹, 화면이 하얘짐', (198, 0, 602, 720)),
        (6, 'l20qv8', CR, 'jKo4B2_nejQ · 0:19', '놓인 상자 밑 별 개수로 등급 예고', (242, 0, 570, 731)),
        (7, 'cz9sg1', CR, 'jKo4B2_nejQ · 0:52', '별 2개 상자 — 틈새로 빛이 샘', (238, 0, 562, 720)),
        (8, 'rrsg5e', CR, 'jKo4B2_nejQ · 0:27', '별 4개 상자 — 무지개 바탕 + 강한 빛', (238, 0, 562, 720)),
        (9, 'vhxjir', BS, 'fBzCRUMtW5I · 0:16', '에픽 — 보라 바탕, 반짝 별이 흩날림', (100, 0, 480, 273)),
        (10, '5kiglp', BS, 'fBzCRUMtW5I · 1:20', '전설 대기 — 노랑 바탕 + 뒤 광원만', (100, 0, 480, 270)),
        (11, '6pe88d', BS, 'fBzCRUMtW5I · 4:05', '전설 열림 — 색 빛살 + 고리 + 섬광', (0, 0, 470, 270)),
    ]),
    ('B', 'B. 상자 열기 — 도트 게임 · 모바일', [
        (12, 'u1c1ck', VS, 'dzD5cr_7Rqs · 7:31', '도트 상자 대기 — 연출 없음, 창 하나', (270, 40, 530, 400)),
        (13, '7w395d', VS, 'Jioh5sWZXQQ · 0:39', '열리는 순간 — 도트 불꽃놀이 알갱이', (270, 40, 540, 410)),
        (14, '5q06gj', VS, 'Jioh5sWZXQQ · 0:44', '절정 — 빛기둥 + 금화 비 + 화면 노랗게', (40, 0, 760, 450)),
        (15, 'bj9mou', VS, 'Jioh5sWZXQQ · 0:54', '아이템 5개 — 부채꼴 빛살', (270, 40, 530, 400)),
        (16, '383ffz', VS, 'Jioh5sWZXQQ · 0:59', '끝 — 아이템마다 도트 반짝 별', (270, 40, 530, 400)),
        (17, 'n3uqh0', VS, 'dzD5cr_7Rqs · 1:25', '아이템 1개 — 반짝 별 하나로 끝', (270, 40, 540, 410)),
        (18, 'mpkqbv', A2, 'Q_oqd378H3s · 1:16', '열린 상자 위로 아이템 + 큰 반짝 별', (0, 0, 436, 880)),
        (19, 'ljlvss', A2, 'Q_oqd378H3s · 1:05', '결과 — 등급색 칸 + 색종이', (0, 0, 436, 880)),
    ]),
    ('C', 'C. 레벨업 카드 · 고른 뒤 캐릭터', [
        (20, 'gdoa66', A2, 'nhJMWJRWq30 · 2:00', '에픽 카드 등장 — 등급색 빛기둥이 내려옴', (0, 0, 436, 880)),
        (21, '0v8a7p', A2, 'nhJMWJRWq30 · 2:01', '에픽 머묾 — 카드 뒤 보라 기둥이 남음', (0, 0, 436, 880)),
        (22, 'sjm2kb', A2, 'nhJMWJRWq30 · 7:10', '보통 등급 — 초록 테두리만, 빛 없음', (290, 0, 510, 450)),
        (23, 'oqiz69', A2, 'nhJMWJRWq30 · 4:50', '전설 — 금 테두리 · 날개 장식, 카드 자체가 화려', (0, 0, 436, 880)),
        (24, 'kzxy0w', VS, 'dzD5cr_7Rqs · 1:36', '도트 레벨업 — 목록만, 연출 거의 없음', (270, 40, 535, 400)),
        (25, 'w3m5xn', A2, 'nhJMWJRWq30 · 20:52', '고른 뒤 — 몸에 빛을 두르지 않고 새 공격이 바로 보임', (0, 0, 436, 880)),
        (26, 'gsnn62', A2, 'nhJMWJRWq30 · 37:30', '쌓인 버프 — 공격 · 몸 둘레 효과로 드러남', (0, 0, 436, 880)),
    ]),
]

CELL_H = 520
LABEL_H = 118
PAD = 16
COLS = 6


def find(tail, src):
    for f in os.listdir(src):
        if f.endswith(tail + '.jpg'):
            return os.path.join(src, f)
    raise FileNotFoundError(tail)


def trim_black(im):
    bg = Image.new('RGB', im.size, (0, 0, 0))
    diff = ImageChops.difference(im, bg).convert('L').point(lambda v: 255 if v > 24 else 0)
    box = diff.getbbox()
    if not box:
        return im
    # 오른쪽 스크롤 막대 · 아래 조회수 줄 같은 얇은 띠는 남아도 그냥 둔다
    return im.crop(box)


def wrap(text, width_px, font, draw):
    lines, line = [], ''
    for ch in text:
        if draw.textlength(line + ch, font=font) > width_px:
            lines.append(line)
            line = ch
        else:
            line += ch
    lines.append(line)
    return lines


def main(src):
    os.makedirs(CAP, exist_ok=True)
    for key, title, rows in BOARDS:
        cells = []
        for num, tail, game, where, note, box in rows:
            path = find(tail, src)
            dst = os.path.join(CAP, '%02d_%s.jpg' % (num, tail))
            shutil.copyfile(path, dst)
            im = Image.open(path).convert('RGB')
            im = im.crop(box) if box else trim_black(im)
            w = round(im.width * CELL_H / im.height)
            cells.append((num, game, where, note, im.resize((w, CELL_H), Image.LANCZOS)))
        # 줄 나누기 — 한 줄 폭 2400 안쪽
        lines, cur, cur_w = [], [], 0
        for c in cells:
            cw = max(c[4].width, 300)
            if cur and (cur_w + cw + PAD > 2400 or len(cur) >= COLS):
                lines.append(cur)
                cur, cur_w = [], 0
            cur.append(c)
            cur_w += cw + PAD
        if cur:
            lines.append(cur)
        W = max(sum(max(c[4].width, 300) + PAD for c in ln) for ln in lines) + PAD
        H = 80 + len(lines) * (CELL_H + LABEL_H + PAD) + PAD
        s = Image.new('RGB', (W, H), (22, 24, 32))
        d = ImageDraw.Draw(s)
        d.text((PAD, 20), title, font=TITLE, fill=(255, 225, 140))
        y = 80
        for ln in lines:
            x = PAD
            for num, game, where, note, im in ln:
                cw = max(im.width, 300)
                s.paste(im, (x + (cw - im.width) // 2, y))
                # 번호 딱지
                d.rectangle((x, y, x + 54, y + 40), fill=(255, 200, 60))
                d.text((x + 8, y + 3), '%d' % num, font=BOLD, fill=(20, 20, 20))
                d.text((x, y + CELL_H + 6), game, font=BOLD, fill=(240, 240, 240))
                for k, t in enumerate(wrap(note, cw, REG, d)[:2]):
                    d.text((x, y + CELL_H + 38 + k * 24), t, font=REG, fill=(200, 210, 225))
                d.text((x, y + CELL_H + 90), where, font=REG, fill=(120, 130, 150))
                x += cw + PAD
            y += CELL_H + LABEL_H + PAD
        out = os.path.join(REF, 'shots_%s.png' % key)
        s.save(out)
        print(out, s.size)


if __name__ == '__main__':
    main(sys.argv[1])
