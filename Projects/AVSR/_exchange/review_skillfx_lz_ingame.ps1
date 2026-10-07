$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.
[검수 대상] 연쇄 방전 퀄업 연출을 **게임에 넣은 결과**(실제 게임 화면, 720x1280).
- 게임 장면: Projects/AVSR/_exchange/ref/skillfx_now/lz_game_1.00.png · lz_game_1.08.png · lz_game_1.13.png · lz_game_1.20.png · lz_game_1.35.png · lz_game_1.60.png
- 전후 비교: Projects/AVSR/_exchange/skillfx_laser_ingame_before_after.png (위 = 예전 게임, 아래 = 새 연출)
- 통과 시안: Projects/AVSR/_exchange/in/skillfx_sample_laser_v2.png
[알아 둘 것] 영상 녹화용으로 적 셋을 일부러 내 앞에 모았다. 스킬은 적 셋을 이어 탄다(조준선이 나 → 적1 → 적2 → 적3 으로 이어진다) — 시안은 적 하나였다.
[볼 것] 시안 대비 「통과 / 고칠 것(구체적으로, 숫자로)」: 1 예비→발동→타격→여운이 읽히는가 2 코어 · 파편이 적을 덮지 않는가 3 무기 속성 모양 4 밝기 왕 하나 5 화면 전체 틴트 없음.
마지막 줄 종합 판정: 「그대로 보고」 또는 「고쳐서 다시」.
[저장] Projects/AVSR/_exchange/review_skillfx_lz_ingame.md 에 한국어로.
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt