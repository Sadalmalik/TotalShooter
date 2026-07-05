using Sadalmalik.GridNavigation;
using UnityEngine;

public class TEST : MonoBehaviour
{
    public NavGridAgent agent;
    
    public Transform target;

    public bool setTarget;

    private void Update()
    {
        if (setTarget)
        {
            setTarget = false;
            agent.SetDestination(target.position);
        }
    }
}
