"""RE:BORN UI 리뉴얼(2026-10-23) — 그대로 쓰는 리소스를 우리 폴더 규칙대로 옮긴다(2단계).

PD 「720 기준으로 새로 구성 · 우리 규칙 기준으로」 — 묶음의 `Assets/REBORN_UI/` 통째 복사 · 임포터는 쓰지 않는다.
옮기는 것: 아이콘 90종(48 · 96 · 144) · 워드아트 16 · 숫자 비트맵 12 · 그림 생성 일러스트(챕터 배너 1~10 · 결과 · 소환 · 상점 · 패스 · 받침 ·
재사용 그림). 안 옮기는 것: 판 · 버튼 · HUD · 연출(코드 그림 — 다시 발주) · 히든 실루엣(다시 발주) · 예전 전투 바닥 `bg_stage_ch*`.

크기: 묶음 1x 는 1080 폭 기준이다. 720 화면에는 **2/3 크기로 놓는다**(1080 폭 기기에서 그림 1픽셀 = 화면 1픽셀).
이름: 묶음 `ui_{id}@WxH.png` → `rb_{종류}_{id}[_{크기}].png` (아틀라스 안 이름이 겹치지 않게)

python ui_reborn_import.py → Assets/BaseResource/ReBornKit/{Icons,WordArt,Numbers,Art}/
그다음 유니티에서 임포트 설정 · 아틀라스(09_import_settings.md) — ReBornKitImport.cs 메뉴
"""
import os
import re
import shutil

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Projects', 'AVSR', '_handoff', 'reborn_ui_1023', '03_리소스_및_시안', 'REBORN_UI_FINAL', 'REBORN_UI')
DST = os.path.join(ROOT, 'Assets', 'BaseResource', 'ReBornKit')

SKIP_ART = re.compile(r'(hidden_silhouette|bg_stage_ch\d+)')
NAME = re.compile(r'^ui_(.+?)@(\d+)(?:x(\d+))?\.(png|fnt|json)$')


def copy(src, folder, name):
    os.makedirs(os.path.join(DST, folder), exist_ok=True)
    shutil.copy2(src, os.path.join(DST, folder, name))


def main():
    n = 0
    # 아이콘 — Unity 용 이름 폴더만(48 · 96 · 144 는 같은 그림의 정수배 — 화면 크기마다 맞는 것을 쓴다)
    for f in sorted(os.listdir(os.path.join(SRC, 'Icons', 'Unity'))):
        m = NAME.match(f)
        if not m:
            continue
        copy(os.path.join(SRC, 'Icons', 'Unity', f), 'Icons', 'rb_icon_%s_%s.png' % (m.group(1), m.group(2)))
        n += 1
    for f in sorted(os.listdir(os.path.join(SRC, 'WordArt', '1x'))):
        m = NAME.match(f)
        if m:
            copy(os.path.join(SRC, 'WordArt', '1x', f), 'WordArt', 'rb_word_%s.png' % m.group(1)); n += 1
    for f in sorted(os.listdir(os.path.join(SRC, 'Numbers', '1x'))):
        m = NAME.match(f)
        if m:
            # .fnt 안의 page 파일 이름도 새 이름으로
            new = 'rb_%s.%s' % (m.group(1), m.group(4))
            if m.group(4) == 'fnt':
                text = open(os.path.join(SRC, 'Numbers', '1x', f), encoding='utf-8').read()
                text = text.replace(f.replace('.fnt', '.png'), 'rb_%s.png' % m.group(1))
                os.makedirs(os.path.join(DST, 'Numbers'), exist_ok=True)
                open(os.path.join(DST, 'Numbers', new), 'w', encoding='utf-8', newline='\n').write(text)
            else:
                copy(os.path.join(SRC, 'Numbers', '1x', f), 'Numbers', new)
            n += 1
    # 일러스트 — 1x 는 1080 기준 원본 그대로
    art = [os.path.join(SRC, 'Art', '1x', f) for f in os.listdir(os.path.join(SRC, 'Art', '1x'))]
    for dp, _, fs in os.walk(os.path.join(SRC, 'Art', 'Reused')):
        art += [os.path.join(dp, f) for f in fs if f.endswith('.png')]
    for p in sorted(art):
        f = os.path.basename(p)
        if SKIP_ART.search(f):
            continue
        m = NAME.match(f)
        key = m.group(1) if m else os.path.splitext(f)[0]
        key = key[4:] if key.startswith('art_') else key
        copy(p, 'Art', 'rb_art_%s.png' % key)
        n += 1
    print(n, 'files →', DST)


if __name__ == '__main__':
    main()
