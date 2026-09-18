"""홍보 영상 40초 편집 — 녹화본 두 개(take1 · take2)에서 구간을 잘라 이어 붙인다.

녹화: Unity Recorder, 1080×1920 · 30fps · 게임 소리 포함 (2026-09-18)
  take1 — 타이틀 → 로비 → 챕터 선택 → 호스트 선택 → 1-001 빙의 → 스킬
  take2 — 1-015 보스전 → 보스 격파 → 레벨업 카드 → CHAPTER 1 CLEAR

구간을 바꾸려면 아래 SEGMENTS 의 초만 고쳐 다시 돌린다.
"""
import subprocess, os

HERE = os.path.dirname(os.path.abspath(__file__))
FFMPEG = r'C:\won\tools\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe'
RAW = os.path.join(HERE, 'raw')
OUT = os.path.join(HERE, 'AVSR_promo_40s.mp4')

# (파일, 시작 초, 끝 초, 설명)
SEGMENTS = [
    ('promo_take1.mp4', 1.0, 4.0, '타이틀'),
    ('promo_take1.mp4', 46.0, 50.0, '로비 → PLAY → 챕터 선택'),
    ('promo_take1.mp4', 56.5, 59.5, 'START → 호스트 선택'),
    ('promo_take1.mp4', 66.0, 68.2, '호스트 선택 → 빙의 시작'),
    ('promo_take1.mp4', 70.5, 72.0, '유령 등장'),
    ('promo_take1.mp4', 97.8, 101.0, '적에게 빙의'),
    ('promo_take2.mp4', 0.3, 15.5, '보스 등장 · 공격 · 스킬 컷인'),
    ('promo_take2.mp4', 36.3, 40.0, '마지막 스킬 → 보스 격파'),
    ('promo_take2.mp4', 57.3, 61.5, 'CHAPTER 1 CLEAR'),   # 멈춘 화면이라 길게 두지 않는다
]

FADE = 0.4   # 처음 · 끝 페이드
CLICK = 0.04  # 구간 이음매 소리 튐 방지


def main():
    inputs, parts, labels = [], [], []
    files = sorted({s[0] for s in SEGMENTS})
    for f in files:
        inputs += ['-i', os.path.join(RAW, f)]
    idx = {f: i for i, f in enumerate(files)}
    total = sum(e - s for _, s, e, _ in SEGMENTS)

    for n, (f, s, e, _) in enumerate(SEGMENTS):
        i, dur = idx[f], e - s
        parts.append(f'[{i}:v]trim={s}:{e},setpts=PTS-STARTPTS,fps=30,format=yuv420p[v{n}]')
        parts.append(f'[{i}:a]atrim={s}:{e},asetpts=PTS-STARTPTS,'
                     f'afade=t=in:d={CLICK},afade=t=out:st={dur - CLICK:.3f}:d={CLICK}[a{n}]')
        labels.append(f'[v{n}][a{n}]')

    parts.append(''.join(labels) + f'concat=n={len(SEGMENTS)}:v=1:a=1[vc][ac]')
    parts.append(f'[vc]fade=t=in:d={FADE},fade=t=out:st={total - FADE:.3f}:d={FADE}[vo]')
    parts.append(f'[ac]afade=t=in:d={FADE},afade=t=out:st={total - FADE:.3f}:d={FADE}[ao]')

    cmd = [FFMPEG, '-y', '-v', 'error'] + inputs + [
        '-filter_complex', ';'.join(parts),
        '-map', '[vo]', '-map', '[ao]',
        '-c:v', 'libx264', '-preset', 'slow', '-crf', '18', '-pix_fmt', 'yuv420p',
        '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', OUT]
    subprocess.run(cmd, check=True)
    print(f'{OUT}  ({total:.1f}초)')


if __name__ == '__main__':
    main()
