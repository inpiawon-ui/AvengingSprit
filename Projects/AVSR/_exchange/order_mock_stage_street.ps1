$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
먼저 C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다(우리가 정한 그림 방향). 이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 픽셀을 그리거나 늘리거나 뒤집어서 때우지 마라. 한 장만 그린다.

AVENGING SPIRIT: RE:BORN — 3단계 방 물건 시안 「밤거리」 무대 (원작 방 그대로 · 이 무대 물건으로)

■ 구도 · 칸 정답(쓰레기장 판, 통과) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/ref/room_tpl_mock/stage_mock_ref_junkyard.png
  · 이 화면과 **같은 구도 · 같은 칸 자리**로 그린다 — 좌우 귀퉁이 막는 물건 · 가시 줄 셋 · 아래 가운데 물 웅덩이와 그 안 섬 · 몬스터 자리.
  · 화면 폭 720 px 에 가로 11 칸(한 칸 약 65 px). **물건은 제 칸 안에만**, 빈 칸은 바닥이 보이게(특히 물 웅덩이 양옆의 빈 세로 줄은 지나가는 길).
  · 비교용으로 화면 아래쪽 빈 칸 두 곳(물 웅덩이 양옆 가시 줄 바깥 아래)에 **울타리 한 칸씩**만 더 놓는다.
■ 이 무대 바닥 : C:/won/UnityProject/AvengingSprit/Assets/BundleResource/RoomFloor/roomfloor_env_street.png — 방 바닥 · 벽 · 문 · 아래 입구는 이 그림 그대로.
  지금 이 무대 물건(버릴 것 — 모양이 다른 무대와 같다) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/plan_content_expand/step3_research/room_objects.png 의 street 줄
■ 쓰레기장 부품(화풍 · 굵기 · 시점 기준, 모양은 베끼지 않는다) : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_block.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_spike_fence.png · C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/parts_junkyard_water.png

■ 이 무대만의 물건 — 원작(궁수의 전설)은 테마마다 막는 물건 **모양이 통째로 다르다**. 이 무대도 쓰레기장과 겹치는 모양이 없어야 한다.
  · 막는 물건(몸 · 탄 막음) — 서로 다른 실루엣 **5~6가지**를 섞어서. 예(고르는 것은 네 몫) : 자판기 · 대형 쓰레기통 · 소화전 + 표지판 · 넘어진 오토바이 · 신문 가판대 · 망가진 네온 간판 조각
  · 물(몸 막음 · 탄 통과) — 하수가 넘친 빗물 웅덩이(보도블록 턱). 여러 칸이 이어지는 웅덩이 · 가장자리 둑. 빛 · 반짝이를 굽지 않는다.
  · 가시(밟으면 아프다 · 지나갈 수 있다) — 깨진 유리 · 못 박힌 판. 바닥에 붙어 낮게.
  · 울타리(몸 막음 · 탄 통과) — 공사장 철제 펜스. 틈이 보여야 한다.
■ 몬스터는 기준 화면 그림 그대로 · 같은 배율. HUD · 조이스틱 · 버튼은 기준 화면 그대로(챕터 이름 칸 글자는 그대로 둬도 된다).
■ 그림체 — 1991 아케이드 16비트 픽셀 아트 · 1 px 짙은 외곽선 · 2~3단 끊기는 명암 · 물건에 빛 · 아우라를 굽지 않는다.

■ 만들 것 : C:/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/in/mock_stage_street.png — 720 x 1280.
  네가 이 게임 아트 디렉터라면 「밤거리」을 어떻게 꾸미겠나 — 칸 자리는 지키고 물건 모양 · 결 · 색은 자유.
끝나면 경로만 알려라.

"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
