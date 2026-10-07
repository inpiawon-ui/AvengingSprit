# 시험판 HTML 굽기 — 원본의 /*SPRITES*/{} 자리에 유닛 그림(data URI)을 넣는다.
#   python build.py sprites.json                 → modelab_src.html  → AVSR_ModeLab.html
#   python build.py sprites.json bossbreak       → bossbreak_src.html → AVSR_BossBreak.html
import json, sys, pathlib
here = pathlib.Path(__file__).parent
name = sys.argv[2] if len(sys.argv) > 2 else 'modelab'
out = {'modelab': 'AVSR_ModeLab.html', 'bossbreak': 'AVSR_BossBreak.html'}[name]
src = (here / f'{name}_src.html').read_text(encoding='utf-8')
spr = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8'))
(here / out).write_text(src.replace('/*SPRITES*/{}', json.dumps(spr)), encoding='utf-8')
print('ok', out, len(src) // 1024, 'KB src')
