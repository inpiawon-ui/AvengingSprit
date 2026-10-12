$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그리거나 다른 그림을 잘라 붙여 때우지 마라 — 이미지 도구가 막히면 그리지 말고 「못 그렸다」고만 답하라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 와 Projects/AVSR/_exchange/guide_random_pick.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 로비 — 「?(랜덤 선택)」 카드와 아래 랜덤 설명 칸 **시안**.
[바탕] Projects/AVSR/_exchange/ref/hostinfo_mock/base_random_now.png — 지금 실제 게임 화면(720x1280). **「?」 카드(카드 줄 맨 왼쪽, 금색 테두리)와 바로 아래 「랜덤 선택」 칸 두 곳만** 다시 꾸민다. 나머지(다른 카드 · 챕터 칸 · 도전하기 버튼 · 아래 바)는 그대로.
PD 말: 「너무 구린데, 다른 방식으로 좀 이쁘게 · 캐릭터 리스트로 넣을 필요 없고 · 다른 게임 표현을 참고」. 지금 칸의 사신 실루엣 · 아래 캐릭터 실루엣 줄은 **없앤다**.
[방향] 네가 아트 디렉터라면 어떻게 꾸미겠나 — 아래 세 방식에 얽매이지 말고 가장 예쁜 안.
- 설명 칸은 지금 크기 그대로(카드 줄 아래 ~ 도전하기 버튼 바로 위). 글: 「ランダム選択 / 全ホストの中から1体がランダムで選ばれます。/ 低確率で伝説のホストが登場！」 — 글자는 또렷하게, 가리지 않는다.
- 원작 캐릭터를 새로 그리지 않는다.
그릴 것: 화면 두 장 나란히 — 왼쪽 = 머무는 장면(골라진 상태), 오른쪽 = 고르는 순간(가장 밝게 빛나는 박자). 각각 720x1280. 그림 아래 여백에 한국어 메모 3줄 이내(무엇이 어떻게 움직이나).
[저장] Projects/AVSR/_exchange/in/mock_random_free.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
