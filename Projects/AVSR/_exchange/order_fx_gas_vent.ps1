$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 5챕터 방 전체 기믹 「가스 누출 띠」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_gas_band.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_gas_band_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-GB-02 fx_gas_vent — 바닥 배관 틈에서 솟는 짧은 연두 가스 분출 기둥
[앵커] Projects/AVSR/_exchange/ref/gas_band_mock/crop_vent.png — 시안에서 잘라 2배로 키운 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **6칸 가로로**(칸마다 64 × 128, 전체 384 × 128). 시간 순서: 1 틈에서 작게 → 2~4 위로 솟구침 → 5~6 흩어짐.
- 기둥 아래 끝은 칸 아래쪽 가운데. 색은 FX-GB-01 과 같다.
- **완전한 검은 바탕**(#000000) — 게임에서 가산으로 얹는다. 테두리 · 그림자 금지.
[저장] Projects/AVSR/_exchange/in/fx_gas_vent.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
