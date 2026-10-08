"""팝업 · 방 오브젝트 · 스테이지 클리어 연출 시안(3컷 스토리보드) 발주 (2026-10-08).

PD: 「팝업 띄우는 연출 · 그에 따른 이펙트가 하나도 없다 — 이미지 하나로 끝냈다. 특수 오브젝트도, 스테이지 클리어 팝업도
      전체적으로 다 이펙트를 넣어야 한다. 시안을 한 번에 싹 잡아 보고하라」.
틀 · 색은 통과한 C 「원혼 회로」(in/mock_set_C_*.png · 게임 반영 ref/popup_c_ingame/). 시안마다 3컷 가로(1 등장 · 2 최대 · 3 선택/끝).
python order_fx_story_mocks.py → _exchange/order_fxstory_{key}.ps1 · run_fxstory.ps1
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
REF = 'Projects/AVSR/_exchange/ref/popup_c_ingame'

COMMON = '''[공통]
- 지금 게임 화면(틀 · 글자 · 배치)은 {base} 그대로 — 그 위에 **연출 · 이펙트를 더한** 모습을 그린다. 틀 디자인을 바꾸지 않는다.
- 글자는 칸 안에 딱 맞게(삐져나가거나 테두리에 걸치면 불합격). 한국어 글자는 기준 캡처 그대로.
- 이펙트는 게임과 같은 도트(픽셀 아트) — 빛 줄기 · 원혼(유령 기운) 흐름 · 도트 입자 · 연기 · 불씨 · 빛 번짐.
  대표 색: 레벨업 금 · 회복 청록 · 악마 진홍 · 상점 호박 · 클리어 금+청백.
- 별 · 표창 · 십자 반짝이 모양 금지. 화면 전체를 한 색으로 물들이지 않는다(빛은 창 둘레 · 대상 둘레에만).
- 3컷을 가로로 나란히(각 컷 720 x 1280, 전체 2160 x 1280), 컷 왼쪽 위에 작은 번호 · 제목 「1 등장」 「2 최대」 「3 …」. 컷마다 화살표 · 짧은 메모로 움직임을 적는다(한국어).'''

STORIES = {
    'levelup': ('레벨업 3택1 팝업', f'{REF}/levelup.png', [
        '1 등장 — 화면이 어두워지며 창 틀이 위에서 툭 내려와 멈춘다. 금빛 원혼이 위 유령 문장에서 터져 관로를 따라 아래로 흐르고 좌우 유리관이 차례로 켜진다',
        '2 최대 — 「LEVEL UP!」 판 뒤로 금빛 빛줄기가 퍼지고, 카드 3장이 왼쪽부터 0.08초 간격으로 아래에서 솟아 자리 잡는다. 등급 빛(레어 청 · 에픽 보라 · 일반 회색)이 카드 테를 한 바퀴 돈다. 고를 수 있는 카드 테가 은은히 숨쉰다',
        '3 선택 — 고른 카드가 살짝 커지며 금빛 도트 폭발, 카드 그림 속 원혼이 빛 줄기가 되어 위로 빨려 올라가고 나머지 두 장은 어두워지며 가라앉는다'], ),
    'altar': ('회복의 제단 팝업', f'{REF}/altar.png', [
        '1 등장 — 창 위 물방울 문장에서 청록 물방울이 똑 떨어지며 물결 고리가 퍼지고, 좌우 유리관에 청록 액체가 아래에서 위로 차오른다. 창이 물결처럼 일렁이며 또렷해진다',
        '2 최대 — 선택 칸 3줄 위로 청록 빛이 위에서 아래로 한 번 쓸고 지나가고, 칸 오른쪽 물방울 표식이 맥박처럼 빛난다. 작은 거품 입자가 관 속에서 올라간다',
        '3 선택 — 고른 칸이 청록으로 번쩍, 칸에서 치유 입자(작은 물방울 · 하트 없이)가 솟아 화면 위 HP 막대 쪽으로 흘러간다. 다른 칸은 흐려진다'], ),
    'devil': ('악마의 거래 팝업', f'{REF}/devil.png', [
        '1 등장 — 진홍 연기가 바닥에서 피어오르고 사슬이 양옆에서 철컥 감기며 창이 나타난다. 뿔 끝 · 붉은 관로에 불이 들어온다',
        '2 최대 — 보상 판 테두리는 금빛 불씨, 대가 판 테두리는 진홍 불씨가 타오른다. 수락 버튼이 심장 박동처럼 두 번 쿵쿵 빛난다. 아래 찢어진 천이 열기에 일렁인다',
        '3 수락 — 사슬이 팽팽히 조여지며 진홍 섬광, 플레이어 유령에게서 붉은 혼이 빨려 나와 창 가운데 문장으로 들어간다(대가). 보상 판에서 금빛이 터진다'], ),
    'shop': ('상점 팝업', f'{REF}/shop.png', [
        '1 등장 — 창 위 저울 문장 등불이 켜지고 좌우 유리관 속 노란 유령들이 깨어나 빙글 돈다. 호박빛이 관로를 따라 퍼지며 창이 켜진다',
        '2 최대 — 상품 칸 6개 위로 호박빛 반사광이 왼쪽 위부터 차례로 훑고 지나가고, 가격 판이 반짝 빛난다(별 모양 말고 가로 광택). 살 수 있는 칸 테가 은은히 숨쉰다',
        '3 구매 — 산 칸에서 금화 도트가 튀어 오르고 물건 아이콘이 빛 덩어리가 되어 플레이어 쪽으로 날아간다. 소지 금 숫자가 줄며 깜빡, 산 칸은 어두워진다'], ),
    # v1 은 옛 틀 · 옛 HUD 그대로 그려 반려 — 바탕은 지금 게임 화면, 틀은 C 레벨업 창과 같은 원혼 회로 문법으로(내용 앵커만 옛 시안)
    'clear': ('스테이지(챕터) 클리어 팝업 — **C 원혼 회로 틀로 새로 그린다**(내용만 Projects/AVSR/_exchange/in/ui_new_chapter_result_v1.png 를 따른다 · 틀은 '
              'Projects/AVSR/_exchange/ref/popup_c_ingame/levelup.png 와 같은 흑철 기계 틀 · 금빛 원혼 관로 · 유리관 문법, 글자는 한국어: 「CHAPTER 1 CLEAR」 · 「쓰레기 집적장」 · 「골드 획득 +1,240」 · 「은빛 상자」 · 「상자 칸이 가득 찼습니다」 · 「OK」)',
              'Projects/AVSR/_exchange/ref/skill_qx_check/robot.png (위 HUD · 아래 조작 버튼은 이 지금 게임 화면 그대로 — 옛 HUD 금지)', [
        '1 등장 — C 원혼 회로 틀(금빛 + 청백)로 다시 그린 결과 창. 화면 가운데서 금빛 원혼 폭발 · 빛줄기가 퍼지며 「CHAPTER 1 CLEAR」 판이 쾅 박힌다, 위 유령이 날아올라 자리 잡는다',
        '2 최대 — 골드 칸: 금화 도트가 쏟아지며 숫자가 0 에서 +1,240 으로 굴러 올라간다. 상자 칸: 은빛 상자가 위에서 떨어져 통 튀고 둘레에 빛 고리가 돈다',
        '3 끝 — OK 버튼이 금빛으로 숨쉬고, 창 둘레로 유령 기운 입자가 천천히 떠오른다(별 · 꽃가루 말고 둥근 도트 입자)'], ),
    'room_heal': ('방 오브젝트 — 회복의 제단(유령 캡슐)', f'{REF}/room_heal.png', [
        '1 대기 — 캡슐 속 청록 유령이 위아래로 둥실, 캡슐 위로 청록 기운 줄기가 피어오른다. 바닥 링이 바깥으로 한 번씩 퍼지는 물결, 머리 위 표식이 오르내린다',
        '2 다가감 — 플레이어가 링 안에 들어서면 캡슐 유리가 밝아지고 청록 빛 기둥이 위로 솟는다',
        '3 작동 — 캡슐에서 청록 치유 입자가 플레이어에게 쏟아져 들어가고 녹색 숫자가 뜬다. 끝나면 캡슐은 어둡게(다 씀), 링 · 표식이 꺼진다'], ),
    'room_devil': ('방 오브젝트 — 악마의 제단(사슬 캡슐)', f'{REF}/room_devil.png', [
        '1 대기 — 캡슐 속 붉은 해골 원혼이 일렁이고 위로 불씨 · 검붉은 연기가 오른다. 사슬이 가끔 흔들리며 붉게 달아오른다. 바닥 링이 숨쉬듯 밝아졌다 어두워진다',
        '2 다가감 — 해골 눈이 번쩍 플레이어를 보고, 뿔 끝에 진홍 불이 붙는다',
        '3 작동 — 진홍 섬광과 함께 사슬이 풀리듯 출렁, 붉은 연기 고리가 퍼지고 거래 창이 뜬다. 끝나면 캡슐은 식어서 어둡게'], ),
    'room_shop': ('방 오브젝트 — 유령 상인', f'{REF}/room_shop.png', [
        '1 대기 — 상인 등의 등불들이 따로따로 깜빡이고, 노란 혼불이 몸에서 일렁인다. 진열 캡슐에 호박빛 광택이 흐른다(별 말고 가로 광택), 금화 도트 몇 개가 천천히 떠오른다',
        '2 다가감 — 상인이 고개를 들고 눈이 밝아지며 등불이 한꺼번에 환해진다',
        '3 작동 — 호박빛 금화 고리가 퍼지며 상점 창이 뜬다. 다 쓰면 등불이 하나씩 꺼진다'], ),
}

VER = {'clear': 'v2'}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 글자를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 **연출 시안 — 3컷 스토리보드** : {title}
{common}
[컷]
{cuts}
[저장] Projects/AVSR/_exchange/in/mock_fxstory_{key}_{ver}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_fxstory_$k.ps1' *> 'Projects/AVSR/_exchange/log_fxstory_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = Get-ChildItem "Projects/AVSR/_exchange/in/mock_fxstory_$($k)_v*.png" -ErrorAction SilentlyContinue; if ($f) { "ok $k" } else { "MISSING $k" } }
'''


def main(only=None):
    keys = []
    for key, (title, base, cuts) in STORIES.items():
        if only and key not in only:
            continue
        body = TEMPLATE.format(title=title, common=COMMON.format(base=base), cuts='\n'.join('- ' + c for c in cuts), key=key,
                               ver=VER.get(key, 'v1'))
        with open(os.path.join(EX, 'order_fxstory_%s.ps1' % key), 'w', encoding='utf-8-sig') as f:
            f.write(body)
        keys.append(key)
    with open(os.path.join(EX, 'run_fxstory.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    import sys
    main(sys.argv[1:] or None)
