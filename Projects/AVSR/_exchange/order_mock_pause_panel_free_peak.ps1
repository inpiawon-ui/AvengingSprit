$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — pause_panel 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/play/pause_16x9.png — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
[참고 그림] Projects/AVSR/_exchange/in/levelup_mockup_a.png
[참고 그림] Projects/AVSR/_exchange/ref/ui_reborn_1023/host_vs_mock_1026.png
PD 말: 「일시정지 팝업 정리가 하나도 안 되어 있다 · 레벨업 카드랑 현재 호스트 정보를 제대로 시안 뽑아서 정리해줘 · 속성 표시는 작게 들어가서 뭘 의미하는지 모르겠다」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
[장면] mock_pause_panel_free_peak — 네가 아트 디렉터라면 어떻게 꾸미겠나. RE:BORN 일시정지 창을 다시 짠 완성 화면 한 장. 창 위 「일시정지」 이름판 · 오른쪽 위 닫기 X · 「1-07 / 30 · 스토리」 줄은 지금처럼. 가운데를 두 칸으로 나눈다 — ① 「지금 빌드」: 고른 레벨업 카드를 한 줄 4개 × 두 줄(최대 8)로, 칸마다 카드 아이콘(레벨업 카드의 그림 그대로) · 아래 카드 이름 한 줄 · 오른쪽 아래 「Lv 2」 배지, 비면 흐린 빈 칸. 참고 그림의 레벨업 카드 화풍(남색 판 · 등급 띠)을 따른다. ② 「지금 몸」: 왼쪽에 몸 얼굴 큰 칸(등급 테두리) · 오른쪽에 이름 「갱스터」 + 등급 꼬리표 · 체력 줄(182/207) · 그 아래 스킬 두 칸(액티브 · 필살기 아이콘 + 아래 이름). 속성은 아이콘만 두지 말고 「파워」처럼 글자와 함께 작은 꼬리표로. 아래 버튼 줄 「설정」 · 「포기하기」(빨간 글) · 큰 노란 「계속하기」는 지금처럼. 글자는 전부 한국어, 칸 밖으로 넘치지 않게.
기준 그림은 참고일 뿐, 더 멋진 안이 있으면 그걸로. 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변) + 떠오르는 빛 알갱이를 함께.
- 크기는 바탕과 같은 720 x 1280. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
