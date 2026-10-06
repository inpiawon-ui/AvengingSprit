"""스킬 연출 영상 → 한 장 모아 보기 (2026-10-06).

Projects/AVSR/_exchange/skillfx_now/*.mp4 에서 영상마다 장면 8컷(0.75초 간격)을 뽑아 한 줄로 늘어놓는다.
영상이 제대로 찍혔는지(적 · 스킬 · 이펙트가 보이는지) 빠르게 훑어보는 용도다.
출력: Projects/AVSR/_exchange/skillfx_now/sheet_1.png · sheet_2.png (11줄씩)
"""
import os, glob, subprocess, tempfile
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
DIR = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'skillfx_now')
FONT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Fonts', 'NotoSansKR-Bold.ttf')
CUTS, STEP, W = 8, 0.75, 135          # 컷 수 · 간격(초) · 컷 폭(px)
H = W * 1280 // 720
LABEL = 150


def frames(path):
    out = []
    with tempfile.TemporaryDirectory() as tmp:
        for i in range(CUTS):
            png = os.path.join(tmp, f'{i}.png')
            subprocess.run(['ffmpeg', '-v', 'error', '-y', '-ss', f'{0.3 + STEP * i:.2f}', '-i', path,
                            '-frames:v', '1', '-vf', f'scale={W}:{H}', png], check=False)
            out.append(Image.open(png).convert('RGB') if os.path.exists(png) else Image.new('RGB', (W, H)))
    return out


def main():
    vids = sorted(glob.glob(os.path.join(DIR, '*.mp4')))
    font = ImageFont.truetype(FONT, 22)
    for part in range(0, len(vids), 11):
        chunk = vids[part:part + 11]
        sheet = Image.new('RGB', (LABEL + W * CUTS, H * len(chunk)), (20, 24, 36))
        d = ImageDraw.Draw(sheet)
        for r, v in enumerate(chunk):
            d.text((8, r * H + 8), os.path.basename(v)[:-4].replace('_', '\n', 1), fill=(255, 255, 255), font=font)
            for c, im in enumerate(frames(v)):
                sheet.paste(im, (LABEL + c * W, r * H))
        out = os.path.join(DIR, f'sheet_{part // 11 + 1}.png')
        sheet.save(out)
        print(out, sheet.size)


if __name__ == '__main__':
    main()
