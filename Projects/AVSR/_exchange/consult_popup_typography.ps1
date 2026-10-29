$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 상의 문서만 쓴다. 한 번에 끝낸다.

[PD] 「텍스트 사이즈를 잘 맞춰서, 지금 폰트로 최대한 어울리게, 글자 안 삐져나가고 예쁘게 — 볼드나 사이즈. 매일 보면 글자들이 다 따로 이사 와서 새로 잡은 느낌이다. 연출도 중요하지만 텍스트도 중요하다」.

[지금 상태 — 실측]
- 팝업 4종 캡처: Projects/AVSR/_exchange/ref/popup_c_ingame/levelup.png · devil.png · audit_altar_Korean.png · audit_shop_Korean.png (한국어) ·
  audit_{levelup|altar|devil|shop}_Japanese.png · _English.png (있는 것)
- 팝업 글자 칸 목록(칸 크기 · 지금 최대/최소 · 정렬): Projects/AVSR/_exchange/popup_text_boxes.md
- 팝업 글자 설정: 본문은 전부 **NotoSansKR SDF(보통 굵기) + 가짜 굵게(fontStyle Bold) + 「Soft」 재질 + 자동 줄임(최소~최대)**.
  제목은 제각각 — 레벨업 · 회복 제단 · 악마는 OriginalPixel(영문 픽셀 폰트, 한글은 대체 폰트로 떨어져 자간이 벌어짐), 상점만 NotoSansKR.
- 로비(이미 PD 통과)는 다르다: **NotoSansKR-Bold SDF(진짜 굵은 폰트) + HeavyText 컴포넌트(획 두께 dilate · 외곽선 폭 · 외곽선 색) + 고정 크기**
  (자동 줄임 최소=최대). 예: 카드 이름 21 · 섹션 제목 25~26 · 모드 제목 32(dilate .449 · 외곽선 .2) · 작은 값 16~19. 로비 화면: Projects/AVSR/_exchange/ref/popup_ref_lobby.png
- 언어: 한국어 · 일본어(기본) · 영어. HeavyText 칸은 언어가 바뀌면 그 언어의 굵은 폰트로 자동 교체(일본어 굵은 폰트 NotoSansJP-Bold SDF 있음).
- 쓸 수 있는 재질 프리셋(본문 폰트용): Soft · Outline · Shadow · Label · Title · GlowGold · GlowBlue · GlowPurple.

[정해 줄 것 — 팝업 4종 + 앞으로 클리어 창까지 같은 규칙]
1) 글자 역할 표(역할 6~8개: 창 제목 · 안내 한 줄 · 항목 이름 · 항목 설명 · 값/가격 · 칩/등급 · 주 버튼 · 보조 버튼 등) — 역할마다:
   폰트(굵은/보통, 픽셀 폰트를 쓸지) · 크기(고정값, 자동 줄임을 허용하면 최소값) · HeavyText dilate · 외곽선 폭/색 · 글자색 · 자간 · 줄 간격 · 정렬
   — 로비와 한 집안으로 보이게, 이 게임의 원혼 회로 틀(어두운 흑철 + 대표 색 빛)에 어울리게.
2) popup_text_boxes.md 의 노드마다 역할 지정 + 칸 크기가 모자라면 고칠 크기(일본어 · 영어 길이 포함, 넘치지 않게).
3) 칸에 다 안 들어가는 긴 글(설명 · 이벤트 본문)은 어떻게 할지(두 줄 허용 · 줄임 하한 · 말줄임 금지 등) 규칙.
4) 지금 캡처에서 「따로 논다」고 보이는 곳 목록(구체적으로).
[저장] Projects/AVSR/_exchange/consult_popup_typography.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
