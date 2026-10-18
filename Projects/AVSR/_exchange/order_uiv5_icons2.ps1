$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」). 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md
앵커(시안 위 막대와 소환 버튼 줄): Projects/AVSR/_exchange/ref/ui_v5/home_topbar.png · Projects/AVSR/_exchange/in/mock_uiv5_summon.png
기존 아이콘 결(이 셋과 같은 도트 질감 · 크기감으로): Assets/BaseResource/ChapterHost/ch_icon_gold.png · ch_icon_gem.png · ch_icon_mail.png
캔버스 2048 × 512, 배경은 순수 마젠타(#FF00FF) 단색. 부품을 가로로 나란히, 각 약 220 × 220, 사이 여백 60px 이상, 캔버스 끝에 닿지 않게.
 1. 행동력 번개 — 노란 번개(시안 위 막대 「⚡18/20」 의 번개)
 2. 「+」 단추 — 작은 파란 정사각 단추 + 흰 「+」 (시안 위 막대 재화 옆)
 3. 뒤로 단추 — 남색 정사각 판 + 금테 + 금색 「<」 (시안 소환 왼쪽 위)
 4. 트로피 — 금색 우승컵
 5. 자물쇠 — 금속 자물쇠(잠긴 칸 표시)
 6. 체크 — 청록 동그라미 안 흰 체크(받음 표시)
**글자 · 판 바깥 빛 번짐은 그리지 마라.**
[저장] Projects/AVSR/_exchange/in/uiv5_icons2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
