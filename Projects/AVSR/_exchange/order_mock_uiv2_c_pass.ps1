$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 모바일 UI 리뉴얼 2차 시안 — 컨셉 C 「영혼의 픽셀 연대기」 — HD-2D 박물관(잉크 남색 · 양피지 판 · 고동금 테 · 디오라마).
먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v2_screens.md (이번에 그릴 화면 표 — 정답) · Projects/AVSR/_exchange/plan_ui_v2.md (공통 규칙 · 샘플 데이터).
화풍 기준 = 같은 컨셉 1차 시안(이 화풍 · 위 막대 · 아래 바 · 판 모양 그대로): Projects/AVSR/_exchange/in/mock_uiv2_c_home.png · mock_uiv2_c_growth.png · mock_uiv2_c_summon.png
그림 앵커: 유령 Projects/AVSR/_exchange/ref/ui_v2/ghost_anchor.png · 갱스터 ref/ui_v2/host_gangster_portrait.png · 닌자(사슬) ref/ui_v2/host_ninja_chain_portrait.png · 별 ref/ui_v2/ch_star_gold.png(큰 금별) · ch_star_silver.png(작은 은별). 도트 캐릭터는 도트 그대로.
지금 게임의 같은 화면(무엇이 들어가는지 참고 · 모습은 따라 하지 말 것): Projects/AVSR/_exchange/ref/ui_v2/now_pass_rewards.png · ref/ui_v2/now_pass_missions.png
세로 9:16 한 장 · 글자는 한국어로 정확히 · 본문 24px 이상(720 폭 기준) · 숫자 28~40 · 20px 미만 금지 · 터치 칸 72 이상 · 큼직하게 정리, 넘치면 탭 · 스크롤 · 시트로.
[화면] plan_ui_v2_screens.md 표의 **7번** — 표에 적힌 것을 빠짐없이 넣는다.
[저장] Projects/AVSR/_exchange/in/mock_uiv2_c_pass.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
