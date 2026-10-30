$env:OPENAI_API_KEY = $null
$d = "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_bgfix_holding_floor.ps1" *> "$d\log_bgfix_holding_floor.txt"
powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_bgfix_junkyard_apron.ps1" *> "$d\log_bgfix_junkyard_apron.txt"
"다 끝남"
