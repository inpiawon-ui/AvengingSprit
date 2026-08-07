# Assets/BaseResource — 원본 리소스 보존

이 폴더는 **Addressable 번들 대상이 아닌** 원본 리소스를 저장합니다.

## 용도

- 에디터 툴이 자동 생성하는 **placeholder 텍스처(PNG)** 저장 위치
- 빌드에 포함되며 직접 참조됨 (런타임 Addressable 로드 대상 아님)

## 하위 폴더 구조

| 폴더 | 용도 |
|------|------|
| `{프리팹명}/` | 에디터 툴이 자동 생성한 placeholder 텍스처(PNG). 프리팹명과 1:1 대응 |
| `Atlas/` | 공용 아틀라스 원본 소스 (아틀라스 자체는 `BundleResource/Atlas/`에 저장) |
| `Effects/` | 이펙트 원본 소스 |
| `Sounds/` | 사운드 원본 파일 |
| `TableData/` | 테이블 CSV 또는 원본 데이터 파일 |
| `Textures/` | 기타 텍스처 원본 |

> 하위 폴더 추가 시 이 파일을 업데이트한다.

## 경로 규칙

- 프리팹 생성 시 placeholder 텍스처는 `Assets/BaseResource/{프리팹명}/` 에 저장
  - 예: `CharacterSelectPanel` 프리팹 → `Assets/BaseResource/CharacterSelectPanel/ThumbnailImage.png`
- 이 폴더의 텍스처를 런타임 코드에서 직접 로드하지 않는다.
- 런타임 에셋은 반드시 `Assets/BundleResource/`에 두고 Addressable로 로드한다.
