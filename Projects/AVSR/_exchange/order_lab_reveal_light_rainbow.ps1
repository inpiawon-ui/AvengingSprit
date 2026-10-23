$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터 · 색 돌리기로 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 최고 등급(대박) 공개 빛 루프 6장.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab_v2.md
**모양 · 구도 · 루프의 정답**: Projects/AVSR/_exchange/in/lab_reveal_light.png (금빛 판 — 이것과 칸 수 · 칸 크기 · 빛 뿌리 자리 · 빛살 수 · 알갱이까지 같게)
화면 속 모습: Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png

[부품] 대박 공개 빛 — 금빛 판과 같은 그림인데 **빛살 가장자리 · 알갱이가 무지개(분홍 · 보라 · 하늘 · 연두 · 금)로 물든다**
- 빛 뿌리(아래 가운데)와 빛살 속은 흰 · 금 그대로 밝게, 바깥으로 갈수록 무지개 띠가 은은하게 번진다(쨍한 원색 줄무늬 금지).
- 네모 도트 알갱이 · 반짝 별도 무지개 여러 색으로 섞는다.
- **검은 바탕(#000000)**, 가로 6칸 한 줄, 정사각 칸. 1→6→1 끊김 없이 천천히 일렁인다(금빛 판과 같은 움직임).
- 캐릭터 · 배양조 · 글자는 그리지 마라.
※ 크기가 조금 달라도 고치려 하지 말고 그대로 저장하라. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_reveal_light_rainbow.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
