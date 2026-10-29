Set-Location "C:\won\UnityProject\AvengingSprit"
while (Get-Process -Id 191736,186992 -ErrorAction SilentlyContinue) { Start-Sleep 30 }
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_amazon_elite.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_commando_laser.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_dragoon.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_salamander.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_snowwoman.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_thug.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_vampire.ps1
