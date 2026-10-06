$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.

[할 일] 「호스트 육성」 화면 레이아웃 시안 v2. 세로 9:16, 720 x 1280 비율.
[바탕] Projects/AVSR/_exchange/in/growth_merge_mock_v1.png — 이 시안을 거의 그대로 두고 아래만 고친다. 그림체 · 색 · 틀 · 글자는 v1 그대로.
[참고] 지금 게임 화면 Projects/AVSR/_exchange/ref/growth_now_host_stat.png

[고칠 것 — 검수 결과]
1. 호스트 목록: 지금은 4열 2행이라 둘째 줄이 하단 HOST/PLAY/SHOP 바에 잘린다.
   → 카드 **한 줄만**, 하단 바 위에 카드 전체(얼굴 · 이름 · 별 5개)가 **잘리지 않고 온전히** 보이게. 그 줄은 가로로 넘기는 목록 —
     오른쪽 끝 카드가 반쯤 걸쳐 보여 「옆으로 더 있다」가 읽히게(카드 4장 반 정도). 「호스트 목록」 제목과 「기본순」 정렬은 그 줄 바로 위 그대로.
2. 능력치 5줄: 줄과 노란 골드 버튼 높이를 **44px 이상**(720 폭 기준)으로 키운다. 5줄(공격력 · 최대 HP · 크리티컬 확률 · 크리티컬 피해 · 방어력) 모두 보인다.
3. 위 두 가지로 늘어난 높이는 이렇게 마련한다:
   - 몸 정보 카드를 낮게(캐릭터 그림을 조금 작게, 설명 2줄 · Lv · 별 · 조각 막대는 그대로).
   - 스킬 두 칸을 **납작하게**: 「ACTIVE SKILL / PASSIVE」 머리말을 칸 안 아이콘 옆 작은 꼬리표로 붙이고, 아이콘을 조금 작게. 설명 글은 끝까지 다 보여야 한다.
   - 섹션 사이 여백을 조금씩 줄인다.
[그대로] 맨 위 줄(로고 · 골드 · 우편 · 설정), 「유령 육성 / 호스트 육성」 큰 탭, 맨 아래 HOST · PLAY · SHOP 바의 위치 · 크기.
[글자] 한국어, v1 과 같은 글자. 깨짐 · 가짜 글자 금지.
[저장] Projects/AVSR/_exchange/in/growth_merge_mock_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
