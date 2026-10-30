$env:OPENAI_API_KEY = $null
Set-Location "C:/won/UnityProject/AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 글자 상자를 그려 때우지 마라(PIL 합성 금지). 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 720 x 1280) — **완성 화면 시안** 한 장.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(빛은 도트 모양 + 셰이더 광원 · 파티클, 강약, 연출 원칙).
화풍 · 퀄 기준: Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png (통과한 필살기 시안 — 이 정도의 퀄 · 도트 질감).
[주제] 버프 스킬이 뜰 때 「무슨 효과인지」를 **아이콘으로 예쁘게** 알리는 표시. PD 원문: 「다른 스킬은 데미지가 보이는데 이건 버프라 어떤 효과인지 표현하면 좋겠다. 아이콘화로 이쁘게 — 텍스트로 표기하기가 좀 그래서」.
방향: 게임 화면 위 버프 알림을 **예쁜 도트 연출**로 — 보라 혼 · 영매 분위기. **아이콘이 주인공**, 숫자(+25% 같은)는 작게라도 **꼭 읽히게**. 글자는 최소(스킬 이름 정도, 5언어라 긴 글자에 기대지 않는다).
**피할 것**: 파란 정보 판 · 밋밋한 네모 상자에 아이콘과 숫자를 늘어놓는 꼴(이미 반려된 꼴이다). 상자를 쓰더라도 도트 장식 · 혼불 · 문양으로 영매다운 모양이어야 한다.
능력치 아이콘은 성장 화면과 같은 뜻으로 읽혀야 한다 — 게임 아이콘(모양을 그대로 따르거나 같은 모양을 도트로 다듬어 쓴다):
  Assets/BaseResource/ReBornKit/Icons/rb_icon_stat_power_96.png (쌍칼 = 위력/피해)
  Assets/BaseResource/ReBornKit/Icons/rb_icon_stat_attackspeed_96.png (화살표 = 공격 속도)
  Assets/BaseResource/ReBornKit/Icons/rb_icon_stat_crit_96.png (조준점 = 치명 확률)
  Assets/BaseResource/ReBornKit/Icons/rb_icon_stat_critdamage_96.png (별 = 치명 피해)
조건: 게임 정보(위 HUD · 체력바 · 피해 숫자 · 적)를 크게 가리지 않는다. 720 x 1280 화면에서 읽히는 크기(아이콘 한 변 화면 기준 대략 36~56 px). 위 HUD · 아래 조작 단추 · 화면 구성은 바탕 그대로 둔다.
인물은 바탕 화면에 있는 그대로(메인 = 영매 초록 망토, 보조 둘 = 구루 · 화이트 위저드 반투명 보랏빛). 새로 디자인하지 않는다.
화면 글자는 한국어. 그림 옆 여백(화면 밖이 아니라 시안 그림 아래쪽 바닥 빈 곳)에 한국어 메모 3줄 이내로 「어디에 · 어떤 모양 · 언제 뜨고 얼마나 남는지」를 적는다.
[바탕] Projects/AVSR/_exchange/mock1030_buffbadge/now_badge_infuse.png — 지금 인게임 화면(버프 효과 표시 없음, 연출만). 이 위에 그린다.
[장면] 액티브 「혼 깃들이기」(8초) — 파티 전원(메인 + 보조 둘) **피해 +25%(쌍칼)** · **치명 확률 +15%(조준점)**. 시전 직후 0.3초쯤의 화면.
[안 1] 네가 가장 예쁘다고 생각하는 방식으로 자유롭게. 참고 예시(따라 하지 않아도 된다): 아이콘이 몸에서 혼불처럼 솟아올라 머리 위에 잠깐 떠 있다가 작은 표시로 줄어든다.
세 안이 서로 다른 안이 되도록 한다(1 · 2 · 3안은 각각 따로 발주된다). 걸려 있는 동안 남는 작은 표시를 둘지도 네가 정하고 메모에 적는다.
[저장] Projects/AVSR/_exchange/mock1030_buffbadge/mock_badge_infuse_1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
