$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「실루엣 → 공개」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md (이 게임 빛 · 연출 원칙) · Projects/AVSR/_exchange/guide_summon_reveal.md (박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_reveal_free_peak.png
PD 말: 「motion-library buy_shop_item_popup_2 영상(실루엣 → 새 스킨 공개)을 참고해서 우리 가차 연출을 적용」.

[부품] 공개 정보 판 — 「새 몸! / 대박! / PICK UP」 꼬리표 + 이름 + 등급이 들어갈 비스듬한 띠 판(글자 없이 빈 판)
- 형태 그림이다: **마젠타 #FF00FF 단색 바탕**(반투명 헤일로 금지, 검은 바탕 금지).
- 가로로 긴 판 한 장(비율 약 2:1). 왼쪽 위에 작은 꼬리표 자리(빈 리본), 아래로 이름 두 줄이 들어갈 넓은 칸.
- 시안의 남색 판보다 특별하게 — 짙은 남색 몸 + 금 테 · 오른쪽 끝이 비스듬히 잘린 띠. **글자는 넣지 마라**(언어팩이 넣는다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/reveal_plate.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
