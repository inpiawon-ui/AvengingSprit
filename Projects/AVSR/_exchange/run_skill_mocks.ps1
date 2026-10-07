Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('commando_missile','guru','ninja_chain','robot','baseball','salamander','dragoon','dragon_blue','white_wizard','medium','snowwoman','vampire','death')
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_skill_mock_$k.ps1' *> 'Projects/AVSR/_exchange/log_skill_mock_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) {
    $f = "Projects/AVSR/_exchange/in/mock_skill_${k}_v1.png"
    if (Test-Path $f) { Write-Output "ok $k" } else { Write-Output "MISSING $k" }
}
