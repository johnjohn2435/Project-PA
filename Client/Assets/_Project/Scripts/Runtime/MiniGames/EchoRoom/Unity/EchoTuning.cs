using UnityEngine;
namespace PocketArcade.EchoRoom.Unity
{
    public sealed class EchoTuning:ScriptableObject
    {
        [SerializeField] private float loopSeconds=10,speed=3.5f,doorGrace=4f;
        [SerializeField] private int rounds=6,target=18;
        public EchoRules Rules()=>new EchoRules{LoopSeconds=loopSeconds,Speed=speed,DoorGrace=doorGrace,Rounds=rounds,Target=target};
    }
}
