$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「부품 표」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 인게임 UI 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_S1b.png 와 in/mock_partyui_F_*.png. 앵커 크롭: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/ — **이 시안의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 그림에 넣지 마라**(모든 글자는 게임이 넣는다). 칸 안(얼굴 · 막대가 들어갈 자리)은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆에 이름 메모도 넣지 마라.
[이 장 — 창 · 말풍선 부품] 크기 1024 x 1024 시트.
1) UI-PARTY-09 창 판 — 시안의 빙의 선택창 · 사망창 판(어두운 남색 + 청록 테 + 모서리 장식), 9분할로 늘려 쓸 수 있게 가운데 고르게. 약 640 x 420 비율
2) UI-PARTY-10 선택 줄 세 개 — 보통(청록 테) · 추천(금 테 + 왼쪽 작은 왕관 꼬리표, 「추천」 글자는 넣지 마라) · 못 고름(어둡게 + 오른쪽 작은 자물쇠). 가로 긴 줄(약 600 x 88 비율)
3) UI-PARTY-11 원형 시계 테 — 사망창의 5.2초 둥근 테(안은 비움)
4) UI-PARTY-08 말풍선 두 개 — 금빛(시너지 들어감, 꼬리 아래) · 회색(시너지 없음, 꼬리 아래). 가로 긴 작은 말풍선, 안은 비움
[저장] Projects/AVSR/_exchange/in/parts_partyui_popup.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
