$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
[무대] 정유소 — 바닥 물건 **4개**를 한 장에: ① 가시판 ② 가시판(조금 다른 배열) ③ 낮은 울타리 ④ 낮은 울타리(조금 다른 모양).
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/bg_apply/refinery/obj_refinery_block_1.png ~ _3.png (이 무대의 새 장애물 블록 — 같은 화풍 · 도트 굵기 · 명암으로) · Projects/AVSR/_exchange/in/mock_simple2_ch6.png (PD 승인 시안)
[쓰임 정답] Assets/BaseResource/InGameMainUI/obj_refinery_spike_1.png · obj_refinery_fence_1.png (지금 것 — 크기 비율과 쓰임만 본다. 화풍은 버린다)
- 가시판: 바닥에 납작하게 깔린 정사각 판(위에서 내려다봄), 밟으면 아프다는 게 한눈에 — 판 위에 짧고 굵은 가시 3x3 줄. 판 테두리에 노랑-검정 경고 줄무늬 정도. 정사각 칸을 꽉 채운다.
- 낮은 울타리: 칸 하나 폭의 낮은 철책(몸은 막고 탄은 넘어간다) — 블록보다 키가 낮고 사이가 비쳐 보이는 막대 · 철망. 가로 1 : 세로 1.4 칸 안.
- 바탕은 **완전한 분홍(#FF00FF)**. 넷을 **한 줄로** 넉넉히(물건 폭의 절반 이상) 띄운다. 물건 하나 약 200 px 폭.
- 그림자 판 · 바닥 조각 금지.
- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
[저장] Projects/AVSR/_exchange/in/bg_refinery_hazards.png (PNG)
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
