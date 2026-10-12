$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트 · 세로 720x1280 모바일) — 로비 「호스트 선택」 칸 개편 **시안**(완성된 화면 한 장).
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(이 게임 화풍 · 빛 표현 원칙).
[바탕] Projects/AVSR/_exchange/ref/hostinfo_mock/base_lobby.png — 지금 로비 챕터 · 호스트 선택 화면. 화풍 · 창 테두리 · 글꼴 · 색을 그대로 따른다.
[참고] Projects/AVSR/_exchange/out/44_ui/AVSR_HostDetail_applied_preview.png — 예전 호스트 상세 카드(들어갈 정보의 종류만 참고, 모양은 지금 로비 화풍으로).
PD 말: 「제일 왼쪽 선택지에 물음표를 추가해 주고, 각각의 호스트를 클릭하면 호스트 정보를 보여 주는 식으로 변경해 줘」.

그릴 것 — 화면 한 장에 두 상태를 나란히(왼쪽 = ①, 오른쪽 = ②, 각각 720x1280 비율):
① 호스트 선택 칸 — 다른 카드들은 왼쪽 위에 상성 무기 아이콘(작은 배지)이 있는데 맨 왼쪽 「ゴースト(몸 없음)」 카드만 비어 있다.
   그 같은 자리 · 같은 크기 배지 모양으로 **「?」 배지**를 넣는다(유령은 상성이 정해지지 않았다는 뜻). 나머지 카드 · 랜덤 선택 · 도전하기 버튼은 그대로.
② 호스트 카드(예: 아마조네스)를 눌렀을 때 — 로비 위에 **호스트 정보 창**이 뜬다(뒤는 어둡게 깔림).
   창 안: 초상 · 이름 · 직업 배지 · 상성 아이콘 · 별(숙련도) · 능력치 3줄(HP · ATK · SPD 막대와 숫자) ·
   액티브 스킬(아이콘 · 이름 · 설명 2줄 · 쿨타임) · 패시브 스킬(아이콘 · 이름 · 설명 2줄) · 아래에 「이 호스트로 선택」 버튼과 닫기(X).
   글자는 일본어/한국어 어느 쪽이든 읽히게, 글자 칸은 글자가 넘치지 않게 넉넉히.
- 지금 로비와 같은 네온 테두리 · 금색 버튼 화풍. 빛 · 그림자를 과하게 넣지 않는다. 게임 정보(글자 · 숫자)를 가리지 않는다.
- 그림 아래 여백에 한국어 메모 3줄 이내로 「무엇이 · 어디에 · 누르면 어떻게」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/mock_hostinfo_free.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
