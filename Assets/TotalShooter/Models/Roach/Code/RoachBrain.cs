using Sadalmalik.TotalShooter;
using UnityEngine;

[RequireComponent(typeof(NavAgent))]
public class RoachBrain : MonoBehaviour
{
    public NavAgent agent;
    public float remains = 0;

    void Start()
    {
        remains = Time.time + .5f;
        agent = GetComponent<NavAgent>();
    }
    
    void Update()
    {
        if (remains < Time.time)
        {
            remains = Time.time + Random.Range(2f, 8f);
            RefreshTarget();
        }
    }

    private void RefreshTarget()
    {
        int limit = 10;
        while (limit --> 0)
        {
            var offset = Random.insideUnitSphere * 20;
            offset.y = 0;
            if (Physics.Raycast(
                    agent.transform.position + Vector3.up * 5 + offset,
                    Vector3.down * 10,
                    out RaycastHit hit))
            {
                agent.MoveTo(hit.point);
            }
        }
    }
}