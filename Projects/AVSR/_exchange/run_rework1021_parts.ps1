param([string[]]$Sheets)
# 부품 발주를 한 장씩 차례로 돌린다(코덱스 한 번에 한 장). 이미 받은 시트는 건너뛴다.
$ex = 'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange'
$empty = "$ex\ref\rework1021\empty.txt"
foreach ($s in ($Sheets -join ',').Split(',')) {
    if (Test-Path "$ex\in\$s.png") { continue }
    # ⚠ 이 실행기는 **한 줄로만** 돌린다 — 두 줄을 동시에 돌리면 겹치는 순간 429(짧은 시간 요청 몰림)가 난다(2026-10-22, 24장 중 4장).
    for ($try = 1; $try -le 5; $try++) {
        cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File `"$ex\order_$s.ps1`" < `"$empty`" > `"$ex\log_$s.txt`" 2>&1"
        if (Test-Path "$ex\in\$s.png") { break }
        Start-Sleep -Seconds 120   # 429 등으로 못 받았으면 2분 쉬고 다시
    }
    "[ORDER] $s $(if (Test-Path "$ex\in\$s.png") {'ok'} else {'fail'})"
}
