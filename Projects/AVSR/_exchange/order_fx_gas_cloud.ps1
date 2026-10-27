$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 5챕터 방 전체 기믹 「가스 누출 띠」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_gas_band.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_gas_band_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-GB-01 fx_gas_cloud — 가로로 이어 붙이는 연두 독가스 구름 띠 한 토막(도트 연기 덩어리 · 기포 · 떠오르는 작은 알갱이)
[앵커] Projects/AVSR/_exchange/ref/gas_band_mock/crop_gas.png — 시안에서 잘라 2배로 키운 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **8칸 세로로**(칸마다 256 × 128, 전체 256 × 1024). 8장이 끊김 없이 돌게(구름이 일렁이며 피어오른다).
- **왼쪽 끝과 오른쪽 끝이 이어 붙도록**(가로로 여러 장 나란히 깔아 방 폭 띠를 만든다). 구름은 칸 아래쪽 2/3 안에 — 바닥에 깔려 위로 피어오르는 모양.
- 색: 노란 기가 도는 연두 `#99FF73` 계열, 밝은 곳은 옅은 노랑 · 하얀 심. 청록 · 초록(회복) · 보라 금지.
- **완전한 검은 바탕**(#000000) — 게임에서 가산으로 얹는다. 테두리 · 그림자 금지.
[저장] Projects/AVSR/_exchange/in/fx_gas_cloud.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
