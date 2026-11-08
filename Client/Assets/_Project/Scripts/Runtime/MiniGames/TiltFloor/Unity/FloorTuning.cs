using UnityEngine;
namespace PocketArcade.TiltFloor.Unity
{
    public sealed class FloorTuning:ScriptableObject
    {
        [SerializeField] private float halfSize=4f,runSpeed=3.8f,tiltPerMeter=9.4f,maxTilt=34;
        [SerializeField] private float firstDrop=2,spawnInterval=3.2f,minSpawnInterval=.18f,rampSeconds=14,warningSeconds=1,dropSeconds=.5f;
        [SerializeField] private float enemySpeed=.65f,enemySlope=.3f,firstBonus=6,bonusInterval=7,bonusLifetime=9;
        [SerializeField] private int maxEnemies=64,maxItems=3,timePointsPerSecond=10,itemPoints=250;
        public FloorRules Rules()=>new FloorRules{HalfSize=halfSize,RunSpeed=runSpeed,TiltPerMeter=tiltPerMeter,MaxTilt=maxTilt,FirstDrop=firstDrop,SpawnInterval=spawnInterval,MinSpawnInterval=minSpawnInterval,RampSeconds=rampSeconds,WarningSeconds=warningSeconds,DropSeconds=dropSeconds,EnemySpeed=enemySpeed,EnemySlope=enemySlope,FirstBonus=firstBonus,BonusInterval=bonusInterval,BonusLifetime=bonusLifetime,MaxEnemies=maxEnemies,MaxItems=maxItems,TimePointsPerSecond=timePointsPerSecond,ItemPoints=itemPoints};
    }
}
