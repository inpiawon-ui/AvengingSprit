$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
Write-Output "== L"; & "Projects/AVSR/_exchange/order_mode_ui_L.ps1"
Write-Output "== T1"; & "Projects/AVSR/_exchange/order_mode_ui_T1.ps1"
Write-Output "== T5"; & "Projects/AVSR/_exchange/order_mode_ui_T5.ps1"
Write-Output "== T8"; & "Projects/AVSR/_exchange/order_mode_ui_T8.ps1"
Write-Output "== N1"; & "Projects/AVSR/_exchange/order_mode_ui_N1.ps1"
Write-Output "== N3"; & "Projects/AVSR/_exchange/order_mode_ui_N3.ps1"
Write-Output "== N7"; & "Projects/AVSR/_exchange/order_mode_ui_N7.ps1"
