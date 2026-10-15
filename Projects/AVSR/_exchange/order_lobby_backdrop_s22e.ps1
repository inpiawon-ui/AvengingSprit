$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 로비 **배경만** 남긴 판.
[바탕] Projects/AVSR/_exchange/in/lobby_plate_s22e.png (1255 x 1790).
- 이 그림에서 **가운데 남색 칸(테두리 판) 여섯 개를 전부 지운다** — 챕터 칸 · 보스 칸 · 보상 칸 · 노란 막대 제목 칸 둘 · 아래 설명 칸, 그 안의 얇은 선 · 노란 막대 · 작은 네모까지 모두.
- 지운 자리는 **뒤에 있을 배경을 이어 그린다**: 위는 밤 도시 · 하늘, 가운데는 좌우 배관 벽 사이의 어두운 안쪽 벽(배관 · 철판 · 은은한 네온 — 너무 밝거나 복잡하지 않게, 위에 칸이 다시 덮일 자리라 차분하게), 아래는 지금처럼 젖은 바닥.
- 좌우 배관 벽 · 위 도시 · 아래 바닥은 지금 그림 그대로 둔다. 크기 1255 x 1790 그대로. 글자 · 아이콘 · 칸 테두리 없음.
[저장] Projects/AVSR/_exchange/in/lobby_backdrop_s22e.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
