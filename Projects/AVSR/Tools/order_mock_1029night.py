"""10-29 밤 시안 네 장 — 기관총 필살기 A · B · 성장 화면 인연 UI · 호스트 해금 연출(PD 2026-10-29).

PD 원문
- 「저 두 개 시안은 모르겠어 다시 시안 보여줘」(기관총 필살기 A안 거치 기관포 · B안 포탑)
- 「아웃게임의 인연은 유아이 시안을 코덱스한테 다시 받아서 시안 보고 적용하자 아니면 저쪽을 전체적으로 한번 바꿔봐도 좋고」
- 「호스트 조각을 얻었을 때 해금할 수 있는 조각이 되는 순간 캐릭터 해금되었습니다 연출이 필요하고」
→ _exchange/order_mock1029_*.ps1 · 납품 _exchange/in/mock1029_*.png
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화면 글자는 전부 **한국어**. 게임 정보(글자 · 숫자 · 아이콘)를 연출이 가리지 않는다. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
'''
TAIL = '''
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

ORDERS = {
    'ult_mg_a': '''[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_bubble.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[주인공] 코만도(기관총) — 원작 그림 Assets/BaseResource/Unit/commando_mg/unit_commando_mg_e.png · _atk1 · _atk2 그대로(새로 디자인 금지). 화면 가운데 아래쪽에 크게.
[장면] 필살기 A안 「거치 기관포」 — 힘주는 순간. 기관총을 바닥에 거치하고 **앞쪽 60° 부채꼴만** 좌우로 훑으며 굵은 관통 탄줄을 3초 쏟는다.
  탄줄이 적 여럿을 꿰뚫고, 맞은 적은 파랗게 둔해진다. 총구 불꽃 · 튀는 탄피 · 바닥 흙먼지. 화면 위쪽(앞) 방향으로만 쏜다.
  ⚠ 폭력배 필살기 「회전 난사」(제자리에서 사방으로 도는 탄막)와 **한눈에 달라 보여야 한다** — 사방 · 원형 탄막 금지. 「앞을 쓸어 버리는 군인의 제압 사격」.
[저장] Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png''',
    'ult_mg_b': '''[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_bubble.png — 지금 인게임 화면(위 HUD · 아래 조작 단추 그대로 둔다).
[주인공] 코만도(기관총) — 원작 그림 Assets/BaseResource/Unit/commando_mg/unit_commando_mg_e.png 그대로(새로 디자인 금지).
[장면] 필살기 B안 「기관총 포탑」 — 코만도가 옆에 **삼각대 기관총 포탑**을 내려놓고(도트 군용 장비, 원작 군인 느낌) 자기는 다른 곳으로 움직인다.
  포탑이 8초 동안 가까운 적을 자동으로 돌려 겨눠 쏜다. 포탑 머리 위 남은 시간 고리, 총구 불꽃, 탄피. 코만도와 포탑이 따로 싸우는 그림이 한눈에 읽혀야 한다.
  ⚠ 폭력배 「회전 난사」(제자리 사방 탄막)와 달라 보여야 한다.
[저장] Projects/AVSR/_exchange/in/mock1029_ult_mg_b.png''',
    'growth_inyeon': '''[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/play/growth_16x9.png — 지금 「호스트 성장」 화면(위 재화 · 영웅 칸 · 스킬 셋 · 스킬 레벨 칸 · 아래 탭).
[주제] 오른쪽 아래 「인연 · 청부 표식」 칸이 무엇인지 잘 안 읽힌다. **인연을 한눈에 알 수 있게** 화면을 다시 꾸민다 — 인연 부분만 고쳐도 되고, 더 낫다면 스킬 · 인연 아래쪽 전체 배치를 바꿔도 된다.
  알려야 할 것: 인연 이름 · 지금 단계(1단) · 짝 호스트 얼굴과 성급(짝을 보조로 데려가면 켜진다) · 켜지면 바뀌는 것(이 호스트의 액티브 스킬이 「표식 붙은 적을 보조가 우선 노리고 보조 피해 +50%」로 강해짐) · 단계가 오르는 조건(두 호스트 중 낮은 성급).
  지금 화면의 화풍(남색 판 · 청록 테 · 금빛 강조 · 도트 아이콘)을 그대로 따른다. 다른 칸(액티브 · 패시브 · 필살기 레벨 칸)은 그대로 두거나 함께 정돈한다.
[저장] Projects/AVSR/_exchange/in/mock1029_growth_inyeon.png''',
    'host_unlock': '''[바탕] Projects/AVSR/_exchange/ref/party_check_1029/r2_syndetail.png — 판이 끝난 결과 화면(획득 보상에 호스트 조각 칸들).
[장면] **호스트 해금 순간** — 조각이 모여 해금 수에 닿는 순간 「드라군 해금!」 연출. 힘주는 순간 한 장.
  결과 화면이 살짝 어두워지고 가운데에 해금된 호스트(원작 그림 Assets/BaseResource/Growth/Portraits/portrait_dragoon.png 를 그대로 크게)가 영혼 빛과 함께 나타난다.
  조각들이 모여 몸이 되는 흐름, 「새 호스트 해금!」 큰 제목, 등급 딱지(SSR), 호스트 이름, 아래 「확인」 · 「성장으로」 단추.
  보상의 무게: 자주 보지 않는 큰 순간이다 — 확실히 세게(강). 그러나 글자 · 얼굴을 빛이 덮지 않게.
[저장] Projects/AVSR/_exchange/in/mock1029_host_unlock.png''',
}


def main():
    keys = []
    for k, body in ORDERS.items():
        p = os.path.join(EX, f'order_mock1029_{k}.ps1')
        open(p, 'w', encoding='utf-8-sig').write(HEAD + body + TAIL)
        keys.append(k)
    print('발주서', len(keys))


if __name__ == '__main__':
    main()
