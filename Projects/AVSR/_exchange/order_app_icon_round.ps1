$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 **앱 아이콘 — 둥근 아이콘용으로 다시 배치**.
[원본] Projects/AVSR/_exchange/in/app_icon_1028.png — PD 가 고른 앱 아이콘(1254 정사각): 하늘빛 유령 + 「AVENGING SPIRIT」 로고 + 흐린 유령 무늬 바탕.
[문제] 안드로이드 둥근 아이콘으로 잘리면 로고 양 끝(S · M)과 아래가 잘린다 — 미리보기 C:\Users\inpia\AppData\Local\Temp\claude\C--won-UnityProject-AvengingSprit\579b460c-d741-45f1-9cb3-55cd877dbf21\scratchpad\icon_round.png
[할 일] 그림 내용(유령 · 로고)은 **모양 · 색 그대로** 두고 크기만 줄여, 전부 **가운데 원(지름 = 판의 80%) 안**에 들어가게 다시 배치한다. 둘레는 원본의 흐린 유령 무늬 바탕을 이어서 채운다.
- 유령과 로고의 비율 · 위아래 순서는 원본 그대로(유령 위 · 로고 아래). 새 그림 · 글자를 더하지 않는다.
- 결과 크기는 정확히 1024 × 1024 정사각.
[저장] Projects/AVSR/_exchange/in/app_icon_round_safe.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
