$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하(도구 한도). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 영매 스킬 「흑염의 칼라(원혼 강림)」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_skill_medium_v2.png (PD 「시안 퀄 정도는 나와야 한다 · 과하지 않으면서 이쁘게」).
[기준 그림 — 이 모양 · 색 · 화풍 그대로] Projects/AVSR/_exchange/ref/skillmock/anchor_medium_start.png (등 뒤에 강림한 푸른 원혼)
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있는 빛 · 캐릭터 · 적 · 바닥 · 글자는 그리지 않는다(부품만).
[만들 것] 시전자 등 뒤에 떠 있는 **푸른 원혼**(파랑 · 하늘빛 불꽃 몸, 두 팔을 벌린 유령 상반신, 아래는 불꽃 꼬리로 흐려짐, 빛나는 눈) 6장 반복 — 불꽃 몸이 일렁인다. 한 장 160x192, 6장 가로 한 줄(960x192).
[저장] Projects/AVSR/_exchange/in/part_md_wraith.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
