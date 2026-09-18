"""NotoSansKR 가변 폰트(기본 굵기 100 = Thin)에서 굵은 고정 폰트를 뽑는다.

로비 시안 글자는 굵은 고딕인데, 게임은 가변 폰트의 기본값(Thin)으로 글자를 구워 가늘게 나왔다.
글꼴 파일만 새로 떠낼 뿐 그림을 그리는 것이 아니다. OFL 라이선스라 게임에 넣어도 된다.
"""
import os
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'Assets', 'BaseResource', 'Fonts'))
SRC = os.path.join(FONTS, 'NotoSansKR.ttf')

for wght, name in [(700, 'Bold'), (800, 'ExtraBold'), (900, 'Black')]:
    f = TTFont(SRC)
    inst = instancer.instantiateVariableFont(f, {'wght': wght})
    out = os.path.join(FONTS, f'NotoSansKR-{name}.ttf')
    inst.save(out)
    print(out, os.path.getsize(out))
