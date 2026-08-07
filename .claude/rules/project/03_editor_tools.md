---
description: 에디터 툴 사용 규칙 — CreatePrefabs(프리팹 생성/덮어쓰기), CreateAssetTable(테이블 생성)
alwaysApply: true
---

# 에디터 툴 사용 규칙

## 프리팹 생성 (`Tools > Game > CreatePrefabs`)
- 폴더를 스캔하여 프리팹을 생성한다.
- 프리팹이 **없으면** 신규 생성한다.
- 프리팹이 **이미 있으면** 덮어쓰기한다.
- 생성 후 해당 프리팹을 Addressable 그룹에 자동 등록한다. (Addressable 등록 규칙은 `02_addressables.md` 참조)
- Scene에 게임오브젝트를 직접 추가하지 않는다. 반드시 프리팹으로 생성 후 로드한다.

## 테이블 생성 (`Tools > Game > CreateAssetTable`)
- `CharacterTable.asset`, `SkillTable.asset` 두 파일을 `Assets/BundleResource/TableData/`에 생성한다.
- 테이블이 **없으면** 신규 생성한다.
- 테이블이 **이미 있으면** 덮어쓰기한다.
- 추후 Excel 연동으로 전환 예정이므로 현재는 항상 최신 상태로 유지한다.
