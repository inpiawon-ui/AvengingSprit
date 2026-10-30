$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
액티브 「혼 깃들이기」 — 버프가 걸린 몸 머리 위에 떠 있는 「강해졌다」 표시.
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_infuse_free1.png 의 각 인물 머리 위 흰 위쪽 화살표 + 칼 아이콘. 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_inf_up.png
[그릴 것] 위쪽 화살표 하나와 비스듬한 칼 하나가 나란히 — 흰색 바탕에 보라 테두리 빛, 도트 선 깔끔.
[프레임] 3프레임 가로로 — 위아래로 살짝 둥실 + 빛이 반짝(반복).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 768 × 192. 한 프레임 약 160 × 128 칸 안에(게임에서 약 52 × 40).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_inf_up.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
