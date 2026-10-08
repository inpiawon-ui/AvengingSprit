$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 검수 문서만 쓴다. 한 번에 끝낸다.

[목표] 글자 규칙(Projects/AVSR/_exchange/consult_popup_typography.md, 2회차 통과 Projects/AVSR/_exchange/review_popup_type2.md)을 새로 들어온 **스테이지 클리어 창**에 적용한 미리보기 검수.
PD: 「텍스트 사이즈 잘 맞춰서, 지금 폰트로 최대한 어울리게, 안 삐져나가고 예쁘게 — 볼드 · 사이즈」 · 「타이틀이 뿌옇게 보인다」(→ 흰 글자 + 어두운 외곽선으로 고쳐 통과).
[미리보기] 한국어 Projects/AVSR/_exchange/ref/popup_c_type_ko_ChapterResultPopup.png · 일본어 popup_c_type_ja_ChapterResultPopup.png
[시안] PD 통과 연출 시안 Projects/AVSR/_exchange/in/mock_fxstory_clear_v2.png (글자 배치 · 굵기 · 색의 기준)
[값] Projects/AVSR/Tools/popup_c_layout.py — ChapterResultPopup 노드(ResultTitleText · ResultSubText · Gold/Chest 줄 · WarnText · OK) · ROLES 의 result_title · hero · ok_dark
[내가 본 것] 시안 제목은 금빛 굵은 글자인데 우리 규칙(제목 = 흰 글자 + 어두운 외곽선)과 다르다 — 어느 쪽으로 맞출지 판정. 일본어 「シルバー宝箱」 줄이 한국어보다 길다.
[써 낼 것] 한국어:
1) 삐져나감 · 테두리 닿음 · 겹침 · 따로 노는 곳(구체 노드), 한국어 · 일본어 둘 다
2) popup_c_layout.py 에서 바꿀 값(노드 좌표 · 칸 크기 · 역할 · 색) 구체적으로
3) 제목 색: 시안의 금빛 vs 규칙의 흰색 — 하나 골라 이유(다른 4창과 통일감 고려)
마지막에 Claude 가 바로 적용할 변경 목록. 남은 것이 없으면 맨 위에 「통과」.
[저장] Projects/AVSR/_exchange/review_popup_type3.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
