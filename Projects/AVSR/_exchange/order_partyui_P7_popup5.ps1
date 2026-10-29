$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「창 안 부품 — 2차」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 창 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_F_choice.png · F_death.png · F_syndetail.png. 앵커 크롭(3배): Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_*.png — **앵커의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라. 이미 받은 부품 Projects/AVSR/_exchange/in/parts_partyui_popup.png 와 같은 화풍이어야 한다.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자 · 얼굴 · 아이콘을 그림에 넣지 마라**(모두 게임이 넣는다). 칸 안은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆 이름 메모 금지. 크기 1024 x 1024 시트.
[이 장 — 빙의 가능 표시]
정답 시안: Projects/AVSR/_exchange/in/mock_partyui_S1b.png (PD 확정 틀). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_possess_bubble.png
1) UI-PARTY-27 possess_pill — 적 머리 위 보라빛 둥근 알약 꼬리표(왼쪽에 작은 유령 얼굴 포함, 「빙의 가능」 글자 자리는 비움, 아래 가운데 작은 꼬리). 약 140 x 44 비율. 9분할로 가로를 늘릴 수 있게
2) UI-PARTY-28 possess_bubble_light — 그 옆 밝은 회백색 말풍선(어두운 테 1px, 왼쪽 아래로 꼬리 — 앵커 그대로). 글자 자리 비움. 약 170 x 60 비율. 9분할
[저장] Projects/AVSR/_exchange/in/parts_partyui_popup5.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
