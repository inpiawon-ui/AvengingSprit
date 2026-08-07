# AVSR 리소스 교환 폴더

Claude Code(Unity 측)와 Codex(리소스 생성 측)가 공유하는 고정 경로다.
ZIP을 주고받지 않고 이 폴더만 보면 된다.

```
_exchange/
  in/    Codex → Unity   납품물을 여기에 저장한다 (개별 PNG, 하위 폴더 자유)
  out/   Unity → Codex   앵커·명세·수정지시를 여기에 놓는다
```

## 절대 경로

```
C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\
```

Codex 작업 폴더(`Documents\Codex\{날짜}\...`)는 **날짜마다 바뀌므로** 쓰지 않는다.
이 경로는 고정이다.

## 규칙

- Codex는 `out/` 을 읽고 `in/` 에 쓴다. `out/` 을 수정하지 않는다.
- Claude Code는 `in/` 을 읽고 `out/` 에 쓴다. `in/` 을 수정하지 않는다.
- 납품 후 Claude Code가 `_verify_delivery.py` 로 검수하고 결과를 `out/_verify_result.txt` 에 남긴다.
- 파일명은 명세표의 `filename` 열 그대로. 이름이 곧 Unity GameObject 이름이자 바인딩 키다.

## 현재 작업

`out/` 에 재제작 팩(앵커 42종 + 복원본 6종 + 목업)이 들어 있다.
자세한 지시는 `out/README.md` 참조.
