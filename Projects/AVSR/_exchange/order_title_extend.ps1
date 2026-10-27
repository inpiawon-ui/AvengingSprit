$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 타이틀 화면 그림 **넓히기(바깥 이어 그리기)**.
[원본] Projects/AVSR/_exchange/in/title_key_1028.png — PD 가 준 타이틀 그림(941 × 1672, 도트 픽셀 화풍). 내용은 **한 점도 바꾸지 않는다.**
[바탕판] Projects/AVSR/_exchange/ref/title_extend_base.png — 1254 × 2092 판 가운데(왼쪽 156 · 위 210)에 원본을 그대로 얹었고 둘레는 분홍(#FF00FF)이다.
[할 일] 분홍 둘레만 원본과 이어지게 채운다 — 위쪽은 별이 있는 밤하늘(은하수 띠가 이어지게), 양옆은 구름 · 밤 도시 빌딩 · 다리 · 물 반사가 끊김 없이 이어지게, 아래는 어두운 지붕 · 건물. 같은 도트 크기 · 같은 색.
- 새 로고 · 글자 · 캐릭터 · 회사 로고를 더 그리지 않는다. 가운데 원본 부분은 그대로 둔다.
- 결과 크기는 정확히 1254 × 2092. 분홍이 남지 않게.
[저장] Projects/AVSR/_exchange/in/title_key_extended.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
