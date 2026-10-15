$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 720x1280 비율) — **로비 상점 · 시즌 패스** UI 디자인 시안. 완성된 게임 화면 한 장으로 그린다.
[먼저 볼 것]
- 지금 로비(이 화풍 그대로 이어서): Projects/AVSR/_exchange/ref/store_mock/base_lobby.png — 남색 판 · 하늘색 네온 테두리 · 둥근 알약 · 도트 글꼴 · 위 재화 줄(유령 Lv · 골드+ · 젬+ · 우편 · 설정) · 하단 바 HOST / PLAY / SHOP.
- 인게임 HUD(같은 한 벌): Projects/AVSR/_exchange/ref/ingame_ui/hud_draft_D.png
- 유령은 반드시 이 그림 모습으로: Projects/AVSR/Reference/Hosts/idle/ghost_anchor_user_x8.png — 통통한 달걀 몸 · 왼쪽 아래 짧은 뾰족 꼬리 · 큰 눈 · 얼굴 폭만 한 입에 주황 혀.
- 기획: Projects/AVSR/AVSR_Content_Store.md (B-6 화면 이름표) · 상품 Projects/AVSR/Store/store_products.tsv · 패스 Projects/AVSR/Store/pass_s1.tsv
[공통]
- 글자는 한국어. 값은 원화(₩) 예시로. 가챠 · 확률 상품은 없다.
- 고급스럽게, 하지만 이 게임 도트 화풍 그대로(매끈한 3D · 실사 금지). 정보가 한눈에 읽히게 — 값 · 수량 · 남은 횟수가 또렷하게.
- 그림 아래 여백에 한국어 메모 3줄 이내(무엇을 어디에 · 왜).
[화면] 외형(꾸미기) 창 — 유령 육성 화면(Projects/AVSR/_exchange/ref/store_mock/base_growth_ghost.png) 위에 뜨는 팝업.
- 탭 둘 「유령 외형 · 프로필 테두리」. 「유령 외형」 선택.
- 왼쪽 크게 미리보기: 기준 유령이 시즌 외형 「복수의 유령」(붉은 눈 · 보라 불꽃 · 찢어진 망토 끝)을 입은 모습, 가볍게 떠 있는 자세.
- 오른쪽 격자: 기본 유령(장착 중 체크) · 복수의 유령(보유) · 잠긴 칸 둘(「시즌 2 패스」 「곧 공개」). 칸 아래 얻는 곳.
- 아래: 「능력치 변화 없음 · 꾸미기 전용」 작은 글씨 · 「장착」 버튼.
[저장] Projects/AVSR/_exchange/in/mock_store_cosmetic.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
