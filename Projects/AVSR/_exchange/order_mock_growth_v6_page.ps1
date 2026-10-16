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
[장면] 호스트 육성 화면 한 장 — plan 의 배치표 그대로(위 막대 → 육성 탭 → 몸 카드 → 다음 성급 요약 띠 → 스킬(필살기 · 액티브 · 6성 각성 옵션 · 패시브) → 능력치 강화 → 호스트 목록 → 아래 바).
성급은 반 칸 없이 「성급 3/10」 + 5칸 별(금 3개 채움). 호스트 목록 카드(갱스터 · 폭력배 · 아마존 · 호퍼 · 닌자)엔 등급 배지(R 청록 · SR 보라 · SSR 금)와 「N성」 + 미니 5칸 별 — 1~5성은 금별, 6~10성은 금별이 왼쪽부터 보라별로 바뀐다(예: 닌자 SR 7성 = 보라 2 + 금 3).
필살기 줄이 세 스킬 중 가장 크고 화려하게(금 + 적색 발광 테), 다음 값은 초록. 네가 아트 디렉터라면 더 읽기 쉽게 다듬어도 된다 — 단 배치 순서와 글자 정보는 지킨다.
[저장] Projects/AVSR/_exchange/in/mock_growth_v6_page.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
