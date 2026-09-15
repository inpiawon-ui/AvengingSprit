"""분할 녹음과 루프 정보(plan JSON)로 최종 WAV를 만든다. 루프곡은 인트로+1루프, 나머지는 전체를 담는다."""
import json
import sys
import wave
from pathlib import Path


def read_wav(path):
    with wave.open(str(path)) as w:
        params = (w.getnchannels(), w.getsampwidth(), w.getframerate())
        return params, w.readframes(w.getnframes())


def write_wav(path, params, frames):
    path.parent.mkdir(parents=True, exist_ok=True)
    channels, width, rate = params
    with wave.open(str(path), "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(width)
        w.setframerate(rate)
        w.writeframes(frames)


def export_item(item, out_root):
    params, frames = read_wav(item["src"])
    channels, width, rate = params
    total = len(frames) // (channels * width)
    loop = item.get("loop")
    length = total
    if loop:
        length = loop["startSamples"] + loop["loopSamples"]
        if length > total:
            raise ValueError(f"{item['dst']}: 루프 끝({length})이 원본 길이({total})를 넘는다")
        frames = frames[:length * channels * width]
    write_wav(Path(out_root) / item["dst"], params, frames)
    entry = {k: v for k, v in item.items() if k != "src"}
    entry.update(sampleRate=rate, lengthSamples=length, lengthSec=round(length / rate, 3))
    return entry


def main(plan_path, out_root, manifest_path):
    plan = json.loads(Path(plan_path).read_text(encoding="utf-8"))
    manifest = [export_item(item, out_root) for item in plan["items"]]
    Path(manifest_path).write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"exported {len(manifest)} files")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
