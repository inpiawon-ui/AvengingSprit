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
[화면] 프로필 꾸미기 창 — 로비 왼쪽 위 프로필(유령 얼굴 · Lv 10)을 눌러 뜨는 팝업.
- 위: 지금 프로필 미리보기 크게 — 테두리 + 대표 얼굴 + 이름(「유령」) + 칭호 한 줄(「시즌 1 복수자」).
- 탭 셋 「테두리 · 대표 얼굴 · 칭호」. 「테두리」 선택.
- 격자: 기본 테두리(장착 중) · 「GHOST REVENGE」 시즌 1 테두리(보유, 보라 불꽃 무늬) · 챕터 10 클리어 테두리(잠김 — 얻는 곳 표시) · 「곧 공개」 칸.
- 대표 얼굴은 유령 · 가진 호스트 얼굴 중에서 고른다는 걸 탭 아이콘으로 보여 준다.
- 아래: 「능력치 변화 없음 · 꾸미기 전용」 · 「장착」 버튼.
[저장] Projects/AVSR/_exchange/in/mock_store_profile.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
