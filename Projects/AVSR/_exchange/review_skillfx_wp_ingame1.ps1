$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. 이미지를 열어 보고 글로만 검수한다. API 키를 쓰는 CLI 폴백은 쓰지 마라.

[배경] 세로 모바일 액션(720x1280). 네가 쓴 설계서 Projects/AVSR/_exchange/spec_skillfx_weapon.md 대로 무기 속성 7종 스킬 이펙트를 게임에 넣었다.
(호퍼 「정조준」은 부품 hopaim 이 아직 안 와서 이번 검수에서 뺀다. 갱스터 즉사 봉인 gaexec 도 아직 없다.)
통과본 기준: 연쇄 방전 검수 Projects/AVSR/_exchange/review_skillfx_lz_ingame2.md

[게임 결과 — 0.05초 간격, 캐릭터 주변을 잘라 크게] Projects/AVSR/_exchange/ref/skillfx_wp_ingame1/
  03_amazon.png · 02_thug.png · 05_hopper_smg.png · 06_commando_mg.png · 01_gangster.png · 15_ninja.png (+ 15_ninja_full.png 전체 화면 0.2초 간격)
  시간 0.9~1.0초는 컷인(스킬 이름 띠) — 그 뒤가 스킬이다.
[예전 장면] Projects/AVSR/_exchange/ref/skillfx_now/now_{amazon,thug,hopper_smg,commando_mg,gangster,ninja}.png
[부품 원본] Assets/BaseResource/InGameMainUI/fx_wp*.png · fx_amland_* · fx_hsinv_* · fx_cmwall_* · fx_gamark_*

[내가 이미 알고 고칠 것 — 다시 적지 않아도 된다]
- 방벽 전개: 방벽이 서기 전 0.2초 동안 예전 파란 쉴드 거품이 보인다
- 난사: 맞은 자리 청백 코어가 너무 자주 터진다
- 노란 별(평타 공통 타격 이펙트)은 모든 몸 공통이라 따로 다룬다

[답할 것] 한국어. 스킬마다:
1) 판정: 통과 / 손볼 것 있음
2) 손볼 것 — 숫자로(크기 px · 진하기 · 시간 · 자리). 코드로 할 수 있는 것(크기 · 진하기 · 시간 · 자리 · 빼기)만. 새 그림이 필요하면 「새 그림 필요: 무엇」으로 따로 적는다.
3) 예전보다 나아졌는지 한 줄
마지막에 「우선순위 5개」.
[저장] Projects/AVSR/_exchange/review_skillfx_wp_ingame1.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
