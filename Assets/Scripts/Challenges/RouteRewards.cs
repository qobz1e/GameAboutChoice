using UnityEngine;

[CreateAssetMenu(menuName = "DiplomaGame/Route coin rewards")]
public sealed class RouteRewards : ScriptableObject
{
    [Min(0)] public int combatCoins = 10;
    [Min(0)] public int peacefulCoins = 10;
    public int Coins(int route) => route == 0 ? combatCoins : peacefulCoins;
}
