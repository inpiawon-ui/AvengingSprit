$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 화이트 위저드 액티브 「끌어당기는 빛의 원」 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] Projects/AVSR/_exchange/ref/rework1021/base_white_wizard.png — 지금 게임 화면(화이트 위저드 = 분홍 로브 마법사, 가운데 아래). 창 · 칸 · 글자 · 캐릭터 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
PD 말: 「던파 아크메이지 나트 할로우 같은 똥그란 원을 소환해서 거기에 몬스터를 모이는 걸로」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마」.
[장면] mock_wizard_gravity — 네가 아트 디렉터라면 어떻게 꾸미겠나. 한 장 안에 박자 셋을 나란히(세로 3칸 또는 한 화면에 겹쳐도 됨):
  ① 소환: 적이 많은 곳 바닥에 흰 · 금빛 마법진 원(지름 약 캐릭터 4배)이 펼쳐지고 가운데에 빛의 구체가 맺힌다
  ② 끌어당김(머묾 2초): 둘레 해골들이 소용돌이 빛줄기에 끌려 가운데로 빨려 든다 — 빛 알갱이가 안쪽으로 흐른다
  ③ 마무리(가장 셀 때): 모인 자리에서 빛이 한 번 크게 터진다
화이트 위저드는 흰 · 금빛 빛 마법(평타가 금빛 구슬). 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변 바닥) + 안쪽으로 흐르는 빛 알갱이를 함께.
- 크기는 바탕과 같은 540x960 한 칸 기준(세 칸이면 1620x960). 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_wizard_gravity.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
