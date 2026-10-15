$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 아래에 적은 것만(3장 이하). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지). 칸 수 · 칸 크기를 꼭 지킨다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 호퍼 필살기 「킬 포인트」 이펙트 **부품**. PD 가 고른 시안(이 모양 · 색 · 화풍 그대로): Projects/AVSR/_exchange/ref/skillmock/r/hp_1.png (시안의 그 컷).
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결이 살아 있는 빛 · 캐릭터 · 적 · 바닥 타일 · 글자는 그리지 않는다(부품만).
[만들 것] 묶인 적 발밑의 **붉은 사슬 고리 표식**(납작한 타원 붉은 고리 + 고리를 감은 사슬 마디 + 위로 튀는 붉은 불티, 가운데 비움) 4장 반복(사슬이 살짝 돌고 빛이 깜빡) — 한 장 160x96, 4장 가로 한 줄(640x96).
[저장] Projects/AVSR/_exchange/in/part_hp_bind.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
