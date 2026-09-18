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
# take4 — 자동 조종(PromoPilot)으로 타이틀부터 1챕터 끝까지 한 번에 찍은 판 (장면 기록 promo_take4_marks.txt)
# take5 — 보스전만 다시 찍은 판 (보통 데미지 · 상자 칸 비움 → 결과창에 「상자 획득」)
SEGMENTS = [
    # take7 — 청룡(뇌전 폭주)으로 자동 조종, 처음부터 끝까지 한 판 (장면 기록 promo_take7_marks.txt)
    ('promo_take7.mp4', 0.8, 2.6, '타이틀'),
    ('promo_take7.mp4', 5.0, 6.5, '로비 → PLAY'),
    ('promo_take7.mp4', 7.3, 9.3, '챕터 선택 → START'),
    ('promo_take7.mp4', 10.2, 11.4, '호스트 선택 — 청룡'),
    ('promo_take7.mp4', 14.1, 15.8, '1번 방 입장'),
    ('promo_take7.mp4', 30.8, 34.0, '스킬 컷인 → 번개 연쇄'),
    ('promo_take7.mp4', 44.3, 46.1, '레벨업 카드'),
    ('promo_take7.mp4', 49.7, 52.2, '4번 방 천사'),
    ('promo_take7.mp4', 113.4, 115.9, '8번 방 악마의 제단'),
    ('promo_take7.mp4', 119.2, 122.7, '중간보스 방 — 번개 연쇄'),
    ('promo_take7.mp4', 166.4, 168.9, '12번 방 상점'),
    ('promo_take7.mp4', 201.3, 208.7, '보스 등장(이름판) → 스킬'),
    ('promo_take7.mp4', 216.8, 219.8, '보스 레이저 피하기'),
    ('promo_take7.mp4', 235.5, 238.4, '마지막 스킬 → 보스 격파'),
    ('promo_take7.mp4', 239.5, 242.0, 'CHAPTER 1 CLEAR'),
]

SEGMENTS_OLD_TAKE4 = [
    ('promo_take4.mp4', 1.0, 3.0, '타이틀'),
    ('promo_take4.mp4', 31.0, 33.0, '로비 → PLAY'),
    ('promo_take4.mp4', 50.3, 52.3, '챕터 선택 → START'),
    ('promo_take4.mp4', 59.1, 60.4, '호스트 선택 → 빙의 시작'),
    ('promo_take4.mp4', 70.4, 74.1, '유령 → 적에게 빙의'),
    ('promo_take4.mp4', 83.5, 85.5, '전투'),
    ('promo_take4.mp4', 87.8, 89.8, '레벨업 카드'),
    ('promo_take4.mp4', 105.3, 108.3, '4번 방 천사 (회복의 제단)'),
    ('promo_take4.mp4', 163.0, 165.5, '8번 방 중간보스'),
    ('promo_take4.mp4', 177.8, 180.3, '악마의 제단 (목숨 건 도전)'),
    ('promo_take4.mp4', 282.2, 285.2, '12번 방 상점'),
    ('promo_take5_boss.mp4', 23.5, 31.0, '보스 — 예고 지대 · 레이저 피하기'),
    ('promo_take5_boss.mp4', 40.5, 44.0, '스킬 컷인 → 보스 격파'),
    ('promo_take5_boss.mp4', 46.5, 49.5, 'CHAPTER 1 CLEAR · 상자 획득'),
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
