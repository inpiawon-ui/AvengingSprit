# 시험장 HTML 굽기 — 원본(modelab_src.html)의 /*SPRITES*/ 자리에 유닛 그림(data URI)을 넣는다.
import json, sys, pathlib
here = pathlib.Path(__file__).parent
src = (here / 'modelab_src.html').read_text(encoding='utf-8')
spr = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8'))
(here / 'AVSR_ModeLab.html').write_text(src.replace('/*SPRITES*/{}', json.dumps(spr)), encoding='utf-8')
print('ok', len(src) // 1024, 'KB src')
