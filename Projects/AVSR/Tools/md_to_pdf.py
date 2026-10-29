"""기획 마크다운 여러 장을 한 권의 PDF 로 묶는다 — PD 는 PDF 로만 본다(2026-10-29).

사용: python Projects/AVSR/Tools/md_to_pdf.py <나갈 PDF> <제목> <md 또는 png> [<md 또는 png> ...]
- md: 제목(#) · 표(|) · 목록(- , 1.) · 굵게(**) · 코드 묶음(```) · 인용(>) 만 바꾼다
- png/jpg: 한 쪽에 그림으로 넣는다
크롬 헤드리스로 인쇄한다. 표는 쪽을 넘어가도 머리줄이 다시 나온다.
"""
import base64
import html
import os
import re
import subprocess
import sys
import tempfile

CHROME = r'C:\Program Files\Google\Chrome\Application\chrome.exe'

CSS = '''
@page { size: A4; margin: 14mm 12mm; }
body { font-family: "Malgun Gothic", sans-serif; font-size: 10.5pt; color: #1b1f2a; line-height: 1.5; }
h1 { font-size: 20pt; color: #0e1428; border-bottom: 3px solid #3fd6ff; padding-bottom: 4px; margin-top: 0; }
h2 { font-size: 14pt; color: #0e1428; border-left: 6px solid #b98cff; padding-left: 8px; margin-top: 18px; }
h3 { font-size: 12pt; color: #3a2a66; margin-top: 14px; }
table { border-collapse: collapse; width: 100%; margin: 8px 0; font-size: 9pt; }
thead { display: table-header-group; }
th { background: #0e1428; color: #fff; padding: 5px; text-align: left; }
td { border-bottom: 1px solid #d5d8e0; padding: 5px; vertical-align: top; }
tr { break-inside: avoid; }
code { background: #eef0f5; padding: 0 3px; border-radius: 3px; font-size: 9pt; }
pre { background: #eef0f5; padding: 8px; font-size: 8.5pt; white-space: pre-wrap; }
blockquote { border-left: 4px solid #ffd56a; margin: 6px 0; padding: 2px 10px; color: #555; }
.doc { break-before: page; }
.doc:first-of-type { break-before: auto; }
.shot { break-before: page; text-align: center; }
.shot img { max-width: 100%; max-height: 250mm; }
.cover { text-align: center; margin-top: 60mm; }
.cover h1 { border: none; font-size: 26pt; }
'''


def inline(s):
    s = html.escape(s)
    s = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', s)
    s = re.sub(r'`([^`]+)`', r'<code>\1</code>', s)
    return s


def md_to_html(text):
    out, lines, i = [], text.splitlines(), 0
    while i < len(lines):
        l = lines[i]
        if l.startswith('```'):
            j = i + 1
            buf = []
            while j < len(lines) and not lines[j].startswith('```'):
                buf.append(html.escape(lines[j])); j += 1
            out.append('<pre>' + '\n'.join(buf) + '</pre>'); i = j + 1; continue
        m = re.match(r'^(#{1,3}) (.*)', l)
        if m:
            n = len(m.group(1)); out.append(f'<h{n}>{inline(m.group(2))}</h{n}>'); i += 1; continue
        if l.startswith('|'):
            rows = []
            while i < len(lines) and lines[i].startswith('|'):
                rows.append(lines[i]); i += 1
            cells = [[c.strip() for c in r.strip().strip('|').split('|')] for r in rows]
            body = [r for r in cells[1:] if not all(re.fullmatch(r':?-+:?', c or '-') for c in r)]
            t = '<table><thead><tr>' + ''.join(f'<th>{inline(c)}</th>' for c in cells[0]) + '</tr></thead><tbody>'
            for r in body:
                t += '<tr>' + ''.join(f'<td>{inline(c).replace("&lt;br&gt;", "<br>")}</td>' for c in r) + '</tr>'
            out.append(t + '</tbody></table>'); continue
        if re.match(r'^\s*([-*]|\d+\.) ', l):
            ordered = bool(re.match(r'^\s*\d+\. ', l))
            tag = 'ol' if ordered else 'ul'
            buf = []
            while i < len(lines) and re.match(r'^\s*([-*]|\d+\.) ', lines[i]):
                buf.append('<li>' + inline(re.sub(r'^\s*([-*]|\d+\.) ', '', lines[i])) + '</li>'); i += 1
            out.append(f'<{tag}>' + ''.join(buf) + f'</{tag}>'); continue
        if l.startswith('>'):
            out.append('<blockquote>' + inline(l.lstrip('> ')) + '</blockquote>'); i += 1; continue
        if l.strip() == '---':
            i += 1; continue
        if l.strip():
            out.append('<p>' + inline(l) + '</p>')
        i += 1
    return '\n'.join(out)


def main():
    pdf, title, items = sys.argv[1], sys.argv[2], sys.argv[3:]
    parts = [f'<div class="cover"><h1>{html.escape(title)}</h1><p>AVSR 빙의 파티 · 2026-10-29</p></div>']
    for p in items:
        if p.lower().endswith(('.png', '.jpg', '.jpeg')):
            data = base64.b64encode(open(p, 'rb').read()).decode()
            kind = 'png' if p.lower().endswith('.png') else 'jpeg'
            parts.append(f'<div class="shot"><h2>{html.escape(os.path.splitext(os.path.basename(p))[0])}</h2>'
                         f'<img src="data:image/{kind};base64,{data}"></div>')
        else:
            parts.append('<div class="doc">' + md_to_html(open(p, encoding='utf-8').read()) + '</div>')
    doc = f'<!doctype html><html lang="ko"><head><meta charset="utf-8"><title>{html.escape(title)}</title><style>{CSS}</style></head><body>' + '\n'.join(parts) + '</body></html>'
    tmp = os.path.join(tempfile.gettempdir(), 'avsr_md_to_pdf.html')
    open(tmp, 'w', encoding='utf-8').write(doc)
    subprocess.run([CHROME, '--headless=new', '--disable-gpu', '--no-pdf-header-footer',
                    f'--print-to-pdf={os.path.abspath(pdf)}', 'file:///' + tmp.replace('\\', '/')],
                   check=True, capture_output=True)
    print(pdf, os.path.getsize(pdf))


if __name__ == '__main__':
    main()
