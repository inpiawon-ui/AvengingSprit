"""유튜브 영상의 탐색용 미리보기 장면(스토리보드)을 받아 한 장으로 펼친다 — 레퍼런스 장면 찾기용 (2026-10-10).

python yt_storyboard.py <영상ID> [...]  → Projects/AVSR/_exchange/ref/refboard/sb/<ID>.png (+ 각 칸의 초)
영상을 통째로 받지 않고 유튜브가 탐색 막대에 쓰는 작은 장면 묶음만 받는다.
"""
import io
import json
import os
import re
import sys
import urllib.request

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '_exchange', 'ref', 'refboard', 'sb')
UA = {'User-Agent': 'Mozilla/5.0', 'Accept-Language': 'ko,en;q=0.8'}
FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)


def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=30).read()


def main(ids):
    os.makedirs(OUT, exist_ok=True)
    for vid in ids:
        html = get('https://www.youtube.com/watch?v=%s' % vid).decode('utf-8', 'ignore')
        m = re.search(r'"playerStoryboardSpecRenderer":\{"spec":"([^"]+)"', html)
        dur = re.search(r'"lengthSeconds":"(\d+)"', html)
        if not m:
            print(vid, 'no storyboard (private/removed?)')
            continue
        spec = m.group(1).replace('\\u0026', '&')
        parts = spec.split('|')
        base = parts[0]
        level = len(parts) - 2
        w, h, count, cols, rows, interval, name, sigh = parts[-1].split('#')
        w, h, count, cols, rows, interval = int(w), int(h), int(count), int(cols), int(rows), int(interval)
        seconds = int(dur.group(1)) if dur else 0
        step = interval / 1000 if interval else (seconds / max(1, count))
        per = cols * rows
        sheets = (count + per - 1) // per
        frames = []
        for k in range(sheets):
            url = base.replace('$L', str(level)).replace('$N', name.replace('$M', str(k))) + '&sigh=' + sigh
            try:
                sheet = Image.open(io.BytesIO(get(url))).convert('RGB')
            except Exception as e:
                print(vid, 'sheet', k, e)
                break
            for i in range(per):
                if len(frames) >= count:
                    break
                x, y = (i % cols) * w, (i // cols) * h
                if y + h > sheet.height:
                    break
                frames.append(sheet.crop((x, y, x + w, y + h)))
        n = len(frames)
        C = 10
        R = (n + C - 1) // C
        board = Image.new('RGB', (C * w, R * (h + 16)), (15, 15, 20))
        d = ImageDraw.Draw(board)
        for i, f in enumerate(frames):
            x, y = (i % C) * w, (i // C) * (h + 16)
            board.paste(f, (x, y))
            d.text((x + 2, y + h), '%d s' % round(i * step), font=FONT, fill=(255, 220, 120))
        p = os.path.join(OUT, '%s.png' % vid)
        board.save(p)
        print(vid, 'frames', n, 'step', round(step, 2), 'dur', seconds, '->', p)


if __name__ == '__main__':
    main(sys.argv[1:])
