$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[목표] 네가 정한 글자 규칙(Projects/AVSR/_exchange/consult_popup_typography.md)을 팝업 4종에 적용한 미리보기 검수.
PD: 「텍스트 사이즈 잘 맞춰서, 지금 폰트로 최대한 어울리게, 안 삐져나가고 예쁘게 — 볼드 · 사이즈. 글자들이 다 따로 이사 와서 새로 잡은 느낌」.
[미리보기] (실제 굵은/보통 폰트 · 역할 크기 · 묶음 같은 크기 · 외곽선으로 그린 것)
- 한국어: Projects/AVSR/_exchange/ref/popup_c_type_ko.png (왼쪽부터 레벨업 · 회복 제단 · 악마 · 상점) · 각 장 popup_c_type_ko_{BuffChoicePanel|ShrinePanel|EventPanel|ShopPanel}.png
- 일본어: popup_c_type_ja.png · popup_c_type_ja_*.png
- 값: Projects/AVSR/Tools/popup_c_layout.py (ROLES 표 · node() 마다 칸 좌표 · 역할 · group)
[내가 본 것] 회복 제단 설명이 아이콘과 칸 테두리에 붙어 답답함 · 상점 설명이 작고 이름과 가격이 붙음 · 악마 본문이 위 테두리에 걸침 · 레벨업 제목이 픽셀 폰트가 아닌 고딕이 됨(장식 표식으로 픽셀 유지할지?).

[써 낼 것] 한국어:
1) 화면마다 삐져나감 · 테두리 닿음 · 겹침 · 따로 노는 곳(구체 노드)
2) popup_c_layout.py 에서 바꿀 값(노드 좌표 · 칸 크기 · 역할 · 색) 구체적으로, ROLES 표 수정이 필요하면 그것도
3) 레벨업 「LEVEL UP!」 제목: 픽셀 폰트 장식 유지 vs 굵은 고딕 — 하나 골라 이유
마지막에 Claude 가 바로 적용할 변경 목록.
[저장] Projects/AVSR/_exchange/review_popup_type1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
