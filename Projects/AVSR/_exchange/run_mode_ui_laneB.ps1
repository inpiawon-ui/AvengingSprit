$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($s in @('N1','N3','N7')) { Write-Output "== $s"; & "Projects/AVSR/_exchange/order_mode_ui_$s.ps1" }
Write-Output "LANE DONE"
