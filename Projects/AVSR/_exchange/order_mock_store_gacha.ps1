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
[화면] 상점 「조각 뽑기」 탭.
- 위: 이번 주 픽업 띠 — 「이번 주 픽업: 닌자(사슬)」 큰 그림(그 호스트 얼굴 · 몸) + 「A급이 나오면 50% 확률로 픽업 호스트」 + 남은 시간.
- 가운데: 뽑기 버튼 셋 — 「무료 1회(오늘)」 · 「1회 젬 60 / 소환권 1」 · 「10회 젬 540 / 소환권 10 · A급 1회 보장」. 보유 소환권 수.
- 천장 막대: 「대박(조각 ×20)까지 37 / 50」.
- 아래 작은 버튼 「확률 보기」 「뽑기 기록」 — 확률 공개가 법으로 필요하다, 또렷하게.
- 분위기: 유령이 영혼 불꽃으로 몸들의 조각을 불러내는 제단 느낌(가챠지만 도박장처럼 번쩍이지 않게, 이 게임 톤).
[저장] Projects/AVSR/_exchange/in/mock_store_gacha.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
