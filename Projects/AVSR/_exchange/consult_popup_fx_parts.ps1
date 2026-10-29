$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 상의 문서만 쓴다. 한 번에 끝낸다.

[상황] PD: 「팝업 · 특수 오브젝트 · 스테이지 클리어에 연출 · 이펙트가 하나도 없다. 이미지 하나로 끝냈다. 이펙트를 다 넣어라」.
연출 시안(3컷 스토리보드)을 뽑았다 — 이걸 게임에 넣으려면 어떤 이펙트 부품을 발주하고 무엇을 코드(트윈 · 파티클)로 할지 정해야 한다.
- Projects/AVSR/_exchange/in/mock_fxstory_{levelup|altar|devil|shop|room_heal|room_devil|room_shop}_v1.png · mock_fxstory_clear_v2.png(있으면)
- 지금 게임 화면: Projects/AVSR/_exchange/ref/popup_c_ingame/{levelup|altar|devil|shop|room_heal|room_devil|room_shop}.png

[게임 쪽 도구 — 이미 있다]
- Impact: 도트 프레임 애니(fx_{이름}_1..8, 256 px 칸) 재생 · 빔(가로 그림 늘이기) · 색 · 수명 · 이동
- ParticleFxPool: 도트 입자 뿌리기(점 · 불씨 · 연기 · 금화 알갱이 · 색 지정)
- UI 트윈(크기 · 위치 · 알파 · 색, UniTask)
- PD 규칙: 이펙트는 그림으로 발주(코드로 도형을 그리지 않는다) · 별/표창/십자 반짝이 금지 · 화면 전체 물들이기 금지 · 게임을 멈춘 듯한 연출 금지 · 리소스는 공통으로 한 벌을 여러 곳이 같이 쓴다.

[써 낼 것] 한국어 표:
1) 화면마다(8개) 박자별로 — 「발주 그림(이름 · 장 수 · 크기)」 / 「코드 트윈 · 파티클」 / 「길이(초)」
2) 여러 화면이 같이 쓸 공통 부품으로 묶은 최종 발주 목록(중복 제거) — 이름 fx_ui_* · 장 수 · 칸 크기 · 대표 색(색만 다른 것은 한 그림 + 색 곱하기로 갈 수 있는지)
3) 시안에서 그대로 하면 안 되는 것(과함 · 게임 멈춤처럼 보임 · 글자 가림)과 고칠 점
[저장] Projects/AVSR/_exchange/consult_popup_fx_parts.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
