"""등급 연출 시안 발주 — 결과창 상자 · 레벨업 카드 · 깃듦을 등급별로 나란히 (2026-10-12).

PD 「그럼 한번 적용해서 보여줘」 — 레퍼런스 그림판(ref/refboard/shots_*) 방향을 등급 사다리 시안으로 먼저 받는다.
python order_tiers_mock.py → _exchange/order_mock_tiers_*.ps1 + run_mock_tiers.ps1 · 납품 _exchange/in/mock_tiers_*.png
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
CAP = 'Projects/AVSR/_exchange/ref/refboard/cap/'
BASE = 'Projects/AVSR/_exchange/ref/tiers_mock/'

HEAD = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 등급별 연출 **시안**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (도트 모양 + 광원 · 파티클 섞기, 강약, 등급 레벨링),
Projects/AVSR/_exchange/guide_tiers_fx.md (이번 등급표 — 이대로 그린다).
레퍼런스 캡처(다른 게임 실제 장면 — 화풍이 아니라 「등급이 오를 때 무엇을 더하나」를 본다):
{refs}
[바탕] {base} — 지금 게임 화면. 창 · 칸 · 글자 · 카드 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다. 글자는 가리지 않는다.
PD 가 이미 뺀 것(다시 넣지 말 것): 물건 · 카드 뒤에 계속 붙어 있는 둥근 등급색 글로우 · 몸 둘레를 빙글빙글 도는 문양 · 물건 밑 단색 판(그림자).
PD 말: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지」 · 「힘을 더 준다 = 크기가 아니라 파티클 수를 조금 늘리고 종류를 다양하게」.
[장면] {scene}
- 크기: {size}. 등급마다 칸을 나란히, 위에 등급 이름을 그대로 둔다. 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 · 언제 · 어떻게 움직이는지)를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RES_REFS = ['01_6nd2h4', '02_ciermd', '03_zhlqcr', '04_envg65', '06_l20qv8', '07_cz9sg1', '08_rrsg5e',
            '13_7w395d', '14_5q06gj', '16_383ffz', '17_n3uqh0']
LV_REFS = ['20_gdoa66', '21_0v8a7p', '22_sjm2kb', '23_oqiz69', '16_383ffz', '17_n3uqh0']
GAIN_REFS = ['25_w3m5xn', '13_7w395d', '17_n3uqh0']

JOBS = [
    ('mock_tiers_result_peak', 'base_result_tiers.png', RES_REFS, '1536 x 640',
     '결과창 보상 칸 — 은 · 금 · 백금 상자가 칸에 **착지하는 순간**(강). 가이드 표의 「착지 순간」 행 그대로: '
     '은 = 먼지 + 도트 반짝 별 몇 개 / 금 = + 등급색 고리가 퍼지는 중 / 백금 = + 고리 두 겹 + 위에서 내려오는 가는 빛줄기 + 도트 불꽃 알갱이. '
     '상자 위에 등급 별(은 1 · 금 2 · 백금 3, 작은 도트 별). 금화 더미에서는 도트 금화 몇 개가 위로 튀어 오른다.'),
    ('mock_tiers_result_idle', 'base_result_tiers.png', RES_REFS, '1536 x 640',
     '같은 안의 **머무는 장면**(약). 착지 효과는 다 사라졌고 은은하게: 은 = 반짝 별 가끔 하나 / 금 = 조금 더 자주 / 백금 = + 작은 알갱이가 천천히 떠오름. '
     '상자 위 등급 별(1 · 2 · 3)은 남아 있다. 뒤에 붙은 광원 없이 깨끗해야 한다.'),
    ('mock_tiers_lv_appear', 'base_lv_tiers.png', LV_REFS, '1174 x 640',
     '레벨업 카드 4등급이 **나타나는 순간**(중~강). COMMON = 테두리 색만 / RARE = 테두리를 광택이 한 번 훑는 중 / '
     'EPIC = 위에서 보라 빛기둥이 내려와 카드에 꽂힘 / LEGENDARY = 금빛 기둥 + 도트 반짝 별 터짐. '
     '기둥은 카드 폭 안쪽의 곧은 세로 기둥(카드 사이 틈은 비워 둔다, 번지는 둥근 광원 금지).'),
    ('mock_tiers_lv_idle', 'base_lv_tiers.png', LV_REFS, '1174 x 640',
     '같은 카드 4등급의 **머무는 장면**(약). COMMON · RARE = 아무것도 없음(테두리 색만) / EPIC = 카드 뒤로 곧은 보라 기둥이 은은하게 남음 / '
     'LEGENDARY = 금빛 기둥 + 반짝 별이 가끔. 카드 글자 · 그림은 그대로 읽혀야 한다.'),
    ('mock_tiers_gain', 'base_gain.png', GAIN_REFS, '1400 x 700',
     '고른 카드가 유령에게 **깃드는 순간**(강, 0.5 초 안) — 두 칸 나란히: 왼쪽 COMMON, 오른쪽 LEGENDARY. '
     '세로 섬광 한 번 + 도트 알갱이가 위로 + 유령 머리 위에 카드 아이콘 · 이름 글자. 등급이 높으면 알갱이 수 · 섬광 겹만 조금 더(크기는 같게). '
     '몸 둘레를 도는 문양 · 큰 고리 여러 겹 금지. 유령 그림은 그대로.'),
]

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main():
    keys = []
    for name, base, refs, size, scene in JOBS:
        ref_lines = '\n'.join('- %s%s.jpg' % (CAP, r) for r in refs)
        with open(os.path.join(EX, 'order_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(HEAD.format(refs=ref_lines, base=BASE + base, scene=scene, size=size, name=name))
        keys.append(name)
    run = os.path.join(EX, 'run_mock_tiers.ps1')
    with open(run, 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders →', run)


if __name__ == '__main__':
    main()
