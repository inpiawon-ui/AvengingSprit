$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「실루엣 → 공개」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (이 게임 빛 · 연출 원칙) · Projects/AVSR/_exchange/guide_summon_reveal.md (박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_reveal_free_idle.png
PD 말: 「motion-library buy_shop_item_popup_2 영상(실루엣 → 새 스킨 공개)을 참고해서 우리 가차 연출을 적용」.

[부품] 바닥 빛 고리 — 실루엣 발밑에 깔리는 납작한 타원 빛 고리, 도는 루프 6장
- 빛 그림이다: **검은 바탕(#000000)** 위에 그린다(게임이 가산으로 겹친다 — 검은색 = 투명). 마젠타 금지.
- 가로 6칸 한 줄(칸 비율 2:1 — 납작한 타원). 고리 무늬가 천천히 돈다. 1→6→1 끊김 없이.
- 시안 기다림 장면 바닥의 푸른 빛 고리처럼. 흰 심 + 하늘 번짐(게임이 등급 색으로 물들인다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/reveal_ring.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
