# <ProjCode> — 이 프로젝트 값 (fx-art-pipeline)

> 키트 `wa-game-harness-kit` 로 들였다(YYYY-MM-DD). 스킬 본문(`SKILL.md`)보다 이 문서가 이 게임의 값이다. 빈칸을 채운다.

## 경로
- 교환 폴더: `Projects/<ProjCode>/_exchange/` — 납품 `in/` · 참조 `ref/` · 가이드 `guide_<주제>.md` · 발주문 `order_*.ps1`
- **브리프**: `Projects/<ProjCode>/_exchange/codex_brief_fx.md` (없으면 `codex_brief_template.md` 로 만든다 — 첫 자유 시안 통과 뒤 채운다)
- 세이브 위치(시험 전 백업): `%USERPROFILE%\AppData\LocalLow\<회사>\<제품>\SaveData`

## 도구
- 코덱스 발주 실행기: `Tools/harness/run_codex_orders.ps1 -Sheets a,b,c` (한 줄로만)
- 납품 반영: (이 게임의 반영 도구 메뉴 — 예: `Tools/Game/납품 반영 (_exchange/in)`)
- 아이콘 검수 · 맞춤: `python Tools/harness/icon_tool.py review|fit --in <납품 폴더> --names a,b,c --size 128`
- 리소스 포맷 점검: `python Tools/harness/resource_audit.py --out Projects/<ProjCode>/_exchange/ref/resource_audit`
- 이징 곡선: (이 게임의 Ease 위치)
- 파티클 풀: (이 게임의 파티클 풀 위치 — 새 파티클은 여기에 종류를 더한다)

## 이 게임의 화풍
- (도트 픽셀 / 일러스트 / 3D …) · 기준 해상도 · 대상 기기
- 통과 기준 그림(브리프 2절에 「가져올 것 · 피할 것」과 같이)
