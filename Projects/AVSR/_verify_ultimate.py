"""얼티밋 아이콘 12종 납품 검수 — 워크오더에 공표한 기준 그대로.

지난번 오판은 크기·알파만 보고 통과시킨 것이었다. 그래서 이번엔
**코드 도형 판정**(좌우대칭 + 고유색)과 **내용 밀도**를 함께 본다.
그리고 수치가 통과해도 마지막엔 반드시 눈으로 본다(_contact_sheet.py).
"""
import os
from PIL import Image, ImageChops, ImageStat

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')

HOSTS = ['amazoness', 'rambo', 'wizard', 'ninja', 'mafia', 'hitman',
         'yogamaster', 'dragon', 'robot', 'snowwoman', 'slugger', 'vampire']


def symmetry(im):
    """좌우 반전과의 일치도. 코드로 그린 도형은 거의 완벽한 대칭이 나온다."""
    a = im.convert('RGB')
    diff = ImageChops.difference(a, a.transpose(Image.FLIP_LEFT_RIGHT))
    return 1.0 - ImageStat.Stat(diff).mean[0] / 255.0


def density(im):
    """평균색에서 뚜렷하게 벗어난 픽셀 비율 — 거의 빈 이미지를 걸러낸다."""
    rgb = im.convert('RGB')
    mean = ImageStat.Stat(rgb).mean
    px = rgb.load()
    hit = 0
    for y in range(rgb.height):
        for x in range(rgb.width):
            p = px[x, y]
            if max(abs(p[i] - mean[i]) for i in range(3)) >= 18:
                hit += 1
    return hit / (rgb.width * rgb.height)


def main():
    fails = []
    print(f'{"파일":<32}{"크기":>10}{"대칭":>8}{"색":>6}{"알파":>7}{"밀도":>8}  판정')
    for h in HOSTS:
        name = f'ultimateicon_{h}.png'
        p = os.path.join(IN, name)
        if not os.path.exists(p):
            print(f'{name:<32}{"— 없음":>10}')
            fails.append((name, '미납품'))
            continue

        im = Image.open(p).convert('RGBA')
        sym = symmetry(im)
        colors = im.convert('RGB').getcolors(maxcolors=1 << 20)
        ncol = len(colors)
        lo = im.getchannel('A').getextrema()[0]
        den = density(im)

        bad = []
        if im.size != (64, 64):
            bad.append('크기')
        if sym >= 0.98 and ncol <= 6:
            bad.append('코드도형')
        if lo > 250:
            bad.append('알파없음')
        if den < 0.12:
            bad.append('밀도부족')

        print(f'{name:<32}{f"{im.width}x{im.height}":>10}{sym*100:>7.1f}%'
              f'{ncol:>6}{lo:>7}{den*100:>7.1f}%  ' + ('반려 ' + '·'.join(bad) if bad else 'OK'))
        if bad:
            fails.append((name, '·'.join(bad)))

    print()
    print(f'반려 {len(fails)}건' if fails else '수치 검사 12종 전부 통과 — 이제 눈으로 본다')


if __name__ == '__main__':
    main()
