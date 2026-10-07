$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @"
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.
[검수 대상] Projects/AVSR/_exchange/in/skillfx_sample_laser_v1.png — 연쇄 방전 퀄업 시안 4컷(예비 · 발동 · 타격 · 여운).
[비교] Projects/AVSR/_exchange/skillfx_laser_before_after.png (위 = 지금 게임, 아래 = 시안)
[기준] Projects/AVSR/_exchange/review_skillfx_now.md 의 「시안 판정 기준」 5개 + 「2. 방향」.
[볼 것] 판정 기준마다 「통과 / 고칠 것(구체적으로)」, 그리고 게임에 옮길 때(유니티 파티클 · 스프라이트 시퀀스) 무리가 되는 부분.
마지막 줄 종합 판정: 「그대로 보고」 또는 「고쳐서 다시」.
[저장] Projects/AVSR/_exchange/review_skillfx_sample1.md 에 한국어로.
"@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt