Set-Location "C:\won\UnityProject\AvengingSprit"
while (Get-Process -Id 13212 -ErrorAction SilentlyContinue) { Start-Sleep 30 }
"===== baseball ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_baseball.ps1
"===== death ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_death.ps1
"===== commando_laser ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_commando_laser.ps1
"===== amazon ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_amazon.ps1
"===== salamander ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_salamander.ps1
"===== gangster ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_gangster.ps1
"===== ninja_chain ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_ninja_chain.ps1
"===== snowwoman ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_snowwoman.ps1
"===== amazon_elite ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_amazon_elite.ps1
"===== commando_mg ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_commando_mg.ps1
"===== dragoon ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_dragoon.ps1
"===== thug ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_thug.ps1
"===== hopper_smg ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_hopper_smg.ps1
"===== white_wizard ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_white_wizard.ps1
"===== vampire ====="
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_vampire.ps1
"===== end ====="
