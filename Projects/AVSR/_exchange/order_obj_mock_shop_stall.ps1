$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 방 오브젝트 퀄업 **시안** — 완성된 게임 화면 한 장(720 x 1280 세로).

[무엇] 「유령 노점」 — 맵(방)에 딱 하나만 서는 특별 오브젝트. 지금은 너무 작아 티가 안 난다(지금 192 px 칸 — 유닛 한 명과 비슷).
[디자인 앵커 — 반드시 그대로] Projects/AVSR/_exchange/in/obj_shop_stall.png — 보라 · 금 줄무늬 천막 노점 + 물약 · 두루마리 · 금화가 놓인 진열대 + 등불.
  **디자인 · 색 · 구성은 이 그림 그대로** 두고, 크기를 키우고 퀄을 올린다(새 디자인 · 다른 양식으로 바꾸지 말 것).
[바탕] Projects/AVSR/_exchange/ref/skill_qx_check/robot.png — 지금 게임 화면(위 HUD · 아래 조작 버튼 · 방 바닥 그대로).
  방 안의 적 · 이펙트 · 데미지 숫자는 지우고, 아래쪽에 플레이어 캐릭터 하나만 그대로 둔다(크기 비교용, 약 144 px).
[크기 · 위치] 오브젝트는 방 가로 가운데, 방 판(y 약 230~820)의 위쪽 절반에 선다. 크기 폭 약 380 px · 높이 약 320 px —
  플레이어보다 훨씬 커서 방에 들어오면 한눈에 「저게 그거다」.
[퀄업] 진열대 물건이 더 많고 또렷하게(물약 · 두루마리 · 금화 더미 · 작은 상자), 등불 빛이 따뜻하게. 도트 밀도를 높여 디테일을 살리고, 바닥 그림자 · 은은한 빛으로 바닥에 붙어 서 있게. 위에서 비스듬히 내려다본 방 시점과 맞게.
- 게임과 같은 도트(픽셀 아트) 그림체. 별 · 표창 · 십자 반짝이 모양 금지. 화면 전체를 한 색으로 물들이지 않는다. 글자 · 이름표는 넣지 않는다.
[저장] Projects/AVSR/_exchange/in/mock_obj_shop_stall_v1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
