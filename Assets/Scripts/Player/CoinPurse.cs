using UnityEngine;

public sealed class CoinPurse : MonoBehaviour
{
    public int Balance { get; private set; }
    public void Credit(int amount)
    {
        if (amount > 0) Balance += amount;
    }
}
