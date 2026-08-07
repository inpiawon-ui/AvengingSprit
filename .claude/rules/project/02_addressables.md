---
description: Addressable 사용 규칙 — 소문자 네이밍, Key 형식, 라벨(사전 로드 전용) vs 주소(런타임 로드), Atlas 자동 등록, CDN 전환 지점
alwaysApply: true
---

# Addressable 사용 규칙

그룹 구조·경로·라벨 매핑 테이블은 [`Assets/BundleResource/CLAUDE.md`](../../../Assets/BundleResource/CLAUDE.md) 참조.

---

## 네이밍 규칙

- Group Name, Entry Name, Label 등 Addressable에서 사용하는 모든 이름은 **소문자**로 작성한다.
- Label 이름은 `label_{그룹명}` 형식으로 지정한다.

## Addressable 주소(Key) 형식

- 형식: `{그룹 폴더명}/{에셋파일명}` (확장자 제외)
- 예: `TableData/CharacterTable`, `UI/Lobby/CharacterSelectPanel`, `atlas/thumbnail`

## 라벨 vs 주소 사용 구분

| 용도 | 방식 | 예시 |
|------|------|------|
| 번들 사전 로드 | **라벨** 사용 | `Addressables.DownloadDependenciesAsync("label_tabledata")` |
| 에셋 로드 (단일/다수) | **주소** 사용 | `Addressables.LoadAssetAsync<T>("TableData/CharacterTable")` |

- **라벨은 사전 로드 전용**. 런타임 에셋 로드에 라벨 사용 금지.
- **런타임 로드는 반드시 주소(Key)** 를 사용한다.

## Atlas 규칙

- 모든 SpriteAtlas는 `Assets/BundleResource/Atlas/` 에만 저장한다.
- `.spriteatlasv2` 파일을 `Atlas/` 폴더에 추가하면 `AtlasAddressablePostprocessor`가 자동 등록. 수동 등록 불필요.
- 일괄 재등록: `Tools > Game > Scan & Register Atlas Folder`
- 개별 스프라이트를 Addressable에 직접 등록하지 않는다. 아틀라스 파일만 등록한다.

## CDN 전환 지점

현재 빌드 내장(Local). CDN 전환 시 `Assets/BundleResource/CLAUDE.md`의 그룹 테이블에 따라
각 그룹의 Build & Load Path를 `RemoteBuildPath / RemoteLoadPath`로 교체하고 `RemoteLoadPath`에 CDN URL 입력.
코드·주소·라벨 체계는 변경 없음.
