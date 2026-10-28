Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('mock_simple_A_ch1','mock_simple_A_ch3','mock_simple_B_ch1','mock_simple_B_ch3','mock_simple_C_ch1','mock_simple_C_ch3','mock_simple_D_ch1','mock_simple_D_ch3','mock_simple_F_ch1','mock_simple_F_ch3')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
