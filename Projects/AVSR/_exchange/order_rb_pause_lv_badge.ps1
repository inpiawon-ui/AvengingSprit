$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 — 일시정지 창 카드 칸 오른쪽 아래 **Lv 배지 판 부품**.
[정답 시안] Projects/AVSR/_exchange/in/mock_pause_panel_free_peak.png — 카드 칸 오른쪽 아래 「Lv 2」 배지.
[앵커] Projects/AVSR/_exchange/ref/pause_panel_mock/crop_card.png — 오른쪽 아래 「Lv 2」 배지 모양(어두운 남색 속 · 하늘색 테 · 둥근 모서리)을 따른다.
[부품] rb_pause_lv_badge — 가로로 긴 둥근 배지 판 한 장 **128 × 64**, 칸의 90% 안.
- 속은 어두운 남색, 테는 밝은 하늘색 2~3 px, 바깥 빛은 아주 약하게. **글자는 그리지 않는다**(게임이 「Lv 2」를 넣는다).
- 바깥은 분홍(#FF00FF) — 나중에 뚫는다. 판에 분홍이 섞이지 않게.
[저장] Projects/AVSR/_exchange/in/rb_pause_lv_badge.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
