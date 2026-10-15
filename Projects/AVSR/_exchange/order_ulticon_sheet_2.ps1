$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하. 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 인게임 **필살기 버튼 아이콘** 6개.
[화풍 기준 — 이 아이콘들과 같은 크기 · 도트 · 구도] Projects/AVSR/_exchange/ref/skillmock/icons_current_sheet.png (지금 액티브 스킬 아이콘 23개 — 캐릭터가 스킬을 쓰는 순간을 한 칸에 담은 그림). 필살기 아이콘도 같은 방식이되 **더 크고 강한 순간**(필살기 시안의 가장 센 컷)을 담고, 둘레에 금빛 · 붉은 빛 기운을 조금 더 둘러 「필살기」로 구별되게.
[캐릭터는 원작 그대로] 각 캐릭터 원작 시트: Projects/AVSR/_exchange/ref/skillmock/orig_<호스트>_sheet.png
만들 것: 칸 6개 가로 한 줄 — 한 칸 64x64, 전체 384x64. **바탕은 분홍(#FF00FF) 단색**(나중에 뚫는다). 글자 · 숫자 금지.
칸 1(왼쪽부터): 닌자 필살기 「그림자 삼분신」 — 분신 셋을 불러 6초간 같이 공격한다 — 닌자가 던질 때 셋도 같이 던진다. (시안: Projects/AVSR/_exchange/in/mock_skill_ninja_v2.png 의 가장 센 컷)
칸 2(왼쪽부터): 아마존 필살기 「번개 무희」 — 적 사이를 잔상으로 옮겨 다니며 치고 내려찍기, 시전 중 무적. (시안: Projects/AVSR/_exchange/in/mock_skill_amazon.png 의 가장 센 컷)
칸 3(왼쪽부터): 호퍼(기관단총) 필살기 「돌진 난사」 — 가장 먼 적까지 일직선 돌진하며 양옆 난사, 도착하면 무적. (시안: Projects/AVSR/_exchange/in/mock_skill_hopper_smg.png 의 가장 센 컷)
칸 4(왼쪽부터): 흡혈귀 필살기 「피의 폭주」 — 5초간 체력을 내주고 폭주, 처치하면 피 구슬로 회복. (시안: Projects/AVSR/_exchange/in/mock_skill_vampire.png 의 가장 센 컷)
칸 5(왼쪽부터): 드라군 필살기 「용 돌격」 — 불을 뿜는 용을 타고 3초간 돌진. (시안: Projects/AVSR/_exchange/in/mock_skill_dragoon_v2.png 의 가장 센 컷)
칸 6(왼쪽부터): 슬러거 필살기 「풀스윙 회오리」 — 돌며 회오리를 키워 끌어당기고 풀스윙으로 날린다. (시안: Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png 의 가장 센 컷)
[저장] Projects/AVSR/_exchange/in/ulticon_sheet_2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
