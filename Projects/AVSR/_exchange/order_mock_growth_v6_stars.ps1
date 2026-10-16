$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) — **호스트 육성 화면 v6 UI 시안**.
먼저 읽는다: Projects/AVSR/_exchange/plan_growth_v6.md (합친 안 — 배치표 · 성급 표기 · 줄 목록이 정답) · Projects/AVSR/_exchange/consult_growth_v6_answer.md (네가 낸 답).
화풍 기준(이 화풍 그대로): Projects/AVSR/_exchange/in/scene_s22e_host.png (로비 — 위 막대 · 아래 바 4칸 · 칸 테두리 · 글꼴 · 등급 카드) · Projects/AVSR/_exchange/in/growth_starup_mock_v4.png (지난 승인 육성 시안).
지금 게임 화면: Projects/AVSR/_exchange/ref/growth_v6/now_host_page_1006.png
참고 그림: 필살기 시안 Projects/AVSR/_exchange/ref/growth_v6/ult_gangster.jpg (아이콘은 여기서 「붉은 조준선 + 거대한 한 발」을 정사각으로 따 온 듯 그린다) · 액티브 시안 Projects/AVSR/_exchange/ref/growth_v6/active_gangster.jpg
PD 말: 「필살기 정보가 없고 성급에 따라 어떻게 좋아지는지 안 보인다 · 성급 1성~10성, 별 쪼개는 건 별로 · 성급이 오르면 스킬 레벨 · 강화 상한이 오른다는 걸 전체적으로 잘 보이게」.
글자는 전부 한국어(이름 · 숫자 그대로 정확히). 세로 9:16(720 × 1280 비율).
예시 몸: 갱스터 · 등급 R · 컨셉 「청부 저격수 — 보스 킬러」 · 지금 3성(→ 4성) · 조각 16/16 · 성급 UP 골드 960 · 전투력 1,420
  필살기 「청부 처형」 SPECIAL · 빙의할 때 1회 · 위력 ×1.08 → ×1.12 · 한 줄 요약 「가장 강한 적을 조준해 초대형 한 발」
  액티브 「일제 표식」 평타 30타 충전 · 위력 ×1.12 → ×1.18 · 4성 「보조↑ 표식 3초→3.5초」 · 한 줄 「방 안 모든 적에게 표식」
  6성 각성 옵션(잠김, 자물쇠) 「표식 붙은 적이 받는 피해 +15%」
  패시브 「처형 계약」 즉사 확률 22.2% → 23.3% · 한 줄 「표식 붙은 적을 확률로 즉사」
  능력치 강화 5줄(공격력 · 최대 HP · 크리티컬 확률 · 크리티컬 피해 · 방어력) Lv 0/15 · 제목줄 「3성 강화 상한 Lv 15」
[장면] 「성급 표기판」 — PD 가 1성~10성 표기를 고르는 설명용 한 장(게임 화면이 아니라 시안 보드, 배경은 같은 화풍의 어두운 판).
위: 1성부터 10성까지 10줄(또는 2열 × 5) — 각 줄에 「N성」 숫자 + 5칸 별. 1~5성은 금별이 왼쪽부터 찬다, 6~10성은 금별 5칸이 왼쪽부터 보라별로 바뀐다(10성 = 보라 5).
가운데: 같은 규칙이 작은 호스트 카드(가로 약 100 px — 초상 · 등급 배지 · 이름 · 「7성」 + 미니 5칸 별)와 큰 몸 칸(「성급 7/10」 + 큰 5칸 별)에서 어떻게 보이는지 나란히. 등급 R · SR · SSR 카드 세 장으로.
아래: 이정표 띠 1~10성 — 4 · 7 · 10성 「보조↑」, 6성 「각성 옵션」, 10성 「각성 강화 ×2」 표식. 보드 위쪽에 제목 「성급 표기 — 1성~10성(반 칸 없음)」.
[저장] Projects/AVSR/_exchange/in/mock_growth_v6_stars.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
