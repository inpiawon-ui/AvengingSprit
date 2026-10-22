$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 3단계 방 틀 시안 B 「양옆 물 + 돌 블록」(원작 배치 패턴으로 짠 방)

■ 바탕 — 지금 게임 화면(1챕터 쓰레기장, 720 x 1280) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_tpl_mock/base_room_now.png
  · 위 HUD(유령 · 호스트 칸 · 챕터 이름 · 골드 · 일시정지)와 아래 조이스틱 · 버튼 셋 · 방 바닥 · 벽 · 아래 입구 난간은 **그대로** 둔다.
  · 바뀌는 것은 방 안 물건 배치와 몬스터뿐이다. 방 안 칸은 가로 10 칸 × 세로 15 칸쯤(바닥 타일 한 장 = 2 x 2 칸).

■ 있는 물건 그림(쓰레기장 줄을 쓴다) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/plan_content_expand/step3_research/room_objects.png
  · 낱장 : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/InGameMainUI/obj_junkyard_block_1.png · obj_junkyard_bulk.png · obj_junkyard_pillar.png · obj_junkyard_crate_1.png · obj_junkyard_low_cover.png · obj_junkyard_barricade.png (같은 폴더)
  · 이 그림들은 **그대로** 쓴다(크기 · 결 유지). 물건 그림에 빛 · 아우라를 굽지 않는다.

■ 몬스터(이 그림 그대로, 같은 배율) : C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/skeleton/unit_skeleton_s.png (해골 = 근접) ·
  C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/scrapgunner/unit_scrapgunner_s.png (고철 전갈 = 원거리) · C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/bat/unit_bat_s.png (박쥐 = 비행) ·
  C:/won/UnityProject/AvengingSprit/Assets/BaseResource/Unit/actor_enforcer/unit_actor_enforcer_s.png (돌 고릴라 = 근접 큰 몸)
  · 몬스터 크기는 바탕 화면의 해골 · 갱스터와 같게. 몬스터는 모두 아래(입구 쪽)를 본다. 플레이어(갱스터)는 아래 입구 바로 위 가운데.

■ 원작 배치 규칙(지켜야 한다 — 원작 8천여 맵 실측) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/plan_content_expand/step3_research/layout_compare.png 위 줄
  · 물건은 방의 **8~12%** 만 · **좌우 대칭** · 입구 아래 2줄 가운데는 비운다 · 가장 가까운 몬스터는 플레이어에서 3칸 넘게.
  · 몬스터 **5~6마리**, 좌우 짝으로 · 근접은 위 · 가운데 · 원거리는 양옆 끝 · 귀퉁이 · 비행은 물 · 벽 사이.

■ 이 방의 틀(글자 그림 — 위가 문, 아래가 입구. # 막는 물건 · ~ 물(몸은 막고 탄은 지나감) · m 근접 · r 원거리 · f 비행 · P 플레이어)
  ..........
  ...m..m...
  ~~~#..#~~~
  ~~~#..#~~~
  ..........
  .r......r.
  ..........
  ....ff....
  ~~#....#~~
  ~~#....#~~
  ..........
  ..........
  ..........
  ..........
  ....P.....
  · m = 돌 고릴라(위 둘), r = 고철 전갈, f = 박쥐(물 위를 난다), # = 있는 블록.

■ 새로 그릴 것 — **넓은 물 웅덩이**(쓰레기장 결: 기름 섞인 오수 · 녹슨 가장자리)
  · 원작 방의 40% 가 물 위주다. 물은 **몸은 못 건너고 탄은 지나간다** — 건너편 원거리 몹과 탄을 주고받는 곳.
  · 지금 우리 물(obj_junkyard_channel_h.png)은 한 칸 폭 쇠창살 줄이라 물로 안 읽힌다. **여러 칸이 이어지는 웅덩이**로 새로 그린다 — 가장자리 · 귀퉁이가 이어지는 타일 모양.
  · 바닥보다 한 단 낮게, 수면이 어둡고 기름 무지개 몇 점 · 떠 있는 쓰레기 한두 개. 빛 · 반짝이를 굽지 않는다(수면 반짝임은 나중에 이펙트로).

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_room_tpl_b.png — 720 x 1280, 바탕 화면과 같은 구도 · 같은 픽셀 크기.
  네가 이 게임 아트 디렉터라면 이 틀을 어떻게 보이게 하겠나 — 배치(칸)는 지키고 그림 결 · 깊이 · 그림자는 자유.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
