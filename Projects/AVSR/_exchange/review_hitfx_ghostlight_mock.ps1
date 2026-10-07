$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[배경] 네 설계서 Projects/AVSR/_exchange/spec_hitfx_ghostlight.md 대로 시안 두 장이 나왔다. PD에게 보이기 전 검수다.
  - A 피격: Projects/AVSR/_exchange/in/mock_hitfx.png
  - B 유령 빛: Projects/AVSR/_exchange/in/mock_ghostlight.png
  - 지금 게임 화면(비교): Projects/AVSR/_exchange/affinity_stock_ingame.png
PD 지시: 「연출을 이미지 하나로 넣는 건 없어 보이고 밋밋하다」 · 「빛이 내려오는 느낌」 · 「피격이 캐릭터 · 투사체에 맞게(표창에 얼음 금지)」.

[답할 것] 한국어. 시안마다:
1) 판정: PD에게 보일 만함 / 다시 그릴 것
2) 설계서와 어긋난 곳 · 몸을 덮는 곳 · 밋밋한 곳(있으면 숫자로)
3) 부품으로 쪼갤 때 시안에서 그대로 가져갈 점 3개(색 · 모양 · 크기)
4) 「다시 그릴 것」이면 고칠 발주 문장
[저장] Projects/AVSR/_exchange/review_hitfx_ghostlight_mock.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
