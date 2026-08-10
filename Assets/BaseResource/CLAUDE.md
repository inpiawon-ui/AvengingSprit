# Assets/BaseResource — 원본 리소스 보존

이 폴더는 **Addressable 번들 대상이 아닌** 원본 리소스를 저장합니다.

## 용도

- 에디터 툴이 자동 생성하는 **placeholder 텍스처(PNG)** 저장 위치
- 빌드에 포함되며 직접 참조됨 (런타임 Addressable 로드 대상 아님)

## 하위 폴더 구조

| 폴더 | 용도 |
|------|------|
| `{프리팹명}/` | 에디터 툴이 자동 생성한 placeholder 텍스처(PNG). 프리팹명과 1:1 대응 |
| `Unit/{캐릭터키}/` | **인게임 캐릭터 몸통 스프라이트.** 캐릭터 한 종 = 폴더 하나 = 아틀라스 하나 |
| `Atlas/` | 공용 아틀라스 원본 소스 (아틀라스 자체는 `BundleResource/Atlas/`에 저장) |
| `Effects/` | 이펙트 원본 소스 |
| `Sounds/` | 사운드 원본 파일 |
| `TableData/` | 테이블 CSV 또는 원본 데이터 파일 |
| `Textures/` | 기타 텍스처 원본 |

> 하위 폴더 추가 시 이 파일을 업데이트한다.

## 캐릭터 스프라이트 (`Unit/`)

인게임 몸통 스프라이트는 **화면 아틀라스에 넣지 않는다.** 캐릭터별로 쪼갠다.

```
Assets/BaseResource/Unit/rambo/unit_rambo.png        ← 기본
Assets/BaseResource/Unit/rambo/unit_rambo_s.png      ← 방향 5장
                                unit_rambo_{se,e,ne,n}.png
```

화면 아틀라스에 섞으면 한 방에 몇 종만 쓰는데도 12종을 통째로 메모리에 올린다.
방향 5장에 공격 프레임까지 붙으면 한 종이 20장이라 페이지 하나에 들어가지도 않는다.
캐릭터별로 나누면 한 종이 512×512(무압축 1MB)로 끝나고, 방이 필요한 것만 올린다.

- 아틀라스: `Assets/BundleResource/Atlas/unit_{키}.spriteatlasv2` → 주소 `atlas/unit_{키}`
- 규격·방향 접미·발 정렬: [`.claude/project/constants.md`](../../.claude/project/constants.md) 4절
- 새 캐릭터·새 프레임은 폴더에 넣고 `Tools > Game > Import Loose Sprites And Repack` 실행

> 호스트 초상(`hostportraitimage_*`)·궁극기 아이콘은 여기가 아니라 화면 폴더에 둔다.
> 호스트 선택 화면은 12종을 한꺼번에 보여주므로, 캐릭터별로 쪼개면 아틀라스를 12개 열게 된다.

## 경로 규칙

- 프리팹 생성 시 placeholder 텍스처는 `Assets/BaseResource/{프리팹명}/` 에 저장
  - 예: `CharacterSelectPanel` 프리팹 → `Assets/BaseResource/CharacterSelectPanel/ThumbnailImage.png`
- 이 폴더의 텍스처를 런타임 코드에서 직접 로드하지 않는다.
- 런타임 에셋은 반드시 `Assets/BundleResource/`에 두고 Addressable로 로드한다.
