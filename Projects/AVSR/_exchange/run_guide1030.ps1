$env:OPENAI_API_KEY = $null
$d = "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_possess.ps1" *> "$d\log_guide1030_possess.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_ghost_hp.ps1" *> "$d\log_guide1030_ghost_hp.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_party.ps1" *> "$d\log_guide1030_party.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_main_down.ps1" *> "$d\log_guide1030_main_down.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_synergy.ps1" *> "$d\log_guide1030_synergy.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_guide1030_ultimate.ps1" *> "$d\log_guide1030_ultimate.txt"
"다 끝남"
