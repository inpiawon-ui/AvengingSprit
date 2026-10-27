$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 4챕터 방 전체 기믹 「번개 + 물웅덩이」 **부품**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(색의 뜻 · 도트 모양 + 광원) · 다음 Projects/AVSR/_exchange/guide_lightning_puddle.md 를 읽는다(부품 표 · 2차 크기 규칙 — 작게 · 부분적으로).
[정답 시안] Projects/AVSR/_exchange/in/mock_lightning_puddle_guide_peak.png — PD 가 고른 그림. **이 화풍 그대로** 부품만 떼어 그린다.
[부품] FX-LP-03 obj_puddle — 옥상 바닥의 얕은 물웅덩이 4가지
[앵커] Projects/AVSR/_exchange/ref/lightning_puddle_mock/crop_puddle_zap.png — 시안에서 잘라 낸 것. 이 모양 · 색 · 굵기를 따른다.
- 한 장에 **4칸 가로로**(칸마다 256 × 160, 전체 1024 × 160). 칸마다 모양이 다른 납작한 웅덩이(위에서 내려다본 모습), 칸의 80% 안.
- 물빛: 어두운 남청 수면 + 하늘(노을) 반사가 아주 약하게 · 가장자리는 젖은 바닥처럼 조금 짙게. 전기 · 반짝이 · 그림자 판 금지(빛은 따로 낸다).
- 정답 시안의 웅덩이 모양 · 색 그대로. **분홍 바탕**(#FF00FF) — 나중에 뚫는다. 웅덩이 안에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/lp_puddle.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
