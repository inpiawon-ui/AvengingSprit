$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 챕터 클리어 결과창 — 금화 더미 · 보상 상자 연출 **시안(너의 안)**.
[바탕] Projects/AVSR/_exchange/ref/result_mock/base_chest_land.png — 지금 게임 화면(2배). 창 · 칸 · 글자 · 금화 · 상자 그림은 그대로 두고 그 위에 연출을 덧그린다.
[참고] 예전에 그림에 박혀 있던 빛: Assets/BaseResource/ChapterScreens/reward_goldpile.png · Assets/BaseResource/LobbyMainUI/chest_silver.png .
PD 말: 「상자가 돋보이게 · 금화 연출 끝나면 금화가 빛나는 느낌 · 약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 상자 밑에 되도 않는 그림자 넣지 마 · 게임 정보(글자 · 숫자)는 가리지 마」.
[장면] mock_result_codex_peak — 네가 아트 디렉터라면 이 결과창의 금화 · 상자를 어떻게 꾸미겠나 — **힘주는 순간**(상자가 막 놓이고 금화가 다 쌓인 정점) 한 장. 예전 그림의 빛은 참고일 뿐, 더 멋진 안이 있으면 그걸로
- 도트 게임에 어울리게. 크기는 바탕과 같은 920 x 690.
- 그림 옆 빈 곳에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다(바탕 바깥 여백을 늘려도 된다).
[저장] Projects/AVSR/_exchange/in/mock_result_codex_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
