$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 공개 순간 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 수준 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_peak_crop.png (PD 가 고른 시안의 공개 순간)
같이 쓰는 그림: Projects/AVSR/_exchange/in/lab_tank.png (배양조) · Projects/AVSR/_exchange/in/lab_stage.png (무대)
⚠ PD: 「깨지고 그 뒤에 불꽃 연출도 이상하고 · 전체적으로 연출은 괜찮은데 디자인 퀄리티가 별로」 — 지금은 예전 레벨업용 주황 가시 빛살을 빌려 써서 탁했다. 대충 그리지 마라.

[부품] 유리 조각 6개 — 공개 순간 사방으로 튀는 낱개 유리 파편(게임이 하나씩 날린다)
- 형태 그림: **마젠타 #FF00FF 단색 바탕**.
- 가로 6칸 한 줄, 정사각 칸마다 **유리 조각 한 개**(크기 · 모양 다르게: 긴 삼각 · 넓은 판 · 작은 쐐기 등).
  정답 그림의 유리 조각처럼 밝은 하늘 · 흰 유리, 또렷한 도트 외곽선, 한쪽 면에 하이라이트.
- 번짐 · 빛 · 물방울은 그리지 마라 — 조각 하나만 또렷하게, 칸 가운데.
- 캐릭터 · 유령 · 글자는 그리지 마라.
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_glass_shards.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
