$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 인게임 UI 시안. 크기 720 x 1280.
[바탕 = 정답] Projects/AVSR/_exchange/in/mock_partyui_A_free.png — PD 가 고른 전투 화면(왼쪽 세로 시너지 열 · 위쪽 HUD · 아래 조작부 · 레벨 줄). 화풍 · 판 모양 · 문장 모양을 그대로 따른다. 빙의 선택창은 Projects/AVSR/_exchange/in/mock_partyui_choice.png 의 판 모양을 따른다.
[PD 말] 「롤토체스처럼 디테일한 정보를 유저가 알 수 있게」 · 「우리는 시너지를 2~3개 정도만 넣을 거니 그것도 감안해서」 — 왼쪽 시너지 열은 **2~3칸만**(켜졌거나 하나만 더 모으면 켜지는 것만) 선다.
[시안 — 시너지 힌트] 전투 중. 지금 파티: 메인 「슬러거」(파워 · 무도가) · 보조 1 「폭력배」(무기 · 갱단) · 보조 2 「호퍼」(무기 · 갱단). 왼쪽 시너지 열은 세 칸만: 무기 2/3 · 갱단 2/3 · 무도가 1/2.
- 필드에 적 호스트 둘이 「빙의 가능」(체력 60% 아래 · 유령 아이콘 + 터치 표시). 그중 「갱스터」(무기 · 갱단)는 데려오면 **무기 3/3 · 갱단 3/3** 을 켜 준다 — 머리 위에 무기 · 갱단 문장 두 개가 금빛으로 떠서 반짝이고 「3/3」 작은 표시, 왼쪽 열의 무기 · 갱단 칸도 같은 빛으로 깜빡여 「이 호스트를 데려오면 여기가 켜진다」가 이어져 보인다.
- 다른 하나 「드라군」(마법 · 괴물)은 시너지를 안 켜 준다 — 「빙의 가능」 표시만, 문장 없음.
- 위쪽 HUD 의 파티 초상화 셋 아래에 각자 태그 문장 두 개(작게)가 붙어 있다 — 메인 슬러거: 파워 · 무도가, 폭력배 · 호퍼: 무기 · 갱단.
[저장] Projects/AVSR/_exchange/in/mock_partyui_syn_hint.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
