$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「부품 표」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 인게임 UI 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_S1b.png 와 in/mock_partyui_F_*.png. 앵커 크롭: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/ — **이 시안의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 그림에 넣지 마라**(모든 글자는 게임이 넣는다). 칸 안(얼굴 · 막대가 들어갈 자리)은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆에 이름 메모도 넣지 마라.
[이 장 — 위쪽 HUD 부품] 크기 1024 x 1024 시트. 다음을 왼쪽 위부터 차례로:
1) UI-PARTY-01 위쪽 판 — 가로로 긴 판(약 720 x 150 을 그대로, 9분할로 늘려 쓸 수 있게 가운데는 고른 무늬)
2) UI-PARTY-02 메인 칸 테 — 금 테 + 오른쪽 위 작은 왕관, 안은 비움(약 104 x 84 비율, 크게 2배로 그려도 됨)
3) UI-PARTY-03 보조 썸네일 테 — 보라 테 정사각, 안은 비움
4) UI-PARTY-04 빈 자리 칸 — 점선 테 + 가운데 「+」(글자 −10 은 넣지 마라)
5) UI-PARTY-05 막대 세 줄 — 막대 바탕(어두운 홈) · 유령 HP 채움(청록) · 메인 체력 채움(초록). 각각 가로 긴 막대
[저장] Projects/AVSR/_exchange/in/parts_partyui_hud.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
