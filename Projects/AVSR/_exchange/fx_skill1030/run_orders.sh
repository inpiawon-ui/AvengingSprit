cd /c/won/UnityProject/AvengingSprit/Projects/AVSR/_exchange/fx_skill1030
for k in sup_flare sup_swirl msl_body msl_lock msl_blast; do
  echo "== $k $(date +%H:%M:%S)" >> run_log.txt
  powershell -NoProfile -ExecutionPolicy Bypass -File order_$k.ps1 > log_$k.txt 2>&1
  [ -f sheet_$k.png ] && echo "   납품 $k" >> run_log.txt || echo "   실패 $k" >> run_log.txt
done
echo "끝 $(date +%H:%M:%S)" >> run_log.txt
