$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 시즌 패스 레벨 줄 **부품 5개를 한 장에**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pass_track.png — PD 가 고른 시안(가운데 레벨 줄). 화풍 그대로.
[앵커] Projects/AVSR/_exchange/ref/pass_mock/crop_track.png — 시안에서 잘라 2배로 키운 레벨 줄. 마디 모양 · 테 굵기 · 색 · 빛 번짐을 그대로 따른다.
[부품] 한 장 1280 × 256, 다섯 칸(각 256 × 256)에 하나씩, 칸 가운데:
 1) rb_pass_node_now    — 금빛 팔각 마디(현재 레벨): 지름 200 정도, 도트 테 + 은은한 금빛 번짐, 속은 짙은 남색. **숫자는 그리지 않는다**(비워 둔다)
 2) rb_pass_node_on     — 하늘색 팔각 마디: 같은 모양 · 같은 크기, 하늘색 테 + 은은한 하늘색 번짐
 3) rb_pass_node_off    — 회색 팔각 마디: 같은 모양 · 같은 크기, 흐린 회청색 테, 번짐 없음
 4) rb_pass_line_on     — 하늘색 세로 줄 한 토막: 폭 14 · 높이 220, 끝이 둥근 막대(앵커의 줄 그대로)
 5) rb_pass_line_off    — 회청색 세로 줄 한 토막: 같은 크기
- 마디 안 · 줄 위에 글자를 그리지 않는다. 그림자 판 없음.
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 그림에 분홍이 섞이지 않게(빛 번짐도 분홍 위에 그리지 말고 판 안에서 끝낸다).
[저장] Projects/AVSR/_exchange/in/rb_pass_track_parts.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
