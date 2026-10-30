$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 그라데이션을 그려 때우지 마라. 한 장만 그린다.
먼저 공용 브리프 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 모바일 게임) 이펙트 부품 발주. 영매(버프 마법사) 스킬.
액티브 「혼 깃들이기」 — 시전 순간 영매에게서 퍼져 나가 파티에 닿는 보라 혼 고리(한 번).
[정답 그림] Projects/AVSR/_exchange/mock1030_skill/mock_infuse_free1.png 의 인물들을 잇는 보라 빛 물결 · 고리. 크롭(3배): Projects/AVSR/_exchange/fx_medium1030/anchor_inf_group.png
[그릴 것] 위에서 비스듬히 본 납작한 타원 충격 고리 — 보라 · 자주 빛, 가장자리 혼불 알갱이.
[프레임] 5프레임 가로로 — 작은 고리가 크게 퍼지며 엷어져 사라진다(한 번).
[바탕] 순수 검정(#000000) 단색 — 게임이 검정을 투명으로 바꾼다(가산). 프레임 사이 검은 빈칸 넉넉히.
글자 · 숫자 · 사람 · 캐릭터 없음. 이펙트만.
[크기] 캔버스 1600 × 288. 한 프레임 약 300 × 130 칸 안에(게임에서 약 300 × 120).
[저장] Projects/AVSR/_exchange/fx_medium1030/sheet_inf_burst.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
