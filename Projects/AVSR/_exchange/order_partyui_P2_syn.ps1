$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「부품 표」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 인게임 UI 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_S1b.png 와 in/mock_partyui_F_*.png. 앵커 크롭: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/ — **이 시안의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 그림에 넣지 마라**(모든 글자는 게임이 넣는다). 칸 안(얼굴 · 막대가 들어갈 자리)은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆에 이름 메모도 넣지 마라.
[이 장 — 시너지 부품] 크기 1024 x 1024 시트.
1) UI-PARTY-06 시너지 칸 바탕 세 개 — 켜짐(금 테) · 꺼짐(어두운 테) · 미리보기(흐린 점선 테). 세로로 조금 긴 작은 칸(약 56 x 64 비율, 2배로 그려도 됨), 안은 비움
2) UI-PARTY-07 육각 문장 아홉 개(3 x 3 줄) — 각각 육각 테 안에 상징 하나: 파워(주먹 · 빨강) · 무기(칼과 권총 · 금색) · 마법(별빛 구슬 · 파랑) · 갱단(중절모 · 보라) · 코만도 부대(군 철모 · 국방색) · 무도가(주먹을 낀 팔 · 주황) · 괴물(용 비늘 발톱 · 초록) · 마도(마법진 · 청보라) · 균형(세 색이 나뉜 원 · 흰색). 작게 써도 알아보이게 굵고 단순하게. 시안의 무도가 · 무기 문장 화풍을 그대로
[저장] Projects/AVSR/_exchange/in/parts_partyui_syn.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
