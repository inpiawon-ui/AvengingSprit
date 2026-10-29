$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md (컨셉 · 박자표).
PD 가 고른 시안(이 화풍이 정답): Projects/AVSR/_exchange/in/mock_summon_lab_free_idle.png
화풍 기준(실험실 소품 · 이미 게임에 있는 것): Assets/BaseResource/InGameMainUI/obj_lab_bulk.png · Assets/BundleResource/RoomFloor/roomfloor_env_lab_full.png
PD 말: 「motion-library 의 gacha_reveal_1 참고해서 가차 · 우리 컨셉에 맞게 실험실에서 유령이 나오는 컨셉」 · 「그 조합으로 진행해줘」.

[부품] 배양조 유리 앞면 반사광 — 몸 그림자 위에 겹쳐 「유리 속에 있다」로 보이게 하는 빛
- 빛 그림: **검은 바탕(#000000)** 위에(게임이 가산으로 겹친다 — 검은색 = 투명). 마젠타 금지.
- **Projects/AVSR/_exchange/in/lab_tank.png 와 정확히 같은 크기 · 같은 자리**의 한 장. 유리창 자리에만 세로 반사 줄 2~3개 · 가장자리 은은한 하이라이트 · 작은 기포 점.
- 금속 부분 · 액체 색은 그리지 마라(빛만).
- 시안 속 캐릭터(용) · 유령 · 글자 · 이름 띠는 **그리지 마라**(게임이 따로 얹는다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_tank_glass.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
