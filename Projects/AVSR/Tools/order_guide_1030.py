"""10-30 PD 「튜토리얼 그림 바뀐 시스템대로 · 추가해야 될 것 발주」 — 설명 창 그림 6장.

다시: possess · ghost_hp / 새로: party · main_down · synergy · ultimate
화풍 앵커: Assets/BaseResource/ReBornKit/Art/rb_art_guide_*.png (지금 15장)
→ _exchange/order_guide1030_{주제}.ps1 · 실행기 run_guide1030.ps1(한 줄로 차례대로 — 429 피함)
납품 _exchange/in/guide1030_{주제}.png → 720 x 480 으로 맞춰 rb_art_guide_{주제}.png
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_ui_reborn.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 설명(튜토리얼) 창에 들어가는 **설명 그림** 한 장.
[화풍 앵커 — 반드시 열어 본다] Assets/BaseResource/ReBornKit/Art/ 의 rb_art_guide_possess.png · rb_art_guide_stand_fire.png · rb_art_guide_cards.png · rb_art_guide_boss.png.
같은 세트로 보여야 한다: 어두운 남색 바탕 · 가운데 둥근 남색 얼룩 · 굵은 도트 · 하얀 유령(통통한 몸 · 짧은 꼬리 · 큰 입) · 청록 소용돌이 빛 · 금빛 강조.
[게임 화면 참고] Projects/AVSR/_exchange/ref/party_ui_1029/base_ingame_now.png · Projects/AVSR/_exchange/ref/party_hud_1029.png (지금 인게임 모습 — 호스트 · 적 생김새)
[공통] 가로 3 : 세로 2 비율(1536 x 1024 권장) 한 장. **글자 · 숫자 · 말풍선 글을 넣지 마라**(설명 글은 게임이 아래에 따로 쓴다). 화살표 · 손가락 · 아이콘으로 뜻이 읽히게.
한눈에 「무엇을 하라는지」 읽혀야 한다 — 주인공 동작 하나를 가운데 크게.
'''
TAIL = r'''
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

ORDERS = {
    'possess': '''[주제] 빙의 — 적을 직접 눌러서 빙의한다
가운데: 체력이 반쯤 깎인 적 호스트(녹색 양복 갱단). 머리 위에 「빙의 가능」 표시(빛나는 유령 얼굴 표식 · 금빛 테). 그 적 위로 **손가락(터치하는 손)** 이 톡 누르는 모습 + 누른 자리 동그란 파문.
왼쪽 위: 하얀 유령이 그 적 쪽으로 날아드는 청록 소용돌이 궤적.
적의 체력 막대가 반 아래로 빨갛게 줄어 있다(숫자 없이 막대만).
[저장] Projects/AVSR/_exchange/in/guide1030_possess.png''',
    'ghost_hp': '''[주제] 유령 체력 = 한 판 예산 — 빙의 · 교체할 때마다 줄어든다
가운데 위: 큰 하얀 유령. 그 아래 가로로 긴 **유령 체력 막대**(청록) — 칸이 여러 개로 나뉘어 있고 오른쪽 몇 칸이 깨지며 빠져나가는 모습.
유령에서 아래 호스트(녹색 양복 갱단)로 청록 빛줄기가 들어가고, 그 빛줄기에서 막대 조각이 떨어져 나간다(빙의하면 체력을 쓴다는 뜻).
오른쪽 아래 작게: 쓰러진 호스트 위에서 막대 조각이 크게 깨지는 모습(메인이 쓰러지면 크게 깎임).
[저장] Projects/AVSR/_exchange/in/guide1030_ghost_hp.png''',
    'party': '''[주제] 메인 · 보조 — 메인 호스트 뒤를 보조 둘이 따라다니며 같이 싸운다
가운데: 메인 호스트(녹색 양복 갱단, 크게) — 발밑 금빛 고리 + 머리 위 작은 왕관 표식. 앞쪽 적에게 총을 쏜다.
메인 양옆 조금 뒤: 보조 호스트 둘(조금 작게, 서로 다른 생김새 — 예: 군인 · 깡패) — 발밑 청록 고리. 같은 적을 향해 쏜다.
메인과 보조 사이를 잇는 은은한 청록 끈(같이 다닌다는 뜻). 오른쪽에 적 하나.
[저장] Projects/AVSR/_exchange/in/guide1030_party.png''',
    'main_down': '''[주제] 메인이 쓰러지면 — 셋 중 하나를 고른다
가운데 아래: 쓰러진 메인 호스트(녹색 양복 갱단) · 위로 빠져나오는 하얀 유령.
유령 위에 **갈림길 화살표 셋**이 부채꼴로 뻗고, 각 끝에 그림 하나씩:
 ① 왼쪽: 다른 적에게 빙의(유령 → 새 적, 청록 소용돌이)
 ② 가운데: 보조 호스트가 금빛 고리 위로 올라섬(보조가 메인이 됨, 위쪽 화살표)
 ③ 오른쪽: 쓰러진 호스트가 금빛 빛기둥 속에서 다시 일어남(제자리 부활)
유령 옆에 작은 모래시계(시간 제한).
[저장] Projects/AVSR/_exchange/in/guide1030_main_down.png''',
    'synergy': '''[주제] 무리 · 인연 — 같은 무리 호스트를 모으면 힘이 켜진다
가운데: 같은 옷 색(녹색 양복 갱단) 호스트 셋이 나란히 서 있고, 머리 위 작은 무리 문장(같은 모양 세 개)이 하나씩 금빛으로 켜진다.
셋을 잇는 금빛 빛줄 + 위로 솟는 금빛 기운(힘이 켜짐).
오른쪽 작게: 서로 다른 둘(예: 군인 · 마법사)이 **금빛 사슬 고리**로 이어져 하트 · 반짝임(인연 짝).
[저장] Projects/AVSR/_exchange/in/guide1030_synergy.png''',
    'ultimate': '''[주제] 두 버튼 — 액티브 스킬과 필살기
오른쪽 아래: 둥근 버튼 두 개(작은 버튼 = 액티브 스킬 · 큰 버튼 = 필살기). 큰 버튼 둘레를 **게이지 고리**가 금빛으로 거의 다 채운 모습. 큰 버튼을 손가락이 누른다.
왼쪽 · 가운데: 그 결과로 호스트(녹색 양복 갱단)가 큰 필살기를 터뜨린다 — 앞쪽으로 크고 화려한 금빛 · 주황 폭발 줄기가 적 무리를 휩쓴다.
버튼에서 호스트로 이어지는 청록 화살표.
[저장] Projects/AVSR/_exchange/in/guide1030_ultimate.png''',
}


def main():
    run = ['$env:OPENAI_API_KEY = $null', f'$d = "{EX}"']
    for k, body in ORDERS.items():
        open(os.path.join(EX, f'order_guide1030_{k}.ps1'), 'w', encoding='utf-8-sig').write(HEAD + body + TAIL)
        run.append(rf'powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_{k}.ps1" *> "$d\log_guide1030_{k}.txt"')
    run.append('"다 끝남"')
    open(os.path.join(EX, 'run_guide1030.ps1'), 'w', encoding='utf-8-sig').write('\n'.join(run) + '\n')
    print('발주서', len(ORDERS))


if __name__ == '__main__':
    main()
