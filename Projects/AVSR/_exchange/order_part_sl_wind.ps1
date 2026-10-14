$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
⚠ 이미지 도구에 넘기는 참조 그림은 5장 이하(도구 한도). 도구가 크게 그려 오면 칸 격자대로 지정 크기로 **최근접 축소만** 해도 된다(새로 그리거나 잘라 붙이기 금지).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 탑다운) 슬러거 스킬 「풀스윙 회오리」 이펙트 **부품**. PD 가 고른 시안: Projects/AVSR/_exchange/in/mock_skill_baseball_v2.png (PD 「시안 퀄 정도는 나와야 한다 · 과하지 않으면서 이쁘게」).
[기준 그림 — 이 모양 · 색 · 화풍 그대로] Projects/AVSR/_exchange/ref/skillmock/anchor_slugger_vortex.png · anchor_slugger_vortex2.png (PD 가 고른 시안의 회오리 부분만 잘라 낸 것 — 이 회오리를 그대로)
⚠ 지난 납품(in/part_sl_vortex.png)은 PD 반려: 「회오리가 이상하다 · 시안이랑 다르다」. 둥글고 꽉 찬 은하 모양이었다. 이번엔 시안처럼:
 - 위에서 비스듬히 내려다본 **납작한 타원**(가로 : 세로 = 2 : 1)
 - **가는 바람 줄기 여러 가닥**(흰 · 하늘빛 반투명 호 6~8줄, 끝이 가늘어지는 붓 터치)이 소용돌이로 감기며 안쪽으로 빨려 든다 — 줄기 사이는 비어 바닥이 보인다
 - 가운데(몸 자리)는 **비운다** — 안쪽에 얇고 밝은 고리 하나만
 - 돌 · 캐릭터 · 적은 그리지 않는다(돌 파편은 게임에서 파티클로 얹는다)
공통: **검은 바탕(#000000)**(게임에서 더하기로 그린다) · 빛 가장자리는 검정으로 바로 사그라든다(바깥 어두운 판 금지) · 도트 결 · 글자 금지.
[만들 것] 위 회오리 6장 반복 — 장마다 바람 줄기가 조금씩 돌아 감긴다(시계 반대 방향). 한 장 512x256, 6장 세로 한 줄(512x1536).
[저장] Projects/AVSR/_exchange/in/part_sl_wind.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
