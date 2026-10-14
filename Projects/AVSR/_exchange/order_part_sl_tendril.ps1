$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하(도구 한도). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 슬러거 스킬 「풀스윙 회오리」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 「시안 퀄 정도는 나와야 한다 · 과하지 않으면서 이쁘게」).
[기준 그림 — 이 모양 · 색 · 화풍 그대로] Projects/AVSR/_exchange/ref/skillmock/anchor_slugger_tendril.png · anchor_slugger_tendril2.png (PD 가 고른 시안 — 회오리 **바깥쪽**으로 뻗어 나가 적을 감아 끌고 오는 긴 바람 팔)
PD 2026-10-14 「가운데는 맞는데, 상대편을 끌고 오는 느낌이 드는 이펙트가 회오리 바깥쪽에 더 있어 — 저런 디테일이 중요한 거야」.
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결 · 캐릭터 · 적 · 돌 · 글자 금지.
[만들 것] **회오리에서 뻗어 나간 긴 바람 팔 하나**: 오른쪽 끝(바깥, 적이 있는 쪽)에서 왼쪽 끝(회오리 가운데 쪽)으로 휘어 감기는 흰 · 하늘빛 바람 줄기 2~3가닥(한 다발), 바깥 끝은 갈고리처럼 말려 있고 안쪽으로 갈수록 굵고 밝다. 끝이 가늘어지는 붓 터치, 줄기 사이는 비어 있다.
6장 반복 — 바람이 바깥에서 안쪽으로 **흘러 들어가는** 움직임(무늬가 장마다 왼쪽으로 조금씩 흐른다). 한 장 384x128, 6장 세로 한 줄(384x768). 게임이 적 → 회오리 가운데로 늘려 쓴다.
[저장] Projects/AVSR/_exchange/in/part_sl_tendril.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
