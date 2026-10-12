$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
[바탕] Projects/AVSR/_exchange/in/mock_hostinfo_free.png — 네가 그린 시안. **이 그림 그대로** 두고 한 군데만 고친다.
고칠 것: 왼쪽 화면 「호스트 선택」 맨 왼쪽 ゴースト 카드 왼쪽 위의 파란 불꽃 배지를 지우고, 같은 자리 · 같은 크기로 **물음표 「?」 배지**를 넣는다
(다른 카드의 상성 무기 배지와 같은 작은 배지 모양, 금색 테두리 안에 또렷한 흰 「?」 도트 글자). PD 말: 「제일 왼쪽 선택지에 물음표를 추가해 줘」.
나머지(오른쪽 정보 창 포함)는 바꾸지 않는다.
[저장] Projects/AVSR/_exchange/in/mock_hostinfo_free_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
