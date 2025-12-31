using UnityEngine;


[RequireComponent(typeof(Army))]
public class ArmyFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float stopDistance = 0.2f;
    
    [Header("Formation")]
    [SerializeField] private Vector2 formationOffset = new Vector2(-0.3f, -0.2f);
    
    // Target to follow
    private Transform followTarget;
    private bool isFollowing;
    
    // Properties
    public bool IsFollowing => isFollowing;
    public Transform FollowTarget => followTarget;
    public Vector2 FormationOffset => formationOffset;
    
    private void Update()
    {
        if (isFollowing && followTarget != null)
        {
            MoveTowardsTarget();
        }
    }
    
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        isFollowing = target != null;
        
        if (isFollowing)
        {
            Debug.Log($"[ArmyFollower] Now following {target.name}");
        }
    }

    public void StopFollowing()
    {
        followTarget = null;
        isFollowing = false;
    }
    

    public void SetFormationOffset(Vector2 offset)
    {
        formationOffset = offset;
    }

    private void MoveTowardsTarget()
    {
        Vector3 targetPos = followTarget.position + (Vector3)formationOffset;
        float distance = Vector3.Distance(transform.position, targetPos);
        
        if (distance > stopDistance)
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            float moveAmount = followSpeed * Time.deltaTime;
            
            // Don't overshoot
            if (moveAmount > distance)
                moveAmount = distance;
            
            transform.position += direction * moveAmount;
        }
    }
    
 
    public void SnapToTarget()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + (Vector3)formationOffset;
        }
    }
}