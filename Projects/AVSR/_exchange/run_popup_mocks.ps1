Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('A_room','A_levelup','A_altar','A_devil','A_shop','B_room','B_levelup','B_altar','B_devil','B_shop','C_room','C_levelup','C_altar','C_devil','C_shop')
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_popup_mock_$k.ps1' *> 'Projects/AVSR/_exchange/log_popup_mock_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/mock_set_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
