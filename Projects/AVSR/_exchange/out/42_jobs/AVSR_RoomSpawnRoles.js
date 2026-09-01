// 적 자리 — 레이아웃마다 10자리.                          v1.1 (7 → 10자리, I·J·K·L 추가)
// 정본 EnemyCount + EliteCount 만큼 조합 우선순위로 골라 채운다. 최대 10기(CH3 019 w2).
// 태그: FRONT 앞줄(벽) · RANGED 원거리 · FLANK 측면(추격) · BACK 후방(뺏을 몸)
// 잡몹: SKELETON 해골 · BAT 박쥐 · GUNNER 폐품사수
// 호스트(뺏을 수 있음): AMAZON · CMG 코만도기관총 · CGREN 코만도수류탄 — 레이아웃마다 2기
// 같은 태그 안에서는 이 배열 순서를 지킨다.
module.exports={
 A:[[2.5,9.5,'BACK','AMAZON'],[7,7.5,'RANGED','GUNNER'],[4.5,6.5,'FRONT','SKELETON'],
    [2,3.5,'FRONT','AMAZON'],[5,9.5,'BACK','GUNNER'],[7.5,3.5,'FLANK','BAT'],[1.5,6.5,'FLANK','BAT'],
    [5,4.5,'FRONT','SKELETON'],[8.5,6,'RANGED','GUNNER'],[1.5,2,'FLANK','BAT']],

 B:[[5,10,'BACK','CMG'],[2,7.5,'FLANK','BAT'],[8,7.5,'FLANK','BAT'],[5,6,'RANGED','GUNNER'],
    [2,3.5,'FRONT','SKELETON'],[8,3.5,'FRONT','AMAZON'],[5,4,'FRONT','SKELETON'],
    [5,8.5,'FRONT','SKELETON'],[5,1.5,'RANGED','GUNNER'],[8,10,'FLANK','BAT']],

 C:[[5,9.5,'BACK','CGREN'],[2.5,7.5,'RANGED','GUNNER'],[7.5,7.5,'RANGED','GUNNER'],
    [5,3,'FRONT','SKELETON'],[8,5,'FLANK','BAT'],[2,5,'FLANK','BAT'],[2.5,2.5,'FRONT','SKELETON'],
    [2.5,10,'BACK','CMG'],[7.5,10,'RANGED','GUNNER'],[5,1.5,'FRONT','SKELETON']],

 D:[[2.5,9.5,'BACK','CMG'],[7.5,9.5,'BACK','GUNNER'],[2.5,5.5,'FLANK','SKELETON'],
    [7.5,5.5,'FLANK','BAT'],[5,2.5,'FRONT','SKELETON'],[3.5,2.5,'FRONT','BAT'],[6.5,7.5,'RANGED','GUNNER'],
    [5,10,'BACK','CGREN'],[2.5,7.5,'RANGED','GUNNER'],[8,2.5,'FRONT','SKELETON']],

 E:[[7.5,9.5,'BACK','CMG'],[3.5,8.5,'RANGED','GUNNER'],[3,6.5,'FRONT','SKELETON'],
    [7,4,'FLANK','BAT'],[2,4.5,'FRONT','SKELETON'],[5.5,9.5,'BACK','GUNNER'],[5,3.5,'FRONT','BAT'],
    [2,10,'BACK','CGREN'],[8.5,8,'RANGED','GUNNER'],[5,2,'FRONT','BAT']],

 F:[[4,9.5,'BACK','CMG'],[6.5,9.5,'BACK','GUNNER'],[5,8,'RANGED','GUNNER'],
    [3.5,3.5,'FRONT','SKELETON'],[6.5,3.5,'FRONT','BAT'],[5,5,'FRONT','SKELETON'],[5,2,'FRONT','BAT'],
    [2,7,'FLANK','BAT'],[8,7,'RANGED','GUNNER'],[2,10,'BACK','CGREN']],

 G:[[5,8.5,'BACK','CGREN'],[2.5,9.5,'BACK','GUNNER'],[7.5,9.5,'BACK','GUNNER'],
    [3,5.5,'FRONT','SKELETON'],[7,5.5,'FRONT','BAT'],[1.5,5.5,'FLANK','BAT'],[5,6.5,'RANGED','SKELETON'],
    [5,10,'BACK','CMG'],[8.5,6.5,'RANGED','GUNNER'],[5,3.5,'FRONT','SKELETON']],

 H:[[5,10,'BACK','CMG'],[1.5,8.5,'FLANK','BAT'],[8.5,8.5,'FLANK','SKELETON'],[5,6,'RANGED','GUNNER'],
    [3,3,'FRONT','SKELETON'],[7,3,'FRONT','BAT'],[5,8.5,'RANGED','GUNNER'],
    [2.5,6.5,'FLANK','SKELETON'],[7.5,6.5,'FLANK','BAT'],[2,10,'BACK','CGREN']],

 I:[[5,10,'BACK','CMG'],[2,7.5,'FLANK','BAT'],[8,7.5,'FLANK','BAT'],[2,5,'RANGED','GUNNER'],
    [8,5,'RANGED','GUNNER'],[3.5,3.5,'FRONT','SKELETON'],[6.5,3.5,'FRONT','BAT'],
    [3.5,8,'BACK','CGREN'],[6.5,8,'FLANK','SKELETON'],[5,1.5,'FRONT','BAT']],

 J:[[8,9.5,'BACK','CMG'],[5,9.5,'BACK','GUNNER'],[3,7.5,'RANGED','GUNNER'],[7,7.5,'RANGED','GUNNER'],
    [2,5.5,'FRONT','SKELETON'],[8,5.5,'FLANK','BAT'],[2,3.5,'FRONT','BAT'],
    [5,3.5,'FRONT','SKELETON'],[6.5,10,'FLANK','BAT'],[3.5,10,'BACK','CGREN']],

 K:[[6.5,10,'BACK','CMG'],[2,7,'FLANK','BAT'],[8,7,'RANGED','GUNNER'],[5.5,7.5,'FRONT','SKELETON'],
    [2,5,'FRONT','SKELETON'],[8,5.5,'FRONT','BAT'],[5,3,'RANGED','GUNNER'],
    [3.5,10,'BACK','CGREN'],[6.5,2,'FLANK','BAT'],[8.5,1.5,'FLANK','SKELETON']],

 L:[[3.5,10,'BACK','CMG'],[6.5,10,'BACK','GUNNER'],[2,6.5,'FLANK','BAT'],[8,6.5,'FLANK','BAT'],
    [5,8,'RANGED','GUNNER'],[3.5,2.5,'FRONT','SKELETON'],[6.5,2.5,'FRONT','BAT'],
    [5,4,'FRONT','SKELETON'],[6.5,7.5,'RANGED','GUNNER'],[3.5,7.5,'BACK','CGREN']],
};
