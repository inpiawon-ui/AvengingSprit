$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 5챕터 방 전체 기믹 「가스 누출 띠」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_gas_band.md 를 읽는다(목표 모습 · 부품 표 · 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_gas_band_free_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-GB-03 fx_gas_warn — 가스 예고 빗금 띠 한 토막(빨간 사선 빗금 + 위 · 아래 가는 빨간 테)
[앵커] Projects/AVSR/_exchange/ref/gas_band_mock/crop_warn.png — 고른 예고 시안(in/mock_gas_band_guide_idle.png)에서 잘라 2배로 키운 것. 이 빗금 굵기 · 간격 · 색을 따른다.
- 한 장 **256 × 128**. 사선 빗금이 칸 전체를 채우고, **왼쪽 끝과 오른쪽 끝이 이어 붙도록**(가로로 여러 장 깔아 방 폭 띠를 만든다). 위 · 아래 끝에 가는 빨간 테 한 줄.
- 색: 위험 빨강 `#FF4D47` — 빗금은 진하게, 빗금 사이는 비운다(검정).
- **완전한 검은 바탕**(#000000) — 게임에서 가산으로 얹는다. 김 · 배관 · 바닥은 그리지 않는다(빗금만).
[저장] Projects/AVSR/_exchange/in/fx_gas_warn.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
