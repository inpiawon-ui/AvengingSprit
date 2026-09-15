"""avspirit ROM 복사본의 입력 보호 MCU 대기 루틴을 우회 패치한다. 원본 ROM 폴더는 건드리지 않는다.

MCU 덤프(avspirit.mcu)가 없으면 메인 CPU가 MCU 응답 인터럽트를 영원히 기다린다.
대기 루틴을 '선택값 쓰기 → 바로 읽기 → 복귀'로 바꾸고, 읽는 값은 MAME Lua(usage_logger.lua)가 채운다.
사용법: python patch_mcu_bypass.py <원본 ROM 폴더> <출력 폴더(…/avspirit)>
"""
import shutil
import sys
from pathlib import Path

PATCHES = {
    # $fd6: move.w D0,$e0000 / move.w $e0000,D0 / andi.w #$ff,D0 / rts
    0xFD6: bytes([0x33, 0xC0, 0x00, 0x0E, 0x00, 0x00,
                  0x30, 0x39, 0x00, 0x0E, 0x00, 0x00,
                  0x02, 0x40, 0x00, 0xFF,
                  0x4E, 0x75]),
    # $ffa: rts (MCU ack 대기 생략)
    0xFFA: bytes([0x4E, 0x75]),
}
EVEN_ROM, ODD_ROM = "spirit05.rom", "spirit06.rom"
DUMMY_MCU = bytes([0xC8, 0xFE]) * 8192  # TLCS-90 'JR -2' 무한루프 — 체크섬 경고만 뜨고 부팅된다


def main(src_dir, dst_dir):
    src, dst = Path(src_dir), Path(dst_dir)
    if not (src / EVEN_ROM).exists() or not (src / ODD_ROM).exists():
        raise SystemExit(f"{src} 에 {EVEN_ROM}/{ODD_ROM} 이 없다")
    dst.mkdir(parents=True, exist_ok=True)
    for f in src.iterdir():
        if f.is_file():
            shutil.copy2(f, dst / f.name)
    even = bytearray((dst / EVEN_ROM).read_bytes())
    odd = bytearray((dst / ODD_ROM).read_bytes())
    for addr, data in PATCHES.items():
        for i, value in enumerate(data):
            a = addr + i
            (even if a % 2 == 0 else odd)[a // 2] = value
    (dst / EVEN_ROM).write_bytes(even)
    (dst / ODD_ROM).write_bytes(odd)
    mcu = dst / "avspirit.mcu"
    if not mcu.exists():
        mcu.write_bytes(DUMMY_MCU)
    print(f"patched → {dst}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
