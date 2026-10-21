param([string[]]$Sheets)
# 부품 발주를 한 장씩 차례로 돌린다(코덱스 한 번에 한 장). 이미 받은 시트는 건너뛴다.
$ex = 'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange'
$empty = "$ex\ref\rework1021\empty.txt"
foreach ($s in ($Sheets -join ',').Split(',')) {
    if (Test-Path "$ex\in\$s.png") { continue }
    for ($try = 1; $try -le 3; $try++) {
        cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File `"$ex\order_$s.ps1`" < `"$empty`" > `"$ex\log_$s.txt`" 2>&1"
        if (Test-Path "$ex\in\$s.png") { break }
        Start-Sleep -Seconds 60   # 429 등으로 못 받았으면 잠깐 쉬고 다시
    }
    "[ORDER] $s $(if (Test-Path "$ex\in\$s.png") {'ok'} else {'fail'})"
}
