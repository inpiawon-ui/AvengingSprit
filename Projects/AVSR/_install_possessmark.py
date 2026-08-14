"""납품된 빙의 표식 아이콘 4종을 게임에 설치한다 (기획서 1-5 A).

납품본은 256x256 이지만 실제 도트는 **32x32** 다 — 한 픽셀이 8x8 블록으로
확대돼 들어온다. 그래서 8배로 줄이면 원본 도트가 그대로 복원된다.
LANCZOS 로 줄이면 블록 경계가 섞여 흐려지므로 NEAREST 를 쓴다.

화면에서도 32px 로 띄우므로(Unit.MarkSize) 이 파일이 1:1 로 찍힌다.
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = os.path.join(HERE, '_exchange', 'in')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'InGameMainUI')

N = 32
NAMES = ('ready', 'target', 'banned', 'locked')


def main():
    for n in NAMES:
        name = f'possessmark_{n}.png'
        im = Image.open(os.path.join(SRC, name)).convert('RGBA')
        if im.size != (N, N):
            if im.width % N or im.height % N:
                raise SystemExit(f'{name} {im.size} — 32 의 배수가 아니다. 도트가 깨진다')
            im = im.resize((N, N), Image.NEAREST)
        out = os.path.join(DST, name)
        im.save(out)
        print(f'  {name} → {out}')


if __name__ == '__main__':
    main()
