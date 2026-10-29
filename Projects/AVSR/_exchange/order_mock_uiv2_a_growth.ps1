$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 모바일 UI **전체 리뉴얼 시안** — 컨셉 A 「빙의 아케이드」 — 굵고 경쾌한 현대식 아케이드(먹색 바탕 · 크림 정보판 · 민트/노랑 강조 · 장난감 같은 그림자 뎁스).
먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v2.md (합친 안 — 공통 규칙 · 샘플 데이터 · 바꾼 점이 정답) · Projects/AVSR/_exchange/consult_ui_v2_concept_answer.md (네가 낸 답 — 이 컨셉 표와 발주 단락).
그림 앵커(그대로 쓸 것): 유령 Projects/AVSR/_exchange/ref/ui_v2/ghost_anchor.png (통통한 달걀 몸 · 왼쪽 아래 짧은 꼬리 · 큰 눈 · 큰 입) · 갱스터 Projects/AVSR/_exchange/ref/ui_v2/host_gangster_portrait.png · 닌자(사슬) Projects/AVSR/_exchange/ref/ui_v2/host_ninja_chain_portrait.png · 스킬 아이콘 ref/ui_v2/ulticon_gangster.png · acticon_gangster.png · 별 ref/ui_v2/ch_star_gold.png · ch_star_silver.png. 캐릭터는 도트 그대로(정수 배율로 크게).
하지 말 것의 예: Projects/AVSR/_exchange/ref/ui_v2/now_board.png — 작은 글자 · 청록 네온 테두리 칸을 빽빽하게 쌓은 지금 모습.
PD 말: 「정보가 너무 작게 뜬다 · 그래픽이 너무 지저분하다 · 레이아웃 큼직하게, 뎁스가 필요하면 주더라도 정리」.
규칙: 세로 9:16(720 × 1280 비율) 한 장 · 글자는 한국어로 정확히(샘플 데이터 그대로) · 본문 24px · 숫자 28~40 · 제목 36~40 기준(720 폭) · 20px 미만 글자 금지 · 터치 칸 72 이상 · 아래 바 4칸 HOST · PLAY(가운데 크게) · SUMMON · SHOP · 위 막대는 ⚡18/20 · 골드 7,396 · 젬 50.
[화면] 호스트 육성 — 갱스터 R · 「3성」 + 은별 3 · 전투력 1,420 · 큰 도트 캐릭터(화면 35% 이상) · 탭 셋 「기본 능력 / 스킬 / 성급」(지금 「스킬」 탭) · 스킬 카드 셋(필살기 「청부 처형」 빙의할 때 1회 · 위력 ×1.08 → 4성 ×1.12 / 액티브 「일제 표식」 평타 30타 충전 · ×1.12 → ×1.18 / 패시브 「처형 계약」 즉사 22.2% → 23.3%) · 「6성 각성 옵션 · 잠김」 한 줄 · 큰 성급 UP 버튼(조각 16/16 · 골드 960) · 아래 호스트 4명 캐러셀 · 아래 바.
[저장] Projects/AVSR/_exchange/in/mock_uiv2_a_growth.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
