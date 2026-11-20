using UnityEngine;
using UnityEngine.Events;

public sealed class ChallengeCompletion : MonoBehaviour
{
    public bool Completed { get; private set; }
    public UnityEvent completed = new UnityEvent();
    public void Complete()
    {
        if (Completed) return;
        Completed = true;
        completed.Invoke();
    }
}
