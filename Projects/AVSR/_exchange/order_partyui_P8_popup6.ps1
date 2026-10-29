$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「창 안 부품 — 2차」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 창 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_F_choice.png · F_death.png · F_syndetail.png. 앵커 크롭(3배): Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_*.png — **앵커의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라. 이미 받은 부품 Projects/AVSR/_exchange/in/parts_partyui_popup.png 와 같은 화풍이어야 한다.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자 · 얼굴 · 아이콘을 그림에 넣지 마라**(모두 게임이 넣는다). 칸 안은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆 이름 메모 금지. 크기 1024 x 1024 시트.
[이 장 — 다시 받는 부품 셋] 이미 받은 in/parts_partyui_popup2.png 에서 시안과 달랐던 것만 다시 그린다. 시안을 더 정확히 따른다.
1) UI-PARTY-14 party_hp_pill — 앵커 p4_choice_hppill: 「남은 유령 HP 35」 뒤 칸. **밝은 테두리가 없다**(지난번 청록 테두리는 틀림). 줄 판보다 조금 더 어두운 남색으로 채운 둥근 모서리 칸, 테는 아주 어두운 남색 1px 정도. 약 220 x 44 비율. 9분할
2) UI-PARTY-15 party_badge_best — 앵커 p4_choice_badge: 「추천」 딱지. **진한 금노랑으로 꽉 찬** 둥근 딱지(지난번 크림색 안은 틀림) + 어두운 갈색 테. 글자 자리 비움. 약 100 x 40 비율
3) UI-PARTY-18 party_chain — 앵커 p4_death_chain · p4_death_rows 의 C 줄: 못 고름 줄 **오른쪽 위 모서리에서 시작해 오른쪽 끝을 감고 아래 변을 따라 왼쪽으로** 늘어진 사슬(ㄱ 을 뒤집은 모양, 줄 판 · 자물쇠 빼고 사슬만). 약 240 x 100 비율. 지난번 U 자 모양은 틀림
[저장] Projects/AVSR/_exchange/in/parts_partyui_popup6.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
