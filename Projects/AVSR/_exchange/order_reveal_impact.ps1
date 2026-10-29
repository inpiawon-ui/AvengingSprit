$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「실루엣 → 공개」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (이 게임 빛 · 연출 원칙) · Projects/AVSR/_exchange/guide_summon_reveal.md (박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_reveal_free_peak.png
PD 말: 「motion-library buy_shop_item_popup_2 영상(실루엣 → 새 스킨 공개)을 참고해서 우리 가차 연출을 적용」.

[부품] 착지 충격 — 몸이 바닥에 착지할 때 튀는 바닥 파편 · 먼지, 6장(한 번 재생)
- 형태 그림이다: **마젠타 #FF00FF 단색 바탕**(반투명 헤일로 금지, 검은 바탕 금지).
- 가로 6칸 한 줄(칸 비율 2:1 — 바닥 높이 장면). 1 착지 순간 바닥 금 · 파편이 튀어오름 → 3 가장 크게 → 6 파편이 떨어져 사라짐.
- 자유 시안 공개 순간 발밑의 깨진 돌 조각 · 먼지처럼 도트로. 빛 번짐은 그리지 마라(빛은 따로).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/reveal_impact.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
