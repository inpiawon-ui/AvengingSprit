$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 등급별 연출 **시안**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (도트 모양 + 광원 · 파티클 섞기, 강약, 등급 레벨링),
Projects/AVSR/_exchange/guide_tiers_fx.md (이번 등급표 — 이대로 그린다).
레퍼런스 캡처(다른 게임 실제 장면 — 화풍이 아니라 「등급이 오를 때 무엇을 더하나」를 본다):
- Projects/AVSR/_exchange/ref/refboard/cap/25_w3m5xn.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/13_7w395d.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/17_n3uqh0.jpg
[바탕] Projects/AVSR/_exchange/ref/tiers_mock/base_gain.png — 지금 게임 화면. 창 · 칸 · 글자 · 카드 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다. 글자는 가리지 않는다.
PD 가 이미 뺀 것(다시 넣지 말 것): 물건 · 카드 뒤에 계속 붙어 있는 둥근 등급색 글로우 · 몸 둘레를 빙글빙글 도는 문양 · 물건 밑 단색 판(그림자).
PD 말: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지」 · 「힘을 더 준다 = 크기가 아니라 파티클 수를 조금 늘리고 종류를 다양하게」.
[장면] 고른 카드가 유령에게 **깃드는 순간**(강, 0.5 초 안) — 두 칸 나란히: 왼쪽 COMMON, 오른쪽 LEGENDARY. 세로 섬광 한 번 + 도트 알갱이가 위로 + 유령 머리 위에 카드 아이콘 · 이름 글자. 등급이 높으면 알갱이 수 · 섬광 겹만 조금 더(크기는 같게). 몸 둘레를 도는 문양 · 큰 고리 여러 겹 금지. 유령 그림은 그대로.
- 크기: 1400 x 700. 등급마다 칸을 나란히, 위에 등급 이름을 그대로 둔다. 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 · 언제 · 어떻게 움직이는지)를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_tiers_gain.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
