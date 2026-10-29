$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 공개 순간 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 수준 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_peak_crop.png (PD 가 고른 시안의 공개 순간)
같이 쓰는 그림: Projects/AVSR/_exchange/in/lab_tank.png (배양조) · Projects/AVSR/_exchange/in/lab_stage.png (무대)
⚠ PD: 「깨지고 그 뒤에 불꽃 연출도 이상하고 · 전체적으로 연출은 괜찮은데 디자인 퀄리티가 별로」 — 지금은 예전 레벨업용 주황 가시 빛살을 빌려 써서 탁했다. 대충 그리지 마라.

[부품] 공개 빛 — 몸 뒤에서 터지는 금빛 빛줄기 · 빛 알갱이, 머무는 루프 6장
- 빛 그림: **검은 바탕(#000000)**(게임이 가산으로 겹친다).
- 가로 6칸 한 줄, 정사각 칸. 정답 그림처럼 **가운데 아래(몸 발 자리)에서 위 · 양옆으로 퍼지는 금빛 빛줄기 다발** + 네모 도트 빛 알갱이 · 반짝 별.
  가운데는 몸이 서므로 너무 하얗게 덮지 않는다(빛 뿌리만 밝게).
- 1→6→1 끊김 없이 빛줄기가 천천히 일렁이고 알갱이가 위로 오른다. 주황 · 빨강 불꽃이 아니라 **금 · 노랑 · 흰 빛**.
- 캐릭터 · 유령 · 글자는 그리지 마라.
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/lab_reveal_light.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
