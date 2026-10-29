$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 인게임 UI 시안. 크기 720 x 1280.
[바탕 = 정답] Projects/AVSR/_exchange/in/mock_partyui_A_free.png — PD 가 고른 전투 화면(왼쪽 세로 시너지 열 · 위쪽 HUD · 아래 조작부 · 레벨 줄). 화풍 · 판 모양 · 문장 모양을 그대로 따른다. 빙의 선택창은 Projects/AVSR/_exchange/in/mock_partyui_choice.png 의 판 모양을 따른다.
[PD 말] 「롤토체스처럼 디테일한 정보를 유저가 알 수 있게」 · 「우리는 시너지를 2~3개 정도만 넣을 거니 그것도 감안해서」 — 왼쪽 시너지 열은 **2~3칸만**(켜졌거나 하나만 더 모으면 켜지는 것만) 선다.
[시안 — 시너지 설명 창] 왼쪽 시너지 열의 「무기 2/3」 문장을 손가락으로 누른 순간. 전투는 잠깐 멈추고 필드는 살짝 어둡다. 누른 문장 옆에서 말풍선처럼 설명 창이 펼쳐진다(화면 가운데를 다 덮지 않는다, 폭은 화면의 60% 쯤).
- 창 위: 무기 문장(크게) · 「무기」 · 한 줄 설명 「총 · 칼 · 창을 든 호스트」.
- 단계 두 줄: 「2명 — 피해 +10%」(지금 켜짐 · 금빛) · 「3명 — 피해 +25%」(아직 · 어둡게 · 「1명 더」 꼬리표).
- 「무기 호스트」 얼굴 목록 8칸: 갱스터 · 폭력배 · 호퍼 · 호퍼(SMG) · 코만도(기관총) · 닌자 · 아마존 · 아마존 정예. **지금 파티에 있는 폭력배 · 호퍼는 밝게 + 「보조 1」 「보조 2」 표시**, 나머지는 어둡게(이름은 보이게). 얼굴은 게임 도트 그림.
- 창 아래 작은 글: 「같은 태그 2명 = 약 · 3명 = 강 · 메인은 100% · 보조는 50%」.
- 왼쪽 열에는 문장 세 칸만: 무기 2/3(눌린 상태로 밝게) · 갱단 2/3 · 무도가 1/2.
[저장] Projects/AVSR/_exchange/in/mock_partyui_syn_detail.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
