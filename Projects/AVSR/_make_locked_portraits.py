"""잠금 표시용 회색 초상을 굽는다.

지금은 초상을 거의 검정(0.10,0.10,0.14)으로 눌러 실루엣만 남긴다. 그러면
어떤 캐릭터인지 안 보인다. **누구인지는 보이되 아직 못 쓴다**는 걸 알리려면
형태를 살리고 색만 빼야 한다.

색 틴트로는 안 된다 — 곱셈이라 어두워질 뿐 채도는 그대로다. UI 셰이더에
채도 조절이 없어 진짜 회색을 만들려면 셰이더를 새로 넣어야 하는데, 그러면
빌드 포함(Resources / Always Included) 처리라는 기제가 하나 더 는다.
이 프로젝트는 폴더에 PNG 를 넣으면 아틀라스·주소가 자동으로 붙으므로,
**회색본을 구워 두는 쪽**이 기존 방식에 그대로 얹힌다.
"""
import os

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.abspath(os.path.join(HERE, '..', '..',
                                   'Assets', 'BaseResource', 'HostSelectPanel'))

KEYS = ['amazoness', 'rambo', 'wizard', 'ninja', 'mafia', 'hitman',
        'yogamaster', 'dragon', 'robot', 'snowwoman', 'slugger', 'vampire']
PREFIXES = ['hostslotportrait', 'hostportraitimage']

DIM = 0.72   # 회색으로 만든 뒤 이만큼 낮춘다. 해금본과 나란히 놓았을 때 눌려 보이게


def to_grey(im):
    px = im.load()
    w, h = im.size
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            # 사람 눈이 느끼는 밝기. 단순 평균으로 하면 초록이 지나치게 밝아진다.
            v = int(round((0.299 * r + 0.587 * g + 0.114 * b) * DIM))
            v = max(0, min(255, v))
            op[x, y] = (v, v, v, a)
    return out


def main():
    n = 0
    for pre in PREFIXES:
        for k in KEYS:
            src = os.path.join(SRC, f'{pre}_{k}.png')
            if not os.path.exists(src):
                print(f'  없음: {pre}_{k}.png')
                continue
            dst = os.path.join(SRC, f'{pre}_{k}_locked.png')
            to_grey(Image.open(src).convert('RGBA')).save(dst)
            n += 1
    print(f'회색 초상 {n}장 생성')


if __name__ == '__main__':
    main()
