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
[화면] 소환 연출 흐름 스토리보드 — **한 장에 5컷**(세로로 위 → 아래 또는 2열): ① 10회 버튼 입력 ② UI 퇴장 · 화면 어두워짐(0.35초) ③ 이 컨셉의 장치가 열림(0.8초 · 영혼 10개 모임) ④ 유령이 호스트 실루엣 중 하나로 돌진(빙의 탐색) ⑤ 등급 확정 + 10회 결과 카드 2×5(SSR 1 · SR 2 · R 7, 높은 등급 마지막). 각 컷 옆에 짧은 한국어 메모(시간 · 효과 · R/SR/SSR 강약). 건너뛰기 버튼 위치도 보인다.
[저장] Projects/AVSR/_exchange/in/mock_uiv2_a_flow.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
