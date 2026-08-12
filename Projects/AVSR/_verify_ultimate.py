"""얼티밋 아이콘 12종 납품 검수 — 워크오더 16차에 공표한 기준 그대로.

기준값은 지어낸 것이 아니라 **이미 완성된 아이콘 9장을 실측한 값**이다.
9장이 전부 반투명 0px · 14~15색 · 채움 26~63% 였다.

지난 오판은 크기·알파만 보고 통과시킨 것이었다. 그래서
**코드 도형 판정**(좌우대칭 + 고유색)과 **내용 밀도**를 함께 본다.
수치가 통과해도 마지막엔 반드시 9장 옆에 놓고 눈으로 본다.
"""
import io
import os
import sys

from PIL import Image, ImageChops, ImageStat

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
IN = os.path.join(HERE, '_exchange', 'in')

HOSTS = ['amazon', 'amazon_elite', 'hopper', 'hopper_smg',
         'commando_mg', 'commando_laser', 'commando_grenade',
         'dragoon', 'dragon_blue', 'medium', 'ninja_chain', 'snowwoman']

COLOR_MAX = 20      # 9장 실측 14~15. 20 을 넘으면 그라데이션을 쪼갠 것이다
FILL_MIN = 0.20     # 9장 실측 최저 26%. 여유를 두고 20%
SEMI_MAX = 0        # 9장 전부 정확히 0px


def symmetry(im):
    """좌우 반전과의 일치도. 코드로 그린 도형은 거의 완벽한 대칭이 나온다."""
    a = im.convert('RGB')
    diff = ImageChops.difference(a, a.transpose(Image.FLIP_LEFT_RIGHT))
    return 1.0 - ImageStat.Stat(diff).mean[0] / 255.0


def stats(im):
    """(불투명 비율, 반투명 픽셀 수, 불투명 영역의 색 수)"""
    px = im.load()
    solid = semi = 0
    cols = set()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a >= 250:
                solid += 1
                cols.add((r, g, b))
            elif a > 5:
                semi += 1
    return solid / (im.width * im.height), semi, len(cols)


def main():
    fails = []
    print(f'{"파일":<34}{"크기":>10}{"채움":>8}{"색":>6}{"반투명":>8}{"대칭":>8}  판정')
    for h in HOSTS:
        name = f'ultimateicon_{h}.png'
        p = os.path.join(IN, name)
        if not os.path.exists(p):
            print(f'{name:<34}{"— 없음":>10}')
            fails.append((name, '미납품'))
            continue

        im = Image.open(p).convert('RGBA')
        fill, semi, ncol = stats(im)
        sym = symmetry(im)

        bad = []
        if im.size != (64, 64):
            bad.append('크기')
        if semi > SEMI_MAX:
            bad.append('반투명')
        if ncol > COLOR_MAX:
            bad.append('색과다')
        if fill < FILL_MIN:
            bad.append('밀도부족')
        # 코드 도형 판정 — 대칭이 거의 완벽한데 색이 몇 개 없다
        if sym >= 0.98 and ncol <= 6:
            bad.append('코드도형')

        print(f'{name:<34}{f"{im.width}x{im.height}":>10}{fill*100:>7.1f}%'
              f'{ncol:>6}{semi:>8}{sym*100:>7.1f}%  '
              + ('반려 ' + '·'.join(bad) if bad else 'OK'))
        if bad:
            fails.append((name, '·'.join(bad)))

    print()
    if fails:
        print(f'반려 {len(fails)}건')
    else:
        print('수치 검사 12종 전부 통과 — 이제 9장 옆에 놓고 눈으로 본다')


if __name__ == '__main__':
    main()
