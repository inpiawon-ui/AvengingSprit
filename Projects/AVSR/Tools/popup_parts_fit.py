# -*- coding: utf-8 -*-
"""공통 팝업 부품 앉히기 (2026-10-06).

코덱스가 마젠타 바탕으로 그려 온 부품에서 바탕을 빼고(투명), 잘라서, 게임 크기로 맞춘다.
그림을 고치지 않는다 — 바탕 빼기 · 자르기 · 줄이기만 한다.

  in/popup_frame.png          → Assets/BaseResource/SystemPopup/popup_frame.png        (폭 600, 9-slice)
  in/popup_groove.png         → Assets/BaseResource/SystemPopup/popup_groove.png       (폭 600, 9-slice — 버튼 하나짜리 알림의 얇은 홈)
  in/popup_button_cancel.png  → Assets/BaseResource/SystemPopup/popup_button_cancel.png (290 x 80 — 노란 버튼과 같은 크기)
  in/popup_button_confirm.png → Assets/BaseResource/SystemPopup/popup_button_confirm.png (290 x 80 — 취소와 짝)
  in/stat_icons_critdmg_def.png → Assets/BaseResource/Growth/icon_critdmg.png · icon_def.png (50 x 43 — 기존 능력치 아이콘 칸)

쓰는 법: python Projects/AVSR/Tools/popup_parts_fit.py [frame|groove|cancel|confirm|icons ...]  (없으면 전부)
"""
import os, sys
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Assets', 'BaseResource', 'SystemPopup')
GROWTH = os.path.join(ROOT, 'Assets', 'BaseResource', 'Growth')


def key_out(im):
    """바탕색(네 귀퉁이의 평균)과 가까운 픽셀을 투명하게. 가장자리는 반투명 + 마젠타 기운을 걷는다."""
    im = im.convert('RGBA')
    w, h = im.size
    px = im.load()
    corners = [px[2, 2], px[w - 3, 2], px[2, h - 3], px[w - 3, h - 3]]
    br = sum(c[0] for c in corners) / 4
    bg_ = sum(c[1] for c in corners) / 4
    bb = sum(c[2] for c in corners) / 4
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            d = ((r - br) ** 2 + (g - bg_) ** 2 + (b - bb) ** 2) ** 0.5
            if d < 60:
                px[x, y] = (0, 0, 0, 0)
            elif d < 150:
                # 가장자리 — 바탕과 섞인 만큼 투명하게, 마젠타 기운(빨강 · 파랑이 초록보다 큰 몫)을 걷는다
                t = (d - 60) / 90.0
                m = min(r, b)
                if m > g:
                    r -= int((m - g) * (1 - t))
                    b -= int((m - g) * (1 - t))
                px[x, y] = (max(0, r), g, max(0, b), int(a * t))
    return im


def trim(im):
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im


def fit_width(im, width):
    w, h = im.size
    return im.resize((width, max(1, round(h * width / w))), Image.LANCZOS)


def save(im, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)
    print('saved', os.path.relpath(path, ROOT), im.size)


def frame():
    save(fit_width(trim(key_out(Image.open(os.path.join(IN, 'popup_frame.png')))), 600),
         os.path.join(OUT, 'popup_frame.png'))


def groove():
    """볼트 달린 받침(popup_tray)은 시안과 달라 뺐다(2026-10-06) — 시안의 버튼 하나짜리 알림은 얇은 홈이다."""
    save(fit_width(trim(key_out(Image.open(os.path.join(IN, 'popup_groove.png')))), 600),
         os.path.join(OUT, 'popup_groove.png'))


def cancel():
    im = trim(key_out(Image.open(os.path.join(IN, 'popup_button_cancel.png'))))
    save(im.resize((290, 80), Image.LANCZOS), os.path.join(OUT, 'popup_button_cancel.png'))


def confirm():
    """취소 버튼과 짝으로 받은 노란 확인 버튼(2026-10-06). 예전 노란 버튼(button_yellow_wide)은
    다른 화면에서 떠 온 그림이라 모서리에 그 화면의 선 조각이 붙어 있어 쓰지 않는다."""
    im = trim(key_out(Image.open(os.path.join(IN, 'popup_button_confirm.png'))))
    save(im.resize((290, 80), Image.LANCZOS), os.path.join(OUT, 'popup_button_confirm.png'))


def icons():
    im = key_out(Image.open(os.path.join(IN, 'stat_icons_critdmg_def.png')))
    w, h = im.size
    for name, box in (('icon_critdmg', (0, 0, w // 2, h)), ('icon_def', (w // 2, 0, w, h))):
        part = trim(im.crop(box))
        # 기존 능력치 아이콘 칸(50 x 43)에 비율을 지켜 앉힌다
        part.thumbnail((50, 43), Image.LANCZOS)
        canvas = Image.new('RGBA', (50, 43), (0, 0, 0, 0))
        canvas.paste(part, ((50 - part.width) // 2, (43 - part.height) // 2), part)
        save(canvas, os.path.join(GROWTH, name + '.png'))


JOBS = {'frame': frame, 'groove': groove, 'cancel': cancel, 'confirm': confirm, 'icons': icons}
for job in (sys.argv[1:] or list(JOBS)):
    JOBS[job]()
