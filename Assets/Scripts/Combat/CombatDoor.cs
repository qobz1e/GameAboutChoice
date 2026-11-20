using UnityEngine;

public sealed class CombatDoor : MonoBehaviour
{
    [SerializeField] CombatChallenge challenge;
    [SerializeField] SpriteRenderer leaf, lamp;
    [SerializeField] Transform bars;
    void Update()
    {
        bool open = challenge.Remaining == 0;
        leaf.color = open ? new Color(.28f,.30f,.34f,.45f) : new Color(.6f,.62f,.67f);
        bars.gameObject.SetActive(!open);
        lamp.color = open ? new Color(1,.73f,.33f) : new Color(.28f,.23f,.18f);
    }
}
