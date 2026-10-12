$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 등급별 연출 **시안**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (도트 모양 + 광원 · 파티클 섞기, 강약, 등급 레벨링),
Projects/AVSR/_exchange/guide_tiers_fx.md (이번 등급표 — 이대로 그린다).
레퍼런스 캡처(다른 게임 실제 장면 — 화풍이 아니라 「등급이 오를 때 무엇을 더하나」를 본다):
- Projects/AVSR/_exchange/ref/refboard/cap/01_6nd2h4.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/02_ciermd.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/03_zhlqcr.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/04_envg65.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/06_l20qv8.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/07_cz9sg1.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/08_rrsg5e.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/13_7w395d.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/14_5q06gj.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/16_383ffz.jpg
- Projects/AVSR/_exchange/ref/refboard/cap/17_n3uqh0.jpg
[바탕] Projects/AVSR/_exchange/ref/tiers_mock/base_result_tiers.png — 지금 게임 화면. 창 · 칸 · 글자 · 카드 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다. 글자는 가리지 않는다.
PD 가 이미 뺀 것(다시 넣지 말 것): 물건 · 카드 뒤에 계속 붙어 있는 둥근 등급색 글로우 · 몸 둘레를 빙글빙글 도는 문양 · 물건 밑 단색 판(그림자).
PD 말: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지」 · 「힘을 더 준다 = 크기가 아니라 파티클 수를 조금 늘리고 종류를 다양하게」.
[장면] 결과창 보상 칸 — 은 · 금 · 백금 상자가 칸에 **착지하는 순간**(강). 가이드 표의 「착지 순간」 행 그대로: 은 = 먼지 + 도트 반짝 별 몇 개 / 금 = + 등급색 고리가 퍼지는 중 / 백금 = + 고리 두 겹 + 위에서 내려오는 가는 빛줄기 + 도트 불꽃 알갱이. 상자 위에 등급 별(은 1 · 금 2 · 백금 3, 작은 도트 별). 금화 더미에서는 도트 금화 몇 개가 위로 튀어 오른다.
- 크기: 1536 x 640. 등급마다 칸을 나란히, 위에 등급 이름을 그대로 둔다. 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 · 언제 · 어떻게 움직이는지)를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_tiers_result_peak.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
