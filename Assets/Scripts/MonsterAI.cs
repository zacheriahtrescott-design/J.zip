// MonsterAI.cs
// Port of the Regular-mode monster's wander/chase/catch logic from
// index.html (the "mons.forEach" block). Uses Unity's NavMesh instead
// of the web version's hand-rolled grid BFS, since Unity ships proper
// pathfinding - bake a NavMesh over your map geometry and this will
// use it directly.

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    [Header("Tuning - mirrors the web version's constants")]
    public float wanderSpeed = 1.3f;
    public float chaseSpeed = 3f;
    public float aggroRange = 12f;     // was "AGGRO" in the web version
    public float catchRange = 1.1f;
    public float catchCooldown = 0.9f;
    public float safeZoneRadius = 3f;  // players inside this can't be targeted
    public Vector3 safeZoneCenter;

    [Header("Scene refs")]
    public Transform[] wanderWaypoints; // pick randomly, like randCell() did
    public LayerMask playerLayer;

    NavMeshAgent agent;
    float nextCatchAllowedAt;
    float nextWanderPickAt;
    Vector3 wanderTarget;

    public bool god; // mirrors M.god - set this from your pause/mod menu, never from a client RPC alone

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        PickNewWanderTarget();
    }

    void Update()
    {
        Transform target = FindClosestValidTarget(out bool targetIsPlayer);

        if (target != null)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(target.position);

            float dist = Vector3.Distance(transform.position, target.position);
            if (targetIsPlayer && dist < catchRange && Time.time >= nextCatchAllowedAt)
            {
                nextCatchAllowedAt = Time.time + catchCooldown;
                TryCatch(target);
            }
        }
        else
        {
            agent.speed = wanderSpeed;
            if (Time.time >= nextWanderPickAt || Vector3.Distance(transform.position, wanderTarget) < 1.5f)
                PickNewWanderTarget();
            agent.SetDestination(wanderTarget);
        }
    }

    Transform FindClosestValidTarget(out bool isPlayer)
    {
        isPlayer = false;
        Transform best = null;
        float bestDist = aggroRange;

        var hits = Physics.OverlapSphere(transform.position, aggroRange, playerLayer);
        foreach (var h in hits)
        {
            if (Vector3.Distance(h.transform.position, safeZoneCenter) < safeZoneRadius) continue; // safe zone
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = h.transform;
                isPlayer = true; // extend this to also consider survivor NPCs, as the web version does
            }
        }
        return best;
    }

    void PickNewWanderTarget()
    {
        if (wanderWaypoints != null && wanderWaypoints.Length > 0)
            wanderTarget = wanderWaypoints[Random.Range(0, wanderWaypoints.Length)].position;
        nextWanderPickAt = Time.time + Random.Range(10f, 18f);
    }

    void TryCatch(Transform target)
    {
        var requester = target.GetComponent<CatchRequester>();
        if (requester != null)
            requester.RequestCaught(this); // see CatchRequester.cs - this is the server-sided confirmation step
    }
}
