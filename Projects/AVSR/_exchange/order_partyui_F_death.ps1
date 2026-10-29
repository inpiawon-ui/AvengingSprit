$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 「빙의 파티」 인게임 UI 시안. 크기 720 x 1280.
[정답 틀 = PD 확정] Projects/AVSR/_exchange/in/mock_partyui_S1b.png — 위쪽 한 줄 판(유령 HP · **메인 칸 크게 + 체력 막대 + 왕관** · **보조 썸네일 작게(이름 · 체력 없음)** · 빈 자리 점선 「+ −10」 · 스테이지 · 골드 · 일시정지) + 큰 레벨 줄 + 판 바로 밑 왼쪽에 붙은 시너지 칸(육각 문장 · 이름 · 「2/3」 만, 점 불빛 없음). 색 · 재질 · 글자 · 크기를 그대로 따른다. 위쪽 전체는 화면 위 190px 안, 시너지 칸은 왼쪽 폭 90px 안 · 한 번에 2~3칸까지.
[원칙] 전투를 가리지 않는다 · 최대한 심플하게 · 장식 · 빛 번짐 최소 · 한국어 글자.
[이번 장 — 메인 사망 선택창] 바탕 모양은 Projects/AVSR/_exchange/in/mock_partyui_death.png(PD 가 괜찮다고 한 창) — 이것을 **정답 틀(S1b)의 색 · 판 · 썸네일 크기**에 맞춰 다듬는다.
- 메인 「슬러거」가 쓰러져 유령이 튕겨 나온 순간. 위쪽 판의 메인 칸은 비어 유령 얼굴(①상태). 유령 HP 60 → 30(−30, 붉은 감소 표시). 원형 시계 「5.2초」.
- 줄 셋: 「A. 근처 호스트에 새로 빙의 −20」 · 「B. 보조 1 「아마존」 을 메인으로 −5」(추천 · 금빛) · 「C. 그 자리에서 슬러거 부활 −40」(유령 HP 부족 · 어둡게 잠김). 줄마다 썸네일 · 남는 유령 HP.
- 그림 오른쪽 아래 여백 없이 꽉 채우고 설명 메모 · 치수 표시는 넣지 마라.
[저장] Projects/AVSR/_exchange/in/mock_partyui_F_death.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
