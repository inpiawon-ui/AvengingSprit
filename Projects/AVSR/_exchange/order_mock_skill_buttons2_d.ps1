$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 인게임 오른쪽 아래 **액티브 · 필살기 버튼 묶음 시안 2차**.
[바탕] Projects/AVSR/_exchange/ref/skill_buttons_mock/base_fight.png — 지금 게임 화면. 오른쪽 아래 두 버튼만 다시 그리고 나머지(조이스틱 · 위 HUD · 전투 바탕)는 그대로.
[1차 반려] Projects/AVSR/_exchange/ref/skill_buttons_mock/skill_buttons_compare.png 의 ②~⑤ — PD 「뭔가 투머치한 느낌」. 이어진 쇠 받침 · 날개 불꽃 장식 · 반짝이 · 큰 이름표 판을 **피한다.**
PD 말: 「ACTIVE · SPECIAL 같은 느낌의 텍스트도 넣을 수 있고, 약어로도 좋고, 다른 게임에도 별로 없겠지만 다른 식으로 더 접근이 필요할 것 같아」.
[접근 D — 게이지 칸 버튼] 버튼 아래 또는 옆에 짧은 가로 게이지 막대와 글자 — 액티브는 「ACT」 + 쿨이 차오르는 막대, 필살기는 「SPECIAL」 + 「READY」 표시. 버튼 원은 단순하게, 상태는 글자 · 막대로 읽힌다
[공통]
- **절제** — 버튼 두 개와 글자만으로 무엇인지 읽힌다. 장식은 최소.
- 필살기가 주 버튼(조금 더 크다), 액티브는 보조. 둘은 가까이 붙어 한 묶음으로 보이되 받침판으로 잇지 않는다.
- 버튼 속 아이콘은 바탕 캡처 속 지금 스킬 아이콘(도트) 그대로. 글자는 도트 픽셀 글꼴(영문 대문자), 작고 또렷하게.
- 한 장에 두 상태를 같이: 액티브는 **쿨 도는 중**(아이콘 원 전체가 어둡게 덮이고 시계 방향으로 걷히는 중, 가운데 남은 초 「5」), 필살기는 **준비됨**.
- 크기는 바탕과 같은 720 x 1280. 그림 옆 여백에 한국어 메모 2줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_skill_buttons2_d.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
