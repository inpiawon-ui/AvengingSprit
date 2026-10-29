$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md (컨셉 · 박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_lab_free_idle.png · Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png
화풍 기준(실험실 소품 · 이미 게임에 있는 것): Assets/BaseResource/InGameMainUI/obj_lab_bulk.png · Assets/BundleResource/RoomFloor/roomfloor_env_lab_full.png
PD 말: 「motion-library 의 gacha_reveal_1 참고해서 가차 · 우리 컨셉에 맞게 실험실에서 유령이 나오는 컨셉」 · 「그 조합으로 진행해줘」.

[부품] 배양실 무대 바탕 — 세로 720×1280 비율 전체 화면 한 장
- 시안의 어두운 청록 실험실(벽 패널 · 바닥 관 · 아래 받침대 · 옆 작은 배양조들 · 경고등).
- **화면 가운데(위에서 25%~72%)는 비워 둔다** — 큰 배양조를 따로 얹는다. 그 자리 바닥에 배양조가 설 둥근 받침만.
- 왼쪽 아래 구석에 유령이 빠져나올 **관 끝(열린 배출구)** 을 그린다(유령은 그리지 마라).
- 시안 속 캐릭터(용) · 유령 · 글자 · 이름 띠는 **그리지 마라**(게임이 따로 얹는다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_stage.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
