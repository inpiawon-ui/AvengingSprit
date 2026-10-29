$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「창 안 부품 — 2차」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 창 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_F_choice.png · F_death.png · F_syndetail.png. 앵커 크롭(3배): Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_*.png — **앵커의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라. 이미 받은 부품 Projects/AVSR/_exchange/in/parts_partyui_popup.png 와 같은 화풍이어야 한다.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자 · 얼굴 · 아이콘을 그림에 넣지 마라**(모두 게임이 넣는다). 칸 안은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆 이름 메모 금지. 크기 1024 x 1024 시트.
[이 장 — 못 고름 줄 · 자물쇠] 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_death_rows.png 의 C 줄(맨 아래)
1) UI-PARTY-10c party_row_locked — 이미 받은 in/parts_partyui_popup.png 의 회색 「못 고름」 줄과 똑같되 **오른쪽 끝 자물쇠를 뺀** 줄(회색 테 · 어두운 남색 안). 가로 긴 줄 약 600 x 80 비율, 9분할
2) UI-PARTY-29 party_lock — 그 자물쇠만 따로(은색 작은 자물쇠, 약 32 x 36). 시안에서는 사슬 왼쪽 끝 · 줄 아래 테 위에 걸쳐 있다
[저장] Projects/AVSR/_exchange/in/parts_partyui_popup7.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
