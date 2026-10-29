$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「실루엣 → 공개」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (이 게임 빛 · 연출 원칙) · Projects/AVSR/_exchange/guide_summon_reveal.md (박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_reveal_free_peak.png · Projects/AVSR/_exchange/in/mock_summon_reveal_guide_peak.png
PD 말: 「motion-library buy_shop_item_popup_2 영상(실루엣 → 새 스킨 공개)을 참고해서 우리 가차 연출을 적용」.

[부품] 공개 빛살 터짐 — 진짜 몸이 나타나는 순간 몸 뒤에서 터지는 빛살, 8장(한 번 재생)
- 빛 그림이다: **검은 바탕(#000000)** 위에 그린다(게임이 가산으로 겹친다 — 검은색 = 투명). 마젠타 금지.
- 가로 4칸 × 2줄 = 8칸(정사각 칸). 1 작은 섬광 → 3 빛살이 사방으로 가장 크게 → 8 빛살이 가늘어지며 흩어짐.
- 자유 시안 공개 순간의 푸른 빛살 · 반짝 별처럼(도트 빛살). 가운데는 몸이 서므로 빛살 뿌리만, 심이 하얗게 덮지 않게.
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/reveal_burst.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
