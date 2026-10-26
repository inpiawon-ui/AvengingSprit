# 결과창 「조각 보상」 표현 시안 — 실제 게임 캡처(20:9, 1080 폭 = 시안 좌표 그대로) 위에 보상 자리만 다시 짠다.
# 그림은 키트 · 게임 원본만 쓴다(칸 판 · 등급 테 · 등급 꼬리표 · 조각 아이콘 · 몸 얼굴). 배치 · 글자만 이 스크립트 몫.
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = "C:/won/UnityProject/AvengingSprit"
KIT = ROOT + "/Assets/BaseResource/ReBornKit/"
FACE = ROOT + "/Assets/BaseResource/Growth/Faces/face_{}.png"
FONT = ROOT + "/Assets/BaseResource/Fonts/ReBorn/"
PLAY = ROOT + "/Projects/AVSR/_exchange/ref/ui_reborn_1023/play/"
OUT = ROOT + "/Projects/AVSR/_exchange/ref/shard_mock/"

INK = (237, 240, 246)
MUTED = (171, 178, 206)
CYAN = (84, 220, 235)
GOLD = (239, 198, 103)
GRADE = {"B": "r", "A": "sr", "S": "ssr"}
RAR_COL = {"r": (79, 128, 177), "sr": (160, 110, 220), "ssr": (230, 180, 70)}

HOSTS4 = [("amazon", "아마존", "B", 3), ("gangster", "갱스터", "B", 2), ("ninja", "닌자", "A", 2), ("medium", "영매", "S", 1)]
HOSTS6 = HOSTS4 + [("guru", "구루", "B", 1), ("robot", "로봇", "A", 1)]


def font(name, size):
    return ImageFont.truetype(FONT + name, size)


BOLD = lambda s: font("Pretendard-Bold.otf", s)
SEMI = lambda s: font("Pretendard-SemiBold.otf", s)
PIX = lambda s: font("Galmuri11-Bold.ttf", s)


def kit(path, scale=1.5):
    im = Image.open(KIT + path + ".png").convert("RGBA")
    return im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS) if scale != 1 else im


