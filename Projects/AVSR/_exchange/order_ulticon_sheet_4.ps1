$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하. 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 인게임 **필살기 버튼 아이콘** 5개.
[화풍 기준 — 이 아이콘들과 같은 크기 · 도트 · 구도] Projects/AVSR/_exchange/ref/skillmock/icons_current_sheet.png (지금 액티브 스킬 아이콘 23개 — 캐릭터가 스킬을 쓰는 순간을 한 칸에 담은 그림). 필살기 아이콘도 같은 방식이되 **더 크고 강한 순간**(필살기 시안의 가장 센 컷)을 담고, 둘레에 금빛 · 붉은 빛 기운을 조금 더 둘러 「필살기」로 구별되게.
[캐릭터는 원작 그대로] 각 캐릭터 원작 시트: Projects/AVSR/_exchange/ref/skillmock/orig_<호스트>_sheet.png
만들 것: 칸 5개 가로 한 줄 — 한 칸 64x64, 전체 320x64. **바탕은 분홍(#FF00FF) 단색**(나중에 뚫는다). 글자 · 숫자 금지.
칸 1(왼쪽부터): 코만도(레이저) 필살기 「충전 레이저」 — 0.5초 차지 뒤 굵은 관통 빔, 반동. (시안: Projects/AVSR/_exchange/in/mock_skill_commando_laser.png 의 가장 센 컷)
칸 2(왼쪽부터): 화이트 위저드 필살기 「빛 구슬 일제 발사」 — 평타로 쌓은 빛 구슬을 한꺼번에 유도 발사. (시안: Projects/AVSR/_exchange/in/mock_skill_white_wizard.png 의 가장 센 컷)
칸 3(왼쪽부터): 청룡 필살기 「번개 구슬」 — 구슬 3개가 몸 둘레를 돌며 번개, 끝에 낙뢰. (시안: Projects/AVSR/_exchange/in/mock_skill_dragon_blue.png 의 가장 센 컷)
칸 4(왼쪽부터): 코만도(기관총) 필살기 「디플렉트 월」 — 방벽을 세워 4초간 밀고 나가며 탄을 막는다. (시안: Projects/AVSR/_exchange/in/mock_skill_commando_mg.png 의 가장 센 컷)
칸 5(왼쪽부터): 아마존 정예 필살기 「역린」 — 3초간 받은 피해를 모았다가 주변으로 되돌려 터뜨린다. (시안: Projects/AVSR/_exchange/in/mock_skill_amazon_elite_v2.png 의 가장 센 컷)
[저장] Projects/AVSR/_exchange/in/ulticon_sheet_4.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
