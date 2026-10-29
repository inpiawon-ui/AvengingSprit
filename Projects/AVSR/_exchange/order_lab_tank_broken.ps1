$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 공개 순간 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 수준 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_peak_crop.png (PD 가 고른 시안의 공개 순간)
같이 쓰는 그림: Projects/AVSR/_exchange/in/lab_tank.png (배양조) · Projects/AVSR/_exchange/in/lab_stage.png (무대)
⚠ PD: 「깨지고 그 뒤에 불꽃 연출도 이상하고 · 전체적으로 연출은 괜찮은데 디자인 퀄리티가 별로」 — 지금은 예전 레벨업용 주황 가시 빛살을 빌려 써서 탁했다. 대충 그리지 마라.

[부품] 깨진 배양조 — 공개 뒤 남는 배양조 틀(유리창이 깨져 뻥 뚫림)
- 형태 그림: **마젠타 #FF00FF 단색 바탕**.
- **lab_tank.png 와 정확히 같은 크기 · 같은 모양 · 같은 자리**의 배양조. 위 뚜껑 · 양옆 관 · 아래 받침은 그대로, **유리창만 깨져서**
  가장자리에 들쭉날쭉한 유리 조각이 붙어 있고 가운데는 뚫려 비어 있다(뒤가 보이게 그 자리는 마젠타).
- 액체는 빠지고 아래 받침에 청록 물기 · 물웅덩이만 조금. 몸 · 빛은 그리지 마라.
- 캐릭터 · 유령 · 글자는 그리지 마라.
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_tank_broken.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