def nine(path, w, h, border):
    """9분할로 늘린다(키트 판은 720 기준 — 1080 시안이라 1.5 배로 키운 뒤 자른다)."""
    src = kit(path)
    b = border
    sw, sh = src.size
    out = Image.new("RGBA", (w, h))
    xs = [(0, b, 0, b), (b, sw - b, b, w - b), (sw - b, sw, w - b, w)]
    ys = [(0, b, 0, b), (b, sh - b, b, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 <= dx0 or dy1 <= dy0:
                continue
            piece = src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS)
            out.alpha_composite(piece, (dx0, dy0))
    return out


def face(key, size):
    im = Image.open(FACE.format(key)).convert("RGBA")
    return im.resize((size, size), Image.NEAREST)


def text(d, xy, s, f, fill, anchor="la"):
    d.text(xy, s, font=f, fill=fill, anchor=anchor)


def base():
    """클리어 캡처에서 보상 자리(1140~1860)를 배경 그림으로 덮어 비운다."""
    cap = Image.open(PLAY + "result_clear_20x9.png").convert("RGBA")
    bg = Image.open(KIT + "Art/rb_art_result_clear_bg_quiet.png").convert("RGBA")
    k = max(1080 / bg.width, 2400 / bg.height)
    bg = bg.resize((round(bg.width * k), round(bg.height * k)), Image.LANCZOS)
    ox, oy = (bg.width - 1080) // 2, (bg.height - 2400) // 2
    bg = bg.crop((ox, oy, ox + 1080, oy + 2400))
    cap.paste(bg.crop((0, 1130, 1080, 1860)), (0, 1130))
    return cap


def currency_cells(im, d, y0, y1):
    """골드 · 유령 경험치 — 가로로 긴 두 칸(아이콘 왼쪽 · 값 오른쪽)."""
    for i, (icon, val, label) in enumerate([("Icons/rb_icon_currency_gold_144", "432", "골드"), ("Icons/rb_icon_stat_attack_144", "30", "유령 경험치")]):
        x0 = 28 + i * 524
        im.alpha_composite(nine("Components/rb_result_cell", 500, y1 - y0, 24), (x0, y0))
        ic = kit(icon, 1.0).resize((120, 120), Image.NEAREST)
        im.alpha_composite(ic, (x0 + 40, y0 + (y1 - y0 - 120) // 2))
        text(d, (x0 + 190, y0 + 30), val, PIX(56), INK)
        text(d, (x0 + 192, y0 + 100), label, SEMI(36), MUTED)


def host_card(im, d, x0, y0, w, h, key, name, grade, n):
    """몸 카드(소환 결과와 같은 말씨) — 등급 테 · 등급 꼬리표 · 얼굴 · 이름 · 조각 ×n."""
    r = GRADE[grade]
    im.alpha_composite(nine("Components/rb_result_cell", w, h, 24), (x0, y0))
    im.alpha_composite(nine(f"Components/rb_host_frame_{r}", w, h, 30), (x0, y0))
    tag = kit(f"Components/rb_rarity_{r}", 1.2)
    im.alpha_composite(tag, (x0 + 10, y0 + 10))
    fs = 118
    im.alpha_composite(face(key, fs), (x0 + (w - fs) // 2, y0 + 44))
    text(d, (x0 + w // 2, y0 + 176), name, BOLD(34), INK, "ma")
    sh = kit("Icons/rb_icon_shard_48", 1.0).resize((40, 40), Image.NEAREST)
    label = f"×{n}"
    tw = d.textlength(label, font=PIX(36))
    sx = x0 + (w - (40 + 6 + tw)) / 2
    im.alpha_composite(sh, (int(sx), y0 + 220))
    text(d, (sx + 46, y0 + 222), label, PIX(36), INK)


def option_a(hosts, scroll=False):
    """A — 골드 · 경험치 두 칸 + 아래 몸 카드 줄(소환 결과와 같은 카드). 6종 넘으면 옆으로 넘김."""
    im = base()
    d = ImageDraw.Draw(im)
    text(d, (28, 1146), "획득 보상", BOLD(48), INK)
    currency_cells(im, d, 1222, 1372)
    text(d, (28, 1404), f"몸 조각  {len(hosts)}종", BOLD(40), INK)
    total = sum(h[3] for h in hosts)
    text(d, (1052, 1408), f"합계 ×{total}", PIX(36), MUTED, "ra")
    w, gap = 190, 18
    shown = hosts if len(hosts) <= 5 else hosts
    n = len(shown)
    row_w = n * w + (n - 1) * gap
    x = 28 if (scroll or n == 5) else (1080 - row_w) // 2
    for key, name, grade, cnt in shown:
        if x > 1080:
            break
        host_card(im, d, x, 1460, w, 280, key, name, grade, cnt)
        x += w + gap
    if scroll:
        # 화면 밖으로 넘친 카드는 옆으로 밀어 본다 — 오른쪽 끝을 살짝 어둡게(받은 그림 rb_fade_bottom 을 돌려 씀)
        fade = kit("Components/rb_fade_bottom", 1.0).rotate(90, expand=True).resize((90, 280))
        im.alpha_composite(fade, (990, 1460))
    return im


def option_b(hosts):
    """B — 지금 3칸 그대로 · 조각 칸 하나에 얼굴을 겹쳐 모은다(「몸 조각 4종 ×8」, 누르면 목록)."""
    im = base()
    d = ImageDraw.Draw(im)
    text(d, (28, 1146), "획득 보상", BOLD(48), INK)
    cells = [(28, "Icons/rb_icon_currency_gold_144", "432", "골드"), (377, None, None, None), (727, "Icons/rb_icon_stat_attack_144", "30", "유령 경험치")]
    for x0, icon, val, label in cells:
        im.alpha_composite(nine("Components/rb_result_cell", 325, 400, 24), (x0, 1214))
        if icon:
            im.alpha_composite(kit(icon, 1.0), (x0 + 91, 1278))
            text(d, (x0 + 162, 1440), val, PIX(44), INK, "ma")
            text(d, (x0 + 162, 1508), label, SEMI(36), INK, "ma")
    # 조각 칸 — 얼굴 셋 겹침 + 「+1」 · 합계 · 「몸 조각 4종」
    x0 = 377
    fs, step = 96, 62
    show = hosts[:3]
    left = x0 + (325 - (fs + step * (len(show) - 1))) // 2
    for i, (key, name, grade, cnt) in enumerate(show):
        r = GRADE[grade]
        fx = left + i * step
        ring = nine(f"Components/rb_host_frame_{r}", fs + 8, fs + 8, 22)
        dark = nine("Components/rb_lv_artwell", fs + 8, fs + 8, 20)
        im.alpha_composite(dark, (fx - 4, 1286))
        im.alpha_composite(face(key, fs), (fx, 1290))
        im.alpha_composite(ring, (fx - 4, 1286))
    if len(hosts) > 3:
        tag = nine("Components/rb_lv_tag", 70, 46, 14)
        im.alpha_composite(tag, (x0 + 240, 1270))
        text(d, (x0 + 275, 1275), f"+{len(hosts) - 3}", PIX(32), INK, "ma")
    total = sum(h[3] for h in hosts)
    sh = kit("Icons/rb_icon_shard_48", 1.0).resize((40, 40), Image.NEAREST)
    im.alpha_composite(sh, (x0 + 110, 1442))
    text(d, (x0 + 156, 1440), f"×{total}", PIX(44), INK)
    text(d, (x0 + 162, 1508), f"몸 조각 {len(hosts)}종", SEMI(36), INK, "ma")
    text(d, (x0 + 162, 1560), "눌러서 보기 ›", SEMI(30), CYAN, "ma")
    return im


def slot(im, d, x0, y0, s, frame, inner, value, name):
    im.alpha_composite(nine(frame, s, s + 6, 26), (x0, y0))
    im.alpha_composite(inner, (x0 + (s - inner.width) // 2, y0 + 22))
    text(d, (x0 + s // 2, y0 + s - 40), value, PIX(34), INK, "ma")
    if name:
        text(d, (x0 + s // 2, y0 + s + 14), name, SEMI(30), MUTED, "ma")


def option_c(hosts):
    """C — 같은 모양 네모 칸 격자(한 줄 5칸). 재화는 남색 칸, 몸 조각은 등급색 칸 + 얼굴 + 조각 배지."""
    im = base()
    d = ImageDraw.Draw(im)
    text(d, (28, 1146), "획득 보상", BOLD(48), INK)
    s, gap = 186, 23
    items = [("Components/rb_slot_common", kit("Icons/rb_icon_currency_gold_144", 1.0).resize((96, 96), Image.NEAREST), "432", "골드"),
             ("Components/rb_slot_common", kit("Icons/rb_icon_stat_attack_144", 1.0).resize((96, 96), Image.NEAREST), "30", "유령 경험치")]
    slot_of = {"r": "rb_slot_rare", "sr": "rb_slot_sr", "ssr": "rb_slot_ssr"}
    for key, name, grade, cnt in hosts:
        f = face(key, 96).copy()
        badge = kit("Icons/rb_icon_shard_48", 1.0).resize((40, 40), Image.NEAREST)
        f.alpha_composite(badge, (0, 56))
        items.append((f"Components/{slot_of[GRADE[grade]]}", f, f"×{cnt}", name))
    for i, (frame, inner, value, name) in enumerate(items):
        row, col = divmod(i, 5)
        x0 = 28 + col * (s + gap)
        y0 = 1222 + row * (s + 70)
        slot(im, d, x0, y0, s, frame, inner, value, name)
    return im


def label(im, title, sub):
    """비교판 머리 — 안 이름 · 한 줄 설명."""
    head = Image.new("RGBA", (im.width, 150), (14, 18, 30, 255))
    d = ImageDraw.Draw(head)
    text(d, (30, 22), title, BOLD(56), GOLD)
    text(d, (30, 96), sub, SEMI(34), INK)
    out = Image.new("RGBA", (im.width, im.height + 150))
    out.paste(head, (0, 0))
    out.paste(im, (0, 150))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    shots = [
        (option_a(HOSTS4), "A안 · 몸 카드 줄", "소환 결과와 같은 몸 카드(등급 · 얼굴 · 이름 · 조각 ×n)"),
        (option_a(HOSTS6, scroll=True), "A안 · 6종일 때", "5장이 넘으면 옆으로 밀어 본다"),
        (option_b(HOSTS4), "B안 · 조각 한 칸에 모음", "지금 3칸 유지 · 얼굴 겹침 + 「+1」 · 누르면 목록"),
        (option_c(HOSTS6), "C안 · 같은 모양 칸 격자", "재화 · 몸 조각을 같은 네모 칸에 · 몸은 등급색 칸"),
    ]
    for i, (im, t, s) in enumerate(shots):
        label(im, t, s).convert("RGB").save(OUT + f"shard_option_{i + 1}.png")
    # 한 장 비교판 — 보상 자리만 확대(1100 ~ 1900)
    crops = [label(im.crop((0, 1100, 1080, 1900)), t, s) for im, t, s in shots]
    W = sum(c.width for c in crops) + 30 * (len(crops) - 1)
    sheet = Image.new("RGB", (W, crops[0].height), (40, 0, 40))
    x = 0
    for c in crops:
        sheet.paste(c.convert("RGB"), (x, 0))
        x += c.width + 30
    sheet.save(OUT + "shard_options_sheet.png")
    print("ok")


if __name__ == "__main__":
    main()
