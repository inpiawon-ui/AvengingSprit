"""인게임 팝업 4종 + 방 오브젝트 3종 새 디자인 시안 발주문 (2026-10-08).

PD: 「팝업(레벨업 · 회복 제단 · 악마의 거래 · 상점)과 방에 하나만 서는 오브젝트(회복 제단 · 악마 제단 · 상점)가
      디자인이 별로다 — 둘 다 새로, 오브젝트는 크게, 다 통일된 느낌으로 시안 몇 가지」.
상의: _exchange/consult_popup_restyle.md(글자 · 칸 수치) · consult_popup_obj_set.md(방향).
  A 는 코덱스가 공장풍으로 바꾸자 했으나 세 방향이 다 공장풍이면 고를 게 없어 「정통 다크 판타지 고퀄」로 둔다(Claude 판단).
방향마다 화면 5장(방 오브젝트 3종 한 화면 + 팝업 4장) — 완성된 게임 화면 한 장씩.
python order_popup_mocks.py → _exchange/order_popup_mock_{dir}_{screen}.ps1 · run_popup_mocks.ps1
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

BG = 'Projects/AVSR/_exchange/ref/skill_qx_check/robot.png'
NOW_POPUP = 'Projects/AVSR/_exchange/ref/popup_now_sheet.png'

COMMON = '''[한 벌 규칙 — 오브젝트 3종 + 팝업 4종이 한 디자인]
- 지금 디자인은 버린다(새로 그린다). 지금 캡처는 글자 · 내용만 따른다.
- 오브젝트와 그 오브젝트가 여는 팝업은 같은 재질 · 같은 실루엣 요소 · 같은 대표 색: 회복 = 청록, 악마 = 진홍, 상점 = 호박, 레벨업 = 금.
- 오브젝트 바닥에는 대표 색 얇은 이중 링(옅게), 머리 위에는 대표 상징 선 아이콘(글자 없이). 그 상징이 팝업 제목 판 위에도 똑같이 붙는다.
- 팝업 글자(720x1280 실제 크기): 제목 42~44 px · 이름 30 px(상점 상품명 28) · 설명 22 px(보조 20) · 가격 · 비용 28 px · 버튼 30/26 px(높이 72 px 이상).
  창 폭 660~680 px. 장식은 글자 · 선택 칸을 침범하지 않게 바깥으로. 위 HUD(y 0~230)와 아래 조작 버튼(y 1060~)은 가리지 않는다. 뒤 딤은 검정 65~70%.
- 게임과 같은 도트(픽셀 아트), 고퀄 디테일. 별 · 표창 · 십자 반짝이 금지, 화면 전체를 한 색으로 물들이지 않는다. 글자는 일본어(게임 기본 언어).'''

DIRS = {
    'A': ('정통 다크 판타지', '고퀄 다크 판타지 — 공장 배경 위에서도 「특별한 장소」로 튀는 성물 · 마물. 틀은 네 팝업 모두 어두운 돌 + 낡은 금 테두리로 같다.', {
        'heal': '흰 대리석 천사 석상(날개를 편) 아래 성배 분수, 위로 솟는 빛 기둥 — 흰 돌 · 금 · 청록 빛',
        'devil': '흑요석 뿔 아치 문(사람 두 배 높이) 안에서 지옥불이 일렁이는 악마의 문 — 흑요석 · 그을린 금 · 진홍',
        'shop': '등불 든 후드 유령 상인과 짐을 가득 실은 나무 마차(물약 · 두루마리 · 금화 · 상자) — 검은 나무 · 황동 · 호박빛',
        'levelup': '어두운 돌 카드 3장, 금 테두리 · 위쪽 등급 보석, 제목 판에 금 날개 문장',
        'altar': '흰 대리석 판 + 금 테, 제목 판 위 천사 날개 성배, 선택 칸 가장자리 청록 빛',
        'devilp': '흑요석 계약판 + 뿔 걸쇠, 대가 칸만 지옥불처럼 갈라진 진홍 빛',
        'shopp': '마차 나무판으로 짠 2x3 상품판, 등불 모양 가격표, 하단 거래대',
    }),
    'B': ('봉인 시설 단말기', '게임 무대(버려진 공장 · 기지)에 맞춘 안 — 낡은 설비 안에서 영혼이 동력처럼 흐른다. 흔한 SF 단말기가 아니라 녹 · 볼트 · 배관이 있는 묵직한 설비.', {
        'heal': '두꺼운 배관과 보호 프레임의 대형 치료 캡슐, 금 간 유리 안에 성수와 혼백이 떠 있다 — 회색 강철 · 탁한 유리 · 청록',
        'devil': '바닥에서 선 수직 격리 금고문, X자 사슬과 가운데 봉인 코어가 뿔처럼 벌어진 실루엣 — 흑강 · 경고 황색 줄 · 진홍',
        'shop': '옆문을 활짝 연 대형 컨테이너 매점, 창구 안 유령 상인, 지붕 간판(글자 대신 금화 아이콘) — 녹슨 청회색 철판 · 황동 · 호박빛',
        'levelup': '장비 슬롯 세 칸이 열린 개조 단말기, 카드마다 부품 고정 레일과 작은 상태등 — 무광 남흑색 · 회색 강철 · 금빛',
        'altar': '치료 캡슐 조작반 모양 판, 수액관 같은 세 선택 칸 — 회색 강철 · 탁한 유리 · 청록',
        'devilp': '봉인 금고문 안쪽 경고 단말기, 비용 · 보상 · 확정 버튼을 잠금 단계처럼 배열 — 흑강 · 경고 황색 · 진홍',
        'shopp': '컨테이너 벽을 접어 내린 부품 선반형 2x3 상품판, 가격은 창고 라벨처럼 — 녹슨 청회색 · 황동 · 호박빛',
    }),
    'C': ('원혼 회로', '빙의 게임의 얼굴 — 검은 공업 프레임에 붙잡힌 원혼이 전류처럼 흐른다. 형체는 큰 덩어리 · 설치물답게 묵직하게(배경 이펙트처럼 흐리지 않게).', {
        'heal': '넓은 고리 코일 사이에 붙잡힌 큰 물방울 모양 혼백, 아래 세 갈래 받침 — 흑철 · 창백한 뼈색 · 청록 혼광',
        'devil': '굵은 뿔 모양 전극 두 개 사이에 눌린 붉은 원혼 덩어리, 아래로 늘어진 봉인 사슬 — 흑철 · 짙은 자주 · 진홍 혼광',
        'shop': '긴 코트의 큰 유령 상인이 등에 멘 반원형 진열 프레임, 양옆에 매달린 상품 캡슐 — 흑철 · 먹청색 · 호박 혼광',
        'levelup': '세 빙의 슬롯을 원혼 관로가 감싸는 틀, 카드 혼이 위로 맥동 — 무광 흑철 · 뼈색 선 · 금빛 혼광',
        'altar': '고리 코일과 물결치는 혼백 관로로 감싼 판, 선택 칸 끝이 물방울 모양으로 점등 — 흑철 · 뼈색 · 청록',
        'devilp': '뿔 전극과 봉인 사슬이 둘러싼 계약판, 지불과 보상 사이를 붉은 혼선이 잇는다 — 흑철 · 짙은 자주 · 진홍',
        'shopp': '상인의 반원형 진열 프레임을 키운 2x3 상품판, 상품마다 작은 혼 캡슐 — 흑철 · 먹청색 · 호박빛',
    }),
}

SCREENS = {
    'room': '''[화면] 방 오브젝트 3종 — 팝업이 아니라 방 바닥에 서 있는 모습(실제 게임에선 방마다 하나씩, 여기선 비교용으로 셋을 나란히).
- 바탕 방(판 y 약 230~820)의 적 · 이펙트 · 숫자는 지우고, 아래쪽에 플레이어 캐릭터 하나만 둔다(크기 비교, 약 144 px).
- 왼쪽 회복 · 가운데 악마 · 오른쪽 상점. 각각 보이는 높이 약 260~300 px(플레이어의 약 2배) — 방에 들어오면 한눈에 알아보게.
- 회복: {heal}
- 악마: {devil}
- 상점: {shop}''',
    'levelup': '''[화면] 레벨업 팝업 — 카드 3장 중 하나(글자는 지금 캡처 맨 왼쪽 그대로).
- 디자인: {levelup}
- 제목 「LEVEL UP!」 + 「カードを選択してください」. 카드 3장 가로(한 장 204x480 px, 간격 12 px): 등급 칩(RARE · RARE · COMMON) · 그림 150 px 이상 ·
  이름(成長加速 / 守護の盾 / 吸魂) 바로 아래 설명(獲得経験値が増える。/ 盾が周囲を回って攻撃を防ぐ。/ 敵を倒すと体力が回復する。).''',
    'altar': '''[화면] 회복의 제단 팝업 — 방의 회복 오브젝트를 건드리면 뜬다(글자는 지금 캡처 두 번째 그대로).
- 디자인: {altar} (방 오브젝트 「{heal}」와 한 벌)
- 제목 「回復の祭壇」 + 안내 「ひとつだけ持っていける」. 선택 칸 3줄(폭 600 px · 높이 112 px 이상): 아이콘 72 px · 이름(手が速くなる / 遠くまで届く / 器を広げる) ·
  아래 설명(攻撃が速くなる / 射程が伸びる / 奪う体がより頑丈になる).''',
    'devil': '''[화면] 악마의 거래 팝업 — 방의 악마 오브젝트를 건드리면 뜬다(글자는 지금 캡처 세 번째 그대로).
- 디자인: {devilp} (방 오브젝트 「{devil}」와 한 벌)
- 제목 「深淵の取引」, 본문 2줄(底の見えない通路。下から声が昇ってくる。/ 代価は魂で受け取ると言う。),
  보상 「報酬・エピックカード1枚」(금) · 대가 「ゴースト体力25%」(진홍)를 한 줄에 나란히,
  수락 버튼 「魂を差し出す」, 거절 버튼 「代わりに休んでいく（ゴースト30%回復）」 — 글자가 버튼 밖으로 넘치지 않게.''',
    'shop': '''[화면] 상점 팝업 — 방의 상점 오브젝트를 건드리면 뜬다(글자 · 값은 지금 캡처 네 번째 그대로).
- 디자인: {shopp} (방 오브젝트 「{shop}」와 한 벌)
- 제목 「ショップ」, 「所持ゴールド 0」, 「残り購入2回・カード1枚」. 물건 6개를 2열 x 3줄(칸 288x132 px 이상): 아이콘 72 px · 첫 줄 이름 + 가격 · 둘째 줄 설명.
  火炎の刻印(攻撃に燃焼を付与する。42 G) · 窮地(体力が半分以下になると強くなる。42 G) · 守護の盾(盾が周囲を回って攻撃を防ぐ。42 G) ·
  体 — ギャングスター(その場でこの体を奪う 66 G) · 停止(次の部屋で敵が5秒止まる 14 G) · 治療(ホスト35%・ゴースト25%回復 42 G). 아래 「店を出る」 버튼.''',
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 글자를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(유령이 군인 · 로봇 · 괴물에 빙의해 싸우는 도트 액션 로그라이크) 인게임 **새 디자인 시안** — 완성된 게임 화면 한 장(720 x 1280 세로).
바탕: {bg} (지금 게임 화면 — 위 HUD · 방 · 아래 조작 버튼 그대로). 지금 팝업(글자 · 내용만 참고, 디자인은 버림): {now}
[방향 {dkey} 「{dname}」] {ddesc}
{common}
{screen}
[저장] Projects/AVSR/_exchange/in/mock_set_{dkey}_{skey}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_popup_mock_$k.ps1' *> 'Projects/AVSR/_exchange/log_popup_mock_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/mock_set_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main():
    keys = []
    for dkey, (dname, ddesc, parts) in DIRS.items():
        for skey, screen in SCREENS.items():
            body = TEMPLATE.format(bg=BG, now=NOW_POPUP, dkey=dkey, dname=dname, ddesc=ddesc,
                                   common=COMMON, screen=screen.format(**parts), skey=skey)
            with open(os.path.join(EX, 'order_popup_mock_%s_%s.ps1' % (dkey, skey)), 'w', encoding='utf-8-sig') as f:
                f.write(body)
            keys.append('%s_%s' % (dkey, skey))
    with open(os.path.join(EX, 'run_popup_mocks.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main()
