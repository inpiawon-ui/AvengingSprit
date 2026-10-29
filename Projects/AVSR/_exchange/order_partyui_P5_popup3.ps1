$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「창 안 부품 — 2차」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 창 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_F_choice.png · F_death.png · F_syndetail.png. 앵커 크롭(3배): Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_*.png — **앵커의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라. 이미 받은 부품 Projects/AVSR/_exchange/in/parts_partyui_popup.png 와 같은 화풍이어야 한다.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자 · 얼굴 · 아이콘을 그림에 넣지 마라**(모두 게임이 넣는다). 칸 안은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆 이름 메모 금지. 크기 1024 x 1024 시트.
[이 장 — 시너지 설명 말풍선 부품] 위에서 아래, 왼쪽에서 오른쪽 순서로 배치한다.
1) UI-PARTY-20 말풍선 판 — p4_syn_popover 의 남색 판 + 청록 테(9분할로 늘려 쓰게 가운데 고르게, 꼬리 빼고). 약 440 x 340 비율
2) UI-PARTY-20 꼬리 — 같은 판의 왼쪽 위로 튀어나온 작은 꼬리만 따로(왼쪽을 가리킨다). 약 24 x 40
3) UI-PARTY-21 단계 줄 두 개 — 켜짐(금 테 + 호박색 안) · 꺼짐(남색 안 + 회청 테), p4_syn_tiers(글자 빼고). 약 400 x 40 비율
4) UI-PARTY-22 「1명 더」 칩 — p4_syn_tiers 오른쪽 회청 테 작은 칩(글자 빼고). 약 80 x 34
5) UI-PARTY-23 몸 칸 세 개 — 금 테 · 보라 테 · 회색 테 정사각 칸(p4_syn_members, 얼굴 빼고 칸만). 약 96 x 96
6) UI-PARTY-24 칸 딱지 — p4_syn_members 의 「M」 · 「1」 금빛 작은 네모 딱지(글자 빼고). 약 28 x 28
7) UI-PARTY-25 가로선 — p4_syn_popover 아래 규칙 줄 위의 가는 가로선. 약 400 x 6
[저장] Projects/AVSR/_exchange/in/parts_partyui_popup3.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
