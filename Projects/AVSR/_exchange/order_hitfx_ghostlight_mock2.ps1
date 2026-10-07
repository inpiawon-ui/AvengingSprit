param([string]$Which)
$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$common = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
PD에게 보여 줄 **연출 시안 2차**다(완성된 게임 화면 한 장). 설계서: Projects/AVSR/_exchange/spec_hitfx_ghostlight.md
1차 시안 검수: Projects/AVSR/_exchange/review_hitfx_ghostlight_mock.md — 「고칠 발주 문장」을 그대로 따른다.
캔버스 720 x 1280(세로). 캐릭터 도트 원본은 덮거나 다시 그리지 않는다. 표시 글자는 한국어로 작게.
'@
switch ($Which) {
  'hit' { $body = @'
[1차 시안] Projects/AVSR/_exchange/in/mock_hitfx.png — 같은 게임 화면과 캐릭터 배치를 유지하고 피격 합성부만 다시 그린다.
- 적 오른쪽 윤곽에 38~42px 회백 표창 접촉 코어. 청록 테두리의 금속 조각은 맞은 반대편(왼쪽) 바깥으로만 흐른다.
- 치명 겹: 58~72px 안에서 백색 10px 편심 마름모 + 왼쪽이 긴 비대칭 담황 스파이크 6개. 대칭 방사형 노란 별 금지.
- 무기 상성 유리 겹: 58~72px 청록 절삭선 3개 · 열린 호 · 황금 사각 파편 5개, 고유 코어보다 옅게(65%).
- 세 겹 모두 얼굴 · 가슴과 중앙 36x72px를 비운다. 몸 위에는 25~45% 얇은 선과 작은 접촉점만.
- 하단 인셋은 완성 이펙트 한 장이 아니라 「고유 코어 / 치명 겹 / 무기 상성 겹」 독립 부품과 각 부품의 프레임 변화(2~4컷)를 보여 준다.
- 파란 얼음 · 큰 노란 별 · 붉은 공통 불티 금지.
[저장] Projects/AVSR/_exchange/in/mock_hitfx_v2.png
'@ }
  'ghost' { $body = @'
[1차 시안] Projects/AVSR/_exchange/in/mock_ghostlight.png — 같은 게임 화면과 대상 위치를 유지하고 중앙의 한 줄짜리 청백 기둥을 지운다.
- 대상 뒤에 폭 82~94px · 높이 190~224px · 진하기 18~30%의 옅은 사다리꼴 광막 두 장(서로 다른 폭, 살짝 어긋남).
- 그 안에만 2~4px 냉백 심을 가진 10~16px 폭의 끊긴 빛심. 머리 위에서 시작해 아래로 뻗되 얼굴 · 가슴 중앙에서는 약해지거나 끊겨 캐릭터 중앙 54x96px 픽셀이 선명해야 한다.
- 1~4px 빛 알갱이 동시 10개 이하, 두 묶음이 위에서 아래로 떨어지는 중.
- 발밑에 76~88 x 24~28px 열린 고리(뒤 60%는 몸 뒤, 앞 40%는 몸 앞).
- 좌우 어깨 · 옆구리 · 다리 바깥에만 30~48% 얇은 몸 테두리.
- 왼쪽 인셋 3컷: 내려옴 / 머묾 / 거둠. 거둠 컷은 별 폭발 없이 광막 92→22px · 고리 88→24px로 가슴 쪽으로 수축, 알갱이가 가슴 바깥으로 빨려 들어가고 빛심이 위로 회수되는 모습.
- 전체 청색 틴트 · 불투명 스포트라이트 원뿔 금지.
[저장] Projects/AVSR/_exchange/in/mock_ghostlight_v2.png
'@ }
}
codex exec --sandbox workspace-write --skip-git-repo-check ($common + "`n" + $body)
