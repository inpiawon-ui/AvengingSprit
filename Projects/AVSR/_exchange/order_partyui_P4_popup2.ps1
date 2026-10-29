$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/guide_party_ui.md 를 끝까지 읽는다(특히 맨 아래 「창 안 부품 — 2차」).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 확정한 「빙의 파티」 창 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다.
[정답 시안] Projects/AVSR/_exchange/in/mock_partyui_F_choice.png · F_death.png · F_syndetail.png. 앵커 크롭(3배): Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p4_*.png — **앵커의 화풍 · 색 · 테 두께 · 도트 크기를 그대로** 따른다. 새로 디자인하지 마라. 이미 받은 부품 Projects/AVSR/_exchange/in/parts_partyui_popup.png 와 같은 화풍이어야 한다.
[공통] 바탕은 **완전한 분홍(#FF00FF)**. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자 · 얼굴 · 아이콘을 그림에 넣지 마라**(모두 게임이 넣는다). 칸 안은 비운다. 빛 번짐을 굽지 마라. 그림자 판 금지. 부품 옆 이름 메모 금지. 크기 1024 x 1024 시트.
[이 장 — 선택창 · 사망창 안 부품] 위에서 아래, 왼쪽에서 오른쪽 순서로 배치한다.
1) UI-PARTY-12 태그 칩 두 개 — 금 테 칩 · 붉은 테 칩(p4_choice_tags 의 「무기」 · 「갱단」 칩, 글자 빼고). 약 120 x 40 비율
2) UI-PARTY-13 정사각 칸 두 개 — 청록 테 칸(p4_choice_skillbox) · 회색 테 칸(p4_death_rows 의 C 줄 얼굴 칸). 약 96 x 96
3) UI-PARTY-14 남은 HP 칸 — p4_choice_hppill 의 어두운 남색 둥근 칸(글자 빼고). 약 220 x 44 비율
4) UI-PARTY-15 추천 딱지 — p4_choice_badge 의 금빛 둥근 딱지(글자 빼고). 약 100 x 40 비율
5) UI-PARTY-16 세로 구분선 — p4_choice_header 의 이름과 「보조 자동 스킬」 사이 가는 청록 세로선. 약 6 x 120
6) UI-PARTY-17 피 튄 판 — p4_death_title 의 「메인 사망」 뒤 붉은 피 얼룩(글자 빼고, 얼룩만). 약 300 x 120 비율
7) UI-PARTY-18 사슬 — p4_death_chain 의 못 고름 줄 오른쪽 위 · 아래를 감는 사슬(자물쇠 빼고, 줄 판 빼고 사슬만). 약 240 x 100 비율
8) UI-PARTY-19 추천 육각 꼬리표 — p4_death_rows 의 B 줄 왼쪽 금빛 육각(왕관만, 「추천」 글자 자리는 비움). 약 64 x 80
[저장] Projects/AVSR/_exchange/in/parts_partyui_popup2.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
