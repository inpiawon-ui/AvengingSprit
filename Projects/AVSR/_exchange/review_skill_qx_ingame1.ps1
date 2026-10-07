$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다.
스킬 연출 퀄업 13종을 게임에 넣었다. PD 가 「코덱스랑 Claude 가 보기에 이상한 것 있으면 다 고쳐 적용해 달라」고 했다. 이상한 점을 전부 찾아라.

[정답 시안] Projects/AVSR/_exchange/in/mock_skill_{몸}_v1.png (1 발동 · 2 최대 · 3 끝, PD 통과)
  몸: commando_missile · guru · ninja_chain · robot · baseball · salamander · dragoon · dragon_blue · white_wizard · medium · snowwoman · vampire · death
  (수류탄 융단 폭격은 in/mock_skill_grenade_v1.png — PD 가 궤적 안내 · 여운을 빼라고 해서 일부러 뺐다)
[게임 캡처] Projects/AVSR/_exchange/ref/skill_qx_check/{몸}.png (720x1280 전체 화면, 시전 직후 약 0.1~0.3초 한 순간) · 묶음 sheet.png
  ※ 캡처 뒤 이미 고친 것: 드라군 불바다 크기(장판 지름으로 줄임), 구루 결계 0.8배, 설녀 얼음 결정 진하기 0.4(PD 「너무 가린다」).
  ※ 테스트 판이라 적이 1~2명뿐인 칸이 있다 · 화면의 큰 노란 구슬 줄은 화이트 위저드 평타 탄이다(스킬 연출 아님) · 옅은 큰 원은 근접 사거리 표시(별개).
[구현 코드] Assets/Scripts/Module/InGame/BattleDirector.SkillFxQuality.cs (크기 scale · 프레임 시간 · 위치 · 수명), 부품 그림 Assets/BaseResource/InGameMainUI/fx_{부품}_{n}.png
  부품 이름: gdring gddome gdrim / cbchain cbsnap cbwrap cbhold / rfring rfswing / vncloud vngather vndrip / ffbloom ffburn /
            dsbolt dscoil dsspark / wfgather wfpop / mgrune mgrise / isgather isprison isbreak / vmcircle vmorb vmglow / rpaura rpslash rpskull
  부품 칸 256 px = 화면 256 px 가 기준 크기(scale 1).

PD 기준: 시안과 최대한 똑같이(더 예쁜 건 괜찮다) · 화면 전체 물들이기 금지 · 원작 자세 그대로 · 궤적 안내 없음 · 여운 짧게 · 너무 크지 않게 · 캐릭터를 가리지 않게 · 별 · 표창 모양 금지.

[써 낼 것] 한국어 표, 스킬마다:
1) 시안 대비 어긋난 점(크기 · 위치 · 색 · 타이밍 · 가림 · 빠진 박자) — 캡처를 재서, 문제 없으면 「통과」
2) 고칠 값 — BattleDirector.SkillFxQuality.cs 기준으로 구체적으로(예: QxGuardOpen 의 GuardScale 0.8 → 0.65, vncloud scale 1.4 → 1.1, 위치 발밑 → 몸 중심, 프레임 0.1 → 0.07)
   그림 자체가 시안과 달라 다시 그려야 하면 「재발주」와 이유
3) 우선순위(높음 · 중간 · 낮음)
마지막에 Claude 가 바로 적용할 수 있게 변경 목록을 한 번에 정리해라.
[저장] Projects/AVSR/_exchange/review_skill_qx_ingame1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
