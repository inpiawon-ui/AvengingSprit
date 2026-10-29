$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 모바일 UI 리뉴얼 3차 시안 — 혼합 컨셉 「픽셀 연대기 × 쇼케이스」.
먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md (PD 결정 10개 · 혼합 규칙 · 표기 규칙 · 화면 표 — 정답) · Projects/AVSR/_exchange/plan_ui_v2.md (샘플 데이터).
색 · 질감 기준(C): Projects/AVSR/_exchange/in/mock_uiv2_c_home.png · mock_uiv2_c_growth.png / 배치 · 글자 크기 기준(D): Projects/AVSR/_exchange/in/mock_uiv2_d_growth.png · mock_uiv2_d_home.png
그림 앵커: 유령 Projects/AVSR/_exchange/ref/ui_v2/ghost_anchor.png · 갱스터 ref/ui_v2/host_gangster_portrait.png · 닌자(사슬) ref/ui_v2/host_ninja_chain_portrait.png · 별 ref/ui_v2/ch_star_gold.png(큰 금별) · ch_star_silver.png(작은 은별) · 속성 ref/ui_v2/ch_kind_blade.png(무기) · ch_kind_force.png(파워) · ch_kind_magic.png(마법) · 스킬 아이콘 ref/ui_v2/ulticon_gangster.png · acticon_gangster.png. 도트 캐릭터는 도트 그대로.
장식은 줄인다(판당 테 한 줄 · 리본 · 모서리 문양 겹침 금지) — PD 「그래픽이 지저분하다」.
[화면] plan_ui_v3.md 화면 표의 **sortie** 줄 — 적힌 것을 빠짐없이.
[저장] Projects/AVSR/_exchange/in/mock_uiv3_sortie.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
