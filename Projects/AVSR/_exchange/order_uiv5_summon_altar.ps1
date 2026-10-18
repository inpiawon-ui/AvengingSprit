$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 UI 리뉴얼(혼합 컨셉 「픽셀 연대기 × 쇼케이스」). 먼저 읽는다: Projects/AVSR/_exchange/plan_ui_v3.md
PD 가 고른 소환 시안: Projects/AVSR/_exchange/in/mock_uiv5_summon.png
앵커(그 시안의 위 장면을 자른 것 — 이 그림 그대로, 더 깨끗하게): Projects/AVSR/_exchange/ref/ui_v5/summon_altar.png
그릴 것: **소환 화면 위 장면 한 장** — 달밤 부두 · 철골 · 가로등 · 왼쪽 「AVENGING SPIRIT RE:BORN」 간판 · 둥근 달, 가운데에 펼친 낡은 「영혼 명부」 책과 그 위 파란 영혼 고리(마법진),
가운데 웃는 흰 유령(통통한 몸 · 짧은 꼬리 · 큰 입 — Assets/BaseResource/Growth/Portraits/portrait_ghost.png 그대로), 둘레를 도는 양피지 호스트 실루엣 카드 8~10장, 작은 파란 유령 불꽃 몇 개.
책 앞면 띠의 「영혼 명부」 글자는 넣어도 된다. **왼쪽 위 「<」 단추 · 「소환」 제목 글자는 그리지 마라** — 게임이 얹는다(그 자리는 어두운 하늘로 비워 둔다).
크기 1440 × 1100, 꽉 찬 그림(투명 · 마젠타 없음). 도트 픽셀아트 질감.
[저장] Projects/AVSR/_exchange/in/uiv5_summon_altar.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
