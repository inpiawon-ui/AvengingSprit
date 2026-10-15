$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — **조각 뽑기(소환)를 일반 구매와 구분**하는 UI 시안. 완성된 게임 화면 한 장.
[먼저 볼 것]
- 지금 시안(화풍 · 틀 그대로): Projects/AVSR/_exchange/in/mock_store_gacha.png(뽑기 탭) · Projects/AVSR/_exchange/in/mock_store_currency.png(재화 탭) · Projects/AVSR/_exchange/ref/store_mock/base_lobby.png(로비)
- 유령은 Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png 모습 그대로. 호스트 얼굴 Assets/BaseResource/Growth/Faces/face_{키}.png
PD 말: 「가챠랑 일반 구매랑 구분이 돼야 하지 않을까?」 — 지금은 상점 탭 「오늘 · 상자 · 조각 뽑기 · 재화」 중 하나라 확정 구매와 뽑기가 같은 판 · 같은 색으로 섞여 있다.
[구분 원칙] 확정 구매(값을 내면 무엇을 받는지 다 보인다)는 **상점**, 확률 상품(조각 뽑기)은 **소환** — 들어가는 문 · 판 색 · 분위기가 달라야 한다.
- 상점: 지금 남색 판 · 하늘색 네온 · 깔끔한 진열대 느낌.
- 소환: 보라 · 검붉은 영혼 불꽃 · 제단(의식) 느낌. 「확률 보기 · 뽑기 기록」은 소환 화면에 늘 또렷하게.
[공통] 글자 한국어 · 값 원화(₩)/젬. 이 게임 도트 화풍 그대로. 그림 아래 한국어 메모 3줄 이내.
[A안] 하단 바를 **네 칸 「HOST · PLAY · 소환 · SHOP」**으로 늘리고, 「소환」을 고른 상태의 **소환 전용 화면**을 그린다.
- 하단 바: 소환 칸은 보라 불꽃 아이콘(유령이 영혼 불꽃을 든 모습) · 「SUMMON / 소환」 · 오늘 무료 남으면 빨간 점. 네 칸이 다 읽히게 칸 폭을 맞춘다.
- 소환 화면: 위 재화 줄 아래 전체가 보라 제단 무대 — 이번 주 픽업(닌자(사슬)) · 무료 1회 · 1회 · 10회(A급 1회 보장) · 대박까지 37/50 · 보유 소환권 · 「확률 보기」 「뽑기 기록」.
- 상점(SHOP)에는 이제 뽑기 탭이 없다(오늘 · 상자 · 재화 셋).
[저장] Projects/AVSR/_exchange/in/mock_summon_a.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
