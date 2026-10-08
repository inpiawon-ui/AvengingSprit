"""결과창 금화 · 상자 연출 시안 4컷 발주 (PD 2026-10-08 「퀄 엉망」 뒤 — 가이드 → 시안 → 제작).

가이드: Projects/AVSR/_exchange/guide_result_fx.md (박자 · 강약 · 목표 모습)
한 장씩 — 바탕 캡처 위에 이펙트만 덧그린 완성 화면. PD 확인용이라 게임에 넣지 않는다.
python order_result_mock.py [이름 ...]
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

MOCKS = {
    'mock_result_gold_peak': ('base_gold_done', '1.30초 금화 완성 순간(강) — 더미 뒤 금빛이 번쩍 확 퍼진 정점, 둘레에서 반짝 별 4~6개가 팡 터진 순간'),
    'mock_result_gold_idle': ('base_idle', '금화 머묾(약) — 예전 금화 그림처럼 더미 뒤 따뜻한 금빛 번짐 + 둘레 반짝 별 1~2개. 상자 칸은 손대지 않는다(상자 밑 파란 판은 지운다)'),
    'mock_result_chest_peak': ('base_chest_land', '2.05초 은 상자 착지 순간(강) — 상자 뒤에서 파란 빛살이 확 펼쳐지고 빛 번짐이 밝게 터진 정점, 모서리 반짝 별 3~4개'),
    'mock_result_chest_idle': ('base_idle', '완성 화면 머묾(약) — 금화(따뜻한 금빛 + 반짝 별 1~2) + 은 상자(예전 은 상자 그림처럼 상자 둘레 파란 빛 번짐 + 뾰족한 빛살 + 모서리 반짝 별). 상자 밑 파란 판은 지운다'),
}

# 코덱스 자유 시안 — 가이드에 얽매이지 않고 코덱스 감각으로(PD 10-08 「아트적 감성은 코덱스가 좀 더 좋은 것 같은데, 둘 다 시안을 잡아봐」)
FREE = {
    'mock_result_codex_peak': ('base_chest_land', '네가 아트 디렉터라면 이 결과창의 금화 · 상자를 어떻게 꾸미겠나 — **힘주는 순간**(상자가 막 놓이고 금화가 다 쌓인 정점) 한 장. '
                               '예전 그림의 빛은 참고일 뿐, 더 멋진 안이 있으면 그걸로'),
    'mock_result_codex_idle': ('base_idle', '같은 안의 **머무는 장면**(보상을 받고 OK 를 누르기 전까지 계속 보이는 모습) 한 장 — 은은하지만 보상이 돋보이게'),
}

FREE_TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 챕터 클리어 결과창 — 금화 더미 · 보상 상자 연출 **시안(너의 안)**.
[바탕] Projects/AVSR/_exchange/ref/result_mock/{base}.png — 지금 게임 화면(2배). 창 · 칸 · 글자 · 금화 · 상자 그림은 그대로 두고 그 위에 연출을 덧그린다.
[참고] 예전에 그림에 박혀 있던 빛: Assets/BaseResource/ChapterScreens/reward_goldpile.png · Assets/BaseResource/LobbyMainUI/chest_silver.png .
PD 말: 「상자가 돋보이게 · 금화 연출 끝나면 금화가 빛나는 느낌 · 약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 상자 밑에 되도 않는 그림자 넣지 마 · 게임 정보(글자 · 숫자)는 가리지 마」.
[장면] {name} — {desc}
- 도트 게임에 어울리게. 크기는 바탕과 같은 920 x 690.
- 그림 옆 빈 곳에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다(바탕 바깥 여백을 늘려도 된다).
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 결과창 연출 **시안**. 먼저 Projects/AVSR/_exchange/guide_result_fx.md 를 읽는다(목표 모습 · 박자 · 강약).
[바탕] Projects/AVSR/_exchange/ref/result_mock/{base}.png — 지금 게임 화면(2배). 창 · 칸 · 글자 · 금화 · 상자 그림은 **그대로** 두고 그 위에 이펙트만 덧그린다.
[목표 느낌] 예전 그림에 박혀 있던 빛: Assets/BaseResource/ChapterScreens/reward_goldpile.png (금화 둘레 금빛 · 반짝 별) · Assets/BaseResource/LobbyMainUI/chest_silver.png (상자 둘레 파란 빛 번짐 · 뾰족한 빛살 · 반짝임).
이 빛을 지우는 게 아니라 **살아 있는 이펙트로 더 멋지게** 보여 주는 장면이다. 도트 게임에 어울리게.
[장면] {name} — {desc}
- 빛은 물건 뒤, 반짝 별만 물건 모서리에 걸쳐도 된다. 오른쪽 글자(골드 획득 · +1,240 · 은 상자)는 덮지 않는다.
- 크기는 바탕과 같은 920 x 690.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

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


def main(only=None):
    keys = []
    for table, tpl in ((MOCKS, TEMPLATE), (FREE, FREE_TEMPLATE)):
        for name, (base, desc) in table.items():
            if only and name not in only:
                continue
            with open(os.path.join(EX, 'order_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
                f.write(tpl.format(name=name, base=base, desc=desc))
            keys.append(name)
    run = 'run_result_mock.ps1' if not only else 'run_result_mock_%s.ps1' % '_'.join(k.split('_')[-1] for k in keys)
    with open(os.path.join(EX, run), 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders')


if __name__ == '__main__':
    main(sys.argv[1:] or None)
