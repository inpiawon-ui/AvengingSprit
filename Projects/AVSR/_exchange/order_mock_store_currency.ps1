$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — **로비 상점** UI 디자인 시안 2차. 완성된 게임 화면 한 장으로 그린다.
[먼저 볼 것]
- 1차 시안(이 화풍 · 틀 그대로 이어서): Projects/AVSR/_exchange/in/mock_store_shop.png · Projects/AVSR/_exchange/in/mock_store_pass.png — 남색 판 · 하늘색 네온 · 둥근 알약 · 도트 글꼴 · 위 재화 줄 · 하단 바 HOST / PLAY / SHOP(SHOP 선택).
- 지금 로비: Projects/AVSR/_exchange/ref/store_mock/base_lobby.png
- 호스트 얼굴은 게임에 있는 것을 그대로: Assets/BaseResource/Growth/Faces/ (face_{호스트키}.png), 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로.
- 기획: Projects/AVSR/AVSR_Content_Store.md · 뽑기 Projects/AVSR/Store/gacha.tsv
[2차에서 바뀐 것] 상점 탭은 「오늘 · 상자 · 조각 뽑기 · 재화」 넷. 시즌 패스는 따로(로비 배지). 외형 판매는 없다.
[공통] 글자 한국어 · 값 원화(₩). 이 게임 도트 화풍 그대로. 값 · 수량 · 남은 횟수가 또렷하게. 그림 아래 한국어 메모 3줄 이내.
[화면] 상점 「재화」 탭.
- 맨 위: 계약 두 칸 「영혼 계약 ₩5,900 — 즉시 젬 300 + 30일 매일 90」 「망령 군주의 계약 ₩19,000 — 즉시 1,000 + 매일 200 + 편의」.
- 그다음: 「입문 패키지 · 6일 남음」 3단 ₩1,200 / ₩5,900 / ₩12,000 (젬 · 소환권 · 정수 묶음) — 1차 시안처럼.
- 그다음: 젬 묶음 6칸 격자 — 60 ₩1,200 · 320 ₩5,900 · 680 ₩12,000 · 2,100 ₩39,000 · 3,600 ₩65,000 · 7,500 ₩129,000, 칸마다 「첫 구매 2배」 띠, 젬 더미가 칸마다 커진다.
- 맨 아래: 골드 3칸(젬으로) — 골드 더미 작은/중간/큰, 「젬 30 · 오늘 2/3」 「젬 150」 「젬 450」.
- 아주 아래 작게 「특정상거래법 · 자금결제법 표기」 「구매 복원」.
[저장] Projects/AVSR/_exchange/in/mock_store_currency.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
