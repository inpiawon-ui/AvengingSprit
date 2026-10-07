$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 스킬 연출 부품 시트 재발주 — 샐러맨더 「독 안개」(vncloud) 한 줄만.
정답 시안: Projects/AVSR/_exchange/in/mock_skill_salamander_v1.png (PD 통과) 의 2 최대 칸 독 구름을 그대로 따라 그린다.
지난번 납품(Projects/AVSR/_exchange/in/skill_parts_salamander.png 위 줄)은 가운데가 뻥 뚫린 진한 도넛 고리라 반려됐다.

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄.
- 위 줄 4칸: 몸 중심을 감싸는 **반투명 독 안개** — 가운데가 비지 않고 가운데부터 바깥까지 이어진 몽글몽글한 도트 구름 덩어리.
  초록(주) · 보라(부) 도트, 옅고 성기게 그려 안개 너머가 비쳐 보이게(진하게 칠한 면 금지). 위에서 내려다본 둥근 덩어리.
  1 작게 모임(지름 약 110 px) → 2 최대(지름 약 200 px) → 3 성기게 흩어짐(약 210 px, 구멍 숭숭) → 4 작은 잔류 점 몇 개만.
- 아래 줄 4칸: 전부 마젠타로 비워 둔다.
- 캐릭터 · 적은 그리지 않는다. 그림자 · 마젠타/분홍 색 금지 · 별 · 표창 · 십자 반짝이 모양 금지. 칸 밖으로 넘치지 않게.
[저장] Projects/AVSR/_exchange/in/skill_parts_salamander_v2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
