$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
[바탕] Projects/AVSR/_exchange/in/mock_hostinfo_v3.png — 네가 그린 시안. 화풍 · 구성은 **그대로** 두고 아래만 고친다.
PD 말: 「제일 왼쪽에는 물음표 버튼을 두라고」 — 지금 시안에는 호스트 카드 줄 맨 왼쪽에 「?」 카드가 없다.
고칠 것(두 화면 모두):
- 호스트 카드 줄 **맨 왼쪽 첫 칸을 「?」 카드**로 만든다 — 다른 카드와 같은 크기 · 같은 테두리, 안에 주사위 + 큰 「?」 도트 그림, 이름 자리에 「랜덤」, 별 · 가격 자리는 비우거나 「?」.
  그 오른쪽으로 고스트 · 갱스터 · 좀비 · 아마존 순으로 한 칸씩 밀린다(줄에 5칸이 보이면 호퍼는 줄 밖으로 나가 스크롤로 본다).
- 왼쪽 화면: **「?」 카드가 금색 테두리로 골라진 상태**, 아래 칸은 지금처럼 랜덤 선택 설명(주사위 · 호스트 실루엣 그림 그대로).
- 오른쪽 화면: 「?」 카드는 보통 테두리, 아마존이 금색으로 골라진 상태, 아래 칸은 지금처럼 아마존 정보.
[저장] Projects/AVSR/_exchange/in/mock_hostinfo_v4.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
