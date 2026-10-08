$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
그림을 그리지 마라. API 키를 쓰는 CLI 폴백은 쓰지 마라. 코드는 고치지 마라 — 상의 문서만 쓴다. 한 번에 끝낸다.

[PD 요청 정정] 「지금 디자인이 별로다 — 팝업 4종(레벨업 · 회복 제단 · 악마의 거래 · 상점)과 방에 하나만 서는 오브젝트 3종(회복 제단 · 악마 제단 · 상점)을
  둘 다 새 디자인으로 바꾸고, 오브젝트는 크게. 다 통일된 느낌으로 시안 몇 가지」. 크기만의 문제가 아니라 디자인 자체.
  앞서 낸 「유령 부적」 같은 엉뚱한 소재는 반려됐다. 팝업만 새 양식, 오브젝트는 그대로 — 처럼 반쪽만 내는 것도 반려.
[지금] 팝업 Projects/AVSR/_exchange/ref/popup_now_sheet.png · 오브젝트 Projects/AVSR/_exchange/ref/obj_now_sheet.png ·
  게임 화면(새 HUD · 공장 방) Projects/AVSR/_exchange/ref/skill_qx_check/robot.png.
  게임: 원작 Avenging Spirit 리메이크 — 유령이 군인 · 로봇 · 괴물 몸에 빙의해 싸우는 도트 액션 로그라이크, 무대는 쓰레기장 · 미사일 기지 · 연구소 같은 시설.
[이전 상의] Projects/AVSR/_exchange/consult_popup_restyle.md — 글자 · 칸 수치는 그대로 쓴다.

[Claude 초안 — 방향 3개, 각각 오브젝트 3 + 팝업 4 가 한 벌]
- A 「성역과 심연」 정통 다크 판타지 고퀄: 회복 = 흰 금빛 천사 성소(날개 석상 · 빛 기둥) / 악마 = 흑요석 뿔 아치 문 + 지옥불 / 상점 = 유령 상인의 랜턴 마차.
  팝업은 각 오브젝트의 재질로 짠 장식 틀.
- B 「버려진 시설 속 영적 장치」(게임 무대에 맞춤): 회복 = 성스러운 빛이 새어 나오는 낡은 치료 캡슐 · 성수 탱크 / 악마 = 사슬 · 룬으로 봉인된 악마 금고문 /
  상점 = 유령 상인이 지키는 컨테이너 가게. 팝업은 시설 단말기 판 + 각 색.
- C 「영계」: 회복 = 떠 있는 영혼의 샘 / 악마 = 허공에 열린 악마의 눈 문 / 상점 = 등불 든 유령 상인 본인(캐릭터) + 떠다니는 물건. 팝업은 반투명 영계 판.

[써 낼 것] 한국어, 짧게:
1) A · B · C 각각 한 줄 평 + 고칠 점. 이 게임에 안 맞는 소재는 빼고 더 나은 방향이 있으면 하나 대체.
2) 최종 3개 방향을 확정해 방향마다 — 오브젝트 3종 · 팝업 4종의 핵심 모습을 그림 담당이 바로 그릴 수 있게 한 줄씩(재질 · 실루엣 · 대표 색).
3) 네 팝업과 세 오브젝트가 「한 벌」로 보이게 하는 공통 장치 2~3개.
[저장] Projects/AVSR/_exchange/consult_popup_obj_set.md
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
