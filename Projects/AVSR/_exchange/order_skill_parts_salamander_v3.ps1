$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임에 바로 넣을 스킬 연출 부품 시트 재발주 — 샐러맨더 「독 안개」(vncloud) 한 줄만.
정답 시안: Projects/AVSR/_exchange/in/mock_skill_salamander_v1.png (PD 통과) 의 2 최대 칸 독 구름을 그대로 따라 그린다.
색 앵커: Projects/AVSR/_exchange/in/mock_salamander_cloud_crop.png (시안 구름만 잘라 둔 것) — 이 색을 그대로 쓴다.
지난번 v2(Projects/AVSR/_exchange/in/skill_parts_salamander_v2.png)는 모양은 좋았으나 회색빛 분홍 연무라 색이 틀려 반려. 모양 · 크기 · 4단계는 v2 그대로, 색만 시안대로.

시트: 1024 x 512, 순수 마젠타(#FF00FF) 배경, 칸 256 x 256 이 4열 x 2줄.
- 위 줄 4칸: 몸 중심을 감싸는 **반투명 독 안개** — 가운데가 비지 않고 가운데부터 바깥까지 이어진 몽글몽글한 도트 구름 덩어리.
  색은 시안처럼 채도 높은 독 초록(#5BD12E 계열) 덩어리와 짙은 보라(#4A2A7A~#7A3AB0 계열) 덩어리가 섞이고 작은 분홍·하늘 점 몇 개. 흰색 · 회색 · 분홍빛 연무 금지(마젠타 배경과 섞여 보인다). 덩어리 사이는 성기게 비워 안개 너머가 비쳐 보이게. 위에서 내려다본 둥근 덩어리.
  1 작게 모임(지름 약 110 px) → 2 최대(지름 약 200 px) → 3 성기게 흩어짐(약 210 px, 구멍 숭숭) → 4 작은 잔류 점 몇 개만.
- 아래 줄 4칸: 전부 마젠타로 비워 둔다.
- 캐릭터 · 적은 그리지 않는다. 그림자 · 마젠타/분홍 색 금지 · 별 · 표창 · 십자 반짝이 모양 금지. 칸 밖으로 넘치지 않게.
[저장] Projects/AVSR/_exchange/in/skill_parts_salamander_v3.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
