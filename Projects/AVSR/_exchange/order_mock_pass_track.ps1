$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 시즌 패스 화면 **가운데 「레벨」 줄 시안**.
[바탕] Projects/AVSR/_exchange/ref/ui_reborn_1023/play/pass_16x9.png — 지금 게임 화면(720 × 1280). 가운데 열(무료 칸과 프리미엄 칸 사이)의 레벨 줄만 다시 그리고 나머지는 그대로.
[문제] PD 「저 부분은 좀 다르게 표현해야 될 것 같은데, 버그 같다」 — 세로 줄이 레벨 숫자를 그대로 관통하고, 「현재」 노란 점이 줄 위에 떠 있어 고장 난 것처럼 보인다. 앵커: Projects/AVSR/_exchange/ref/pass_mock/ruler_rows.png 가운데 열.
[목표] 시즌 패스 게임에서 흔히 쓰는 「진행 트랙」 — 레벨 숫자는 줄 위의 **둥근 마디(배지)** 안에 들어가고, 줄은 마디와 마디 사이만 잇는다.
- 지나온 레벨(받을 수 있음 · 받음): 마디 테와 줄이 밝은 하늘색
- 현재 레벨: 마디가 노랑/금빛으로 강조(조금 더 크게), 마디 아래 작은 「현재」 글자
- 아직 안 온 레벨: 마디 · 줄이 흐린 회청색
- 화풍은 이 게임(남색 판 · 하늘색 · 금빛 포인트, 도트 숫자). 장식 과하지 않게.
- 크기는 바탕과 같은 720 × 1280. 그림 옆 여백에 한국어 메모 2줄 이내.
[저장] Projects/AVSR/_exchange/in/mock_pass_track.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
