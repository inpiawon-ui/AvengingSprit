$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md (컨셉 · 박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_lab_free_idle.png · Projects/AVSR/_exchange/in/mock_summon_lab_guide_idle.png
화풍 기준(실험실 소품 · 이미 게임에 있는 것): Assets/BaseResource/InGameMainUI/obj_lab_bulk.png · Assets/BundleResource/RoomFloor/roomfloor_env_lab_full.png
PD 말: 「motion-library 의 gacha_reveal_1 참고해서 가차 · 우리 컨셉에 맞게 실험실에서 유령이 나오는 컨셉」 · 「그 조합으로 진행해줘」.

[부품] 큰 배양조 한 개 — 빈 유리 원통(안에 청록 액체 · 기포 몇 개, **몸은 없음**)
- 형태 그림: **마젠타 #FF00FF 단색 바탕**(반투명 헤일로 금지 · 검은 바탕 금지).
- 세로로 긴 한 장(가로:세로 약 3:5). 위 금속 뚜껑 · 양옆 관 · 아래 받침 · 가운데 넓은 유리창(시안의 배양조와 같은 모양).
- 유리 안 액체는 **반쯤 비쳐 보이는 밝은 청록**(나중에 안에 짙은 몸 그림자를 겹친다). 유리창은 가운데가 넓고 곧게.
- 시안 속 캐릭터(용) · 유령 · 글자 · 이름 띠는 **그리지 마라**(게임이 따로 얹는다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_tank.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
