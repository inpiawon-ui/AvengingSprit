$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하. 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 인게임 **필살기 버튼 아이콘** 6개.
[화풍 기준 — 이 아이콘들과 같은 크기 · 도트 · 구도] Projects/AVSR/_exchange/ref/skillmock/icons_current_sheet.png (지금 액티브 스킬 아이콘 23개 — 캐릭터가 스킬을 쓰는 순간을 한 칸에 담은 그림). 필살기 아이콘도 같은 방식이되 **더 크고 강한 순간**(필살기 시안의 가장 센 컷)을 담고, 둘레에 금빛 · 붉은 빛 기운을 조금 더 둘러 「필살기」로 구별되게.
[캐릭터는 원작 그대로] 각 캐릭터 원작 시트: Projects/AVSR/_exchange/ref/skillmock/orig_<호스트>_sheet.png
만들 것: 칸 6개 가로 한 줄 — 한 칸 64x64, 전체 384x64. **바탕은 분홍(#FF00FF) 단색**(나중에 뚫는다). 글자 · 숫자 금지.
칸 1(왼쪽부터): 갱스터 필살기 「청부 처형」 — 가장 체력이 높은 적(보스 포함)을 1.5초 조준한 뒤 초대형 한 발. 보스는 최대 체력의 % 피해. (시안: Projects/AVSR/_exchange/in/mock_skill_gangster_v2.png 의 가장 센 컷)
칸 2(왼쪽부터): 호퍼 필살기 「킬 포인트」 — 칼날로 앞의 적들을 묶고, 묶인 적 전원에게 치명 확정. (시안: Projects/AVSR/_exchange/in/mock_skill_hopper.png 의 가장 센 컷)
칸 3(왼쪽부터): 닌자(사슬) 필살기 「사슬 돌격 연격」 — 가장 강한 적에게 사슬을 걸고 내가 날아가 연타 뒤 내려찍는다(보스를 끌어오지 않는다). (시안: Projects/AVSR/_exchange/in/mock_skill_ninja_chain_v3.png 의 가장 센 컷)
칸 4(왼쪽부터): 영매 필살기 「골렘 강령」 — 거대 골렘을 불러 8초간 같이 싸운다 — 내리찍기로 둘레를 부수고 영매 앞을 막는다. (시안: Projects/AVSR/_exchange/in/mock_skill_medium_golem.png 의 가장 센 컷)
칸 5(왼쪽부터): 사신 (히든) 필살기 「망자 군단」 — 해골 · 유령 군단을 불러 돌격시키고, 군단이 사라질 때 혼불로 터진다. (시안: Projects/AVSR/_exchange/in/mock_skill_death_v2.png 의 가장 센 컷)
칸 6(왼쪽부터): 로봇 필살기 「포탑 군단」 — 포탑 3대 + 대형 미사일 포탑 1대를 불러 교차 사격, 마지막에 미사일 일제 사격. (시안: Projects/AVSR/_exchange/in/mock_skill_robot_v3.png 의 가장 센 컷)
[저장] Projects/AVSR/_exchange/in/ulticon_sheet_1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
