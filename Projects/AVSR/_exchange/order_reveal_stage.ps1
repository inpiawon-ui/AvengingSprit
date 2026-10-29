$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「실루엣 → 공개」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (이 게임 빛 · 연출 원칙) · Projects/AVSR/_exchange/guide_summon_reveal.md (박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_reveal_free_idle.png
PD 말: 「motion-library buy_shop_item_popup_2 영상(실루엣 → 새 스킨 공개)을 참고해서 우리 가차 연출을 적용」.

[부품] 영혼 무대 바탕 — 실루엣이 자라는 기다림 단계 전체 화면 그림(세로 720×1280 비율, 한 장)
- 시안의 기다림 장면처럼 밤의 영혼 무대 — 짙은 남색 · 푸른 안개, 화면 아래 1/3 쯤 가운데에 둥근 영혼 발판(바닥 문양).
- 공개 무대(밤 도시 물가)와 **확실히 달라 보여야** 한다(공개 순간에 컷으로 바뀐다) — 도시 · 건물 없이 안개 낀 영혼의 방.
- 빛기둥 · 바닥 빛 고리 · 실루엣 · 글자 · 판은 넣지 마라(따로 겹친다). 가운데는 비워 둔다.
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/reveal_stage.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
