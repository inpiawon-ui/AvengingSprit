$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md (컨셉 · 박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png
화풍 기준(실험실 소품 · 이미 게임에 있는 것): Assets/BaseResource/InGameMainUI/obj_lab_bulk.png · Assets/BundleResource/RoomFloor/roomfloor_env_lab_full.png
PD 말: 「motion-library 의 gacha_reveal_1 참고해서 가차 · 우리 컨셉에 맞게 실험실에서 유령이 나오는 컨셉」 · 「그 조합으로 진행해줘」.

[부품] 유리 깨짐 — 배양조 유리가 산산이 깨지며 파편 · 액체가 튀는 6장(한 번 재생)
- 형태 그림: **마젠타 #FF00FF 단색 바탕**(반투명 헤일로 금지 · 검은 바탕 금지).
- 가로 6칸 한 줄, 칸마다 Projects/AVSR/_exchange/in/lab_tank.png 크기의 1.6배(파편이 밖으로 튄다). 배양조 몸체는 그리지 마라 — 유리 파편 · 액체 방울 · 물보라만.
- 1 유리창이 부서지기 시작 → 2~3 파편이 사방으로 가장 크게 튐(자유 시안 공개 순간처럼) → 6 파편이 떨어져 바닥에 흩어짐 · 물보라 사라짐.
- 파편은 밝은 하늘 · 흰 유리 조각(도트), 액체는 청록 방울.
- 시안 속 캐릭터(용) · 유령 · 글자 · 이름 띠는 **그리지 마라**(게임이 따로 얹는다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_shatter.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
