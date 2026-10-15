$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하. 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 인게임 **필살기 버튼 아이콘** 6개.
[화풍 기준 — 이 아이콘들과 같은 크기 · 도트 · 구도] Projects/AVSR/_exchange/ref/skillmock/icons_current_sheet.png (지금 액티브 스킬 아이콘 23개 — 캐릭터가 스킬을 쓰는 순간을 한 칸에 담은 그림). 필살기 아이콘도 같은 방식이되 **더 크고 강한 순간**(필살기 시안의 가장 센 컷)을 담고, 둘레에 금빛 · 붉은 빛 기운을 조금 더 둘러 「필살기」로 구별되게.
[캐릭터는 원작 그대로] 각 캐릭터 원작 시트: Projects/AVSR/_exchange/ref/skillmock/orig_<호스트>_sheet.png
만들 것: 칸 6개 가로 한 줄 — 한 칸 64x64, 전체 384x64. **바탕은 분홍(#FF00FF) 단색**(나중에 뚫는다). 글자 · 숫자 금지.
칸 1(왼쪽부터): 구루 필살기 「결계 가두기」 — 결계로 적을 가운데로 모아 가두고 터뜨린다. (시안: Projects/AVSR/_exchange/in/mock_skill_guru.png 의 가장 센 컷)
칸 2(왼쪽부터): 코만도(수류탄) 필살기 「C4」 — 반경 안 모든 적에게 폭탄을 붙이고 2초 뒤 연쇄 폭발(그동안 평타 쉼). (시안: Projects/AVSR/_exchange/in/mock_skill_commando_grenade_v2.png 의 가장 센 컷)
칸 3(왼쪽부터): 샐러맨더 필살기 「독 늪」 — 독 늪을 깔고 빠지며, 늪이 끝날 때 터진다. (시안: Projects/AVSR/_exchange/in/mock_skill_salamander.png 의 가장 센 컷)
칸 4(왼쪽부터): 설녀 필살기 「얼음 진」 — 얼음 진을 깔아 기둥이 솟고 가둔 뒤 파쇄. (시안: Projects/AVSR/_exchange/in/mock_skill_snowwoman.png 의 가장 센 컷)
칸 5(왼쪽부터): 폭력배 필살기 「회전 난사」 — 2.5초 돌며 사방 연사, 멈추며 사방 16발. (시안: Projects/AVSR/_exchange/in/mock_skill_thug.png 의 가장 센 컷)
칸 6(왼쪽부터): 코만도(미사일) 필살기 「자폭 미사일 떼」 — 6기가 각자 다른 적을 쫓아 동시 폭발. (시안: Projects/AVSR/_exchange/in/mock_skill_commando_missile.png 의 가장 센 컷)
[저장] Projects/AVSR/_exchange/in/ulticon_sheet_3.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
