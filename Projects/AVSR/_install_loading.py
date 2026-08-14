"""로딩 화면 그림을 설치한다 (배경 N장 + 제자리 유령 루프 N프레임).

파일 이름이 곧 계약이다 — `GhostLoadingView` 가 `loading_bg_1..`, `loading_ghost_1..`
을 번호가 끊길 때까지 찾아 쓴다. 장 수는 코드가 아니라 이 폴더가 정한다.

배경은 720x1280 그대로 넣고, 유령은 4배 블록으로 그려 온 384px 을 96px 로 되돌린다
(NEAREST — LANCZOS 로 줄이면 블록 경계가 섞여 도트가 흐려진다).
화면에서는 96px 을 정확히 2배(192)로 띄운다.
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(HERE, '_exchange', 'in')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'Loading')

# 납품 파일명 → 설치 파일명
BACKGROUNDS = {
    'loadingbackground.png':  'loading_bg_1.png',
    'loadingbackground2.png': 'loading_bg_2.png',
    'loadingbackground3.png': 'loading_bg_3.png',
}
GHOST_FRAMES = 4
GHOST_SIZE = 96


def install_background(src_name, dst_name):
    im = Image.open(os.path.join(SRC, src_name)).convert('RGBA')
    if im.size != (720, 1280):
        raise SystemExit(f'{src_name} {im.size} — 720x1280 이 아니다')
    im.save(os.path.join(DST, dst_name))
    print(f'  {dst_name} {im.size}')


def install_ghost(i):
    name = f'loading_ghost_{i}.png'
    im = Image.open(os.path.join(SRC, name)).convert('RGBA')
    if im.size != (GHOST_SIZE, GHOST_SIZE):
        if im.width % GHOST_SIZE or im.height % GHOST_SIZE:
            raise SystemExit(f'{name} {im.size} — 96 의 배수가 아니다. 도트가 깨진다')
        im = im.resize((GHOST_SIZE, GHOST_SIZE), Image.NEAREST)
    im.save(os.path.join(DST, name))
    print(f'  {name} {im.size}')


def main():
    os.makedirs(DST, exist_ok=True)
    for src, dst in BACKGROUNDS.items():
        if os.path.exists(os.path.join(SRC, src)):
            install_background(src, dst)
    for i in range(1, GHOST_FRAMES + 1):
        install_ghost(i)


if __name__ == '__main__':
    main()
