"""얼티밋 아이콘 12종 재제작용 앵커 팩.

이 아이콘들만 유독 코드 도형(다각형+원)으로 납품됐다. 원인은 명확하다 —
**다른 에셋과 달리 앵커(잘라낸 참조 이미지)가 없었다.** 제안서 로스터 페이지
(p07)를 열어보면 빈 배경 카드뿐이라 딸 그림이 없다.

앵커가 없으면 텍스트 프롬프트만 남고, 그러면 도형이 나온다 —
이미 배경·프레임 작업에서 확인된 패턴이다. 그래서 대체 앵커를 만든다.

  01_style/  이미 잘 나온 같은 아틀라스의 아이콘 — 화풍·해상도·외곽선 기준
  02_host/   해당 호스트 초상 — 누구의 기술인지(색·실루엣 정체성)
  03_slot/   목업에서 이 아이콘이 놓이는 자리 — 크기감·주변 대비
"""
import os, shutil
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
BASE = os.path.join(ROOT, 'Assets', 'BaseResource', 'HostSelectPanel')
MOCK = os.path.join(HERE, 'Reference', 'Mockups', 'host_select.jpeg')
OUT = os.path.join(HERE, '_exchange', 'out', '04_ultimate')

# 화풍 기준 — 코드 도형이 아니라 실제로 잘 그려진 같은 화면의 아이콘들
STYLE = ['staticon_hp', 'staticon_atk', 'staticon_spd', 'staticon_dash',
         'hostslotlockicon', 'ownedhostchesticon', 'possessghosticon', 'tipicon']

# 얼티밋 → 호스트 (초상을 정체성 앵커로 붙인다)
OWNER = {
    'blade_storm': 'amazoness', 'bullet_hell': 'rambo', 'elemental_nova': 'wizard',
    'shadow_burst': 'ninja', 'tommy_barrage': 'mafia', 'perfect_kill': 'hitman',
    'astral_form': 'yogamaster', 'dragon_breath': 'dragon', 'system_override': 'robot',
    'absolute_zero': 'snowwoman', 'grand_slam': 'slugger', 'blood_tornado': 'vampire',
}


def main():
    for d in ('01_style', '02_host', '03_slot'):
        p = os.path.join(OUT, d)
        shutil.rmtree(p, ignore_errors=True)
        os.makedirs(p, exist_ok=True)

    for n in STYLE:
        src = os.path.join(BASE, n + '.png')
        if not os.path.exists(src):
            print(f'  없음 {n}')
            continue
        im = Image.open(src).convert('RGBA')
        im.resize((im.width * 4, im.height * 4), Image.NEAREST).save(
            os.path.join(OUT, '01_style', f'{n}_x4.png'))

    for ult, host in OWNER.items():
        src = os.path.join(BASE, f'hostportraitimage_{host}.png')
        if not os.path.exists(src):
            print(f'  없음 {host}')
            continue
        im = Image.open(src).convert('RGBA')
        im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(
            os.path.join(OUT, '02_host', f'{ult}__{host}_x2.png'))

    # 목업에서 얼티밋 카드가 놓인 자리 (아이콘 크기감 참조)
    src = Image.open(MOCK).convert('RGB')
    card = src.crop((398, 632, 660, 732))
    card.resize((card.width * 3, card.height * 3), Image.LANCZOS).save(
        os.path.join(OUT, '03_slot', 'ultimate_card_in_mockup_x3.png'))

    print(f'앵커 팩 → {OUT}')
    print(f'  01_style {len(STYLE)}종 · 02_host {len(OWNER)}종 · 03_slot 1종')


if __name__ == '__main__':
    main()
