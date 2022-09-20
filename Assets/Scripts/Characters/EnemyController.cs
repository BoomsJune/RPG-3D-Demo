using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { GUARD, PATROL, CHASE, DEAD}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStates))]
public class EnemyController : MonoBehaviour, IEndGameObserver
{
    public EnemyState enemyState;
    private NavMeshAgent agent;
    private Animator anim;
    private Collider coll;

    protected CharacterStates characterStates;

    [Header("Basic Settings")]
    public float sightRadius;
    public bool isGuard;

    private float speed;
    protected GameObject attackTarget;

    private float lastAttackTime;

    private Quaternion guardRotation;


    [Header("Patrol State")]
    public float patrolRange;

    private Vector3 wayPoint;
    private Vector3 guardPos;

    bool isWalk;
    bool isChase;
    bool isFollow;
    bool isDead;

    bool playerDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStates = GetComponent<CharacterStates>();
        coll = GetComponent<Collider>();

        speed = agent.speed;
        guardPos = transform.position;
        guardRotation = transform.rotation;
    }

    private void Start()
    {
        if (isGuard)
        {
            enemyState = EnemyState.GUARD;
        }
        else
        {
            enemyState = EnemyState.PATROL;
            GetNewWayPoint();
        }
    }

    private void OnEnable()
    {
        GameManager.Instance.AddObserver(this);
    }

    private void OnDisable()
    {
        if (!GameManager.IsInitialized) return;
        GameManager.Instance.RemoveObserver(this);
    }

    private void Update()
    {
        if (characterStates.CurrentHealth == 0)
        {
            isDead = true;
        }
        if (playerDead) return;
        SwitchState();
        SwitchAnimation();
        lastAttackTime -= Time.deltaTime;
    }

    void SwitchAnimation()
    {
        anim.SetBool("Walk", isWalk);
        anim.SetBool("Chase", isChase);
        anim.SetBool("Follow", isFollow);
        anim.SetBool("Critical", characterStates.isCritical);
        anim.SetBool("Death", isDead);
    }

    void SwitchState()
    {
        if (isDead)
        {
            enemyState = EnemyState.DEAD;
        }
        else if (FoundPlayer())
        {
            enemyState = EnemyState.CHASE;
        }

        switch (enemyState)
        {
            case EnemyState.GUARD:
                isChase = false;
                if(transform.position != guardPos)
                {
                    isWalk = true;
                    agent.isStopped = false;
                    agent.destination = guardPos;

                    if (Vector3.SqrMagnitude(guardPos - transform.position) <= agent.stoppingDistance)
                    {
                        isWalk = false;
                        transform.rotation = Quaternion.Lerp(transform.rotation, guardRotation, 0.01f);
                    }
                }
                break;  
            case EnemyState.PATROL:
                isChase = false;
                agent.speed = speed * 0.5f;
                if (Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    GetNewWayPoint();
                }
                else
                {
                    isWalk = true;
                    agent.destination = wayPoint;
                }
                
                break;
            case EnemyState.CHASE:
                isWalk = false;
                isChase = true;

                agent.speed = speed;
                if (!FoundPlayer())
                {
                    isFollow = false;
                    agent.destination = transform.position;

                    if (isGuard)
                    {
                        enemyState = EnemyState.GUARD;
                    }
                    else
                    {
                        enemyState = EnemyState.PATROL;
                    }
                }
                else
                {
                    isFollow = true;
                    agent.isStopped = false;
                    agent.destination = attackTarget.transform.position;
                }
                // ¹¥»÷·¶Î§ÄÚ¹¥»÷
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped = true;

                    if(lastAttackTime < 0 ){
                        lastAttackTime = characterStates.attackData.coolDown;

                        // ÊÇ·ñ±©»÷
                        characterStates.isCritical = Random.value < characterStates.attackData.criticalChance;
                        // ¹¥»÷
                        Attack();
                    }
                }
                break;
            case EnemyState.DEAD:
                coll.enabled = false;
                //agent.enabled = false;
                agent.radius = 0;

                Destroy(gameObject, 2f);
                break; 
        }

    }

    void Attack()
    {
        transform.LookAt(attackTarget.transform);
        if (TargetInAttackRange())
        {
            // ½üÉí¹¥»÷¶¯»­
            anim.SetTrigger("Attack");
        }
        if (TargetInSkillRange())
        {
            // Ô¶³Ì¹¥»÷¶¯»­
            anim.SetTrigger("Skill");
        }
    }

    bool FoundPlayer()
    {
        var colliders = Physics.OverlapSphere(transform.position, sightRadius);

        foreach(var target in colliders)
        {
            if (target.CompareTag("Player"))
            {
                attackTarget = target.gameObject;
                return true;
            }
        }
        attackTarget = null;
        return false;
    }

    bool TargetInAttackRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStates.attackData.attackRange;
        else return false;
    }

    bool TargetInSkillRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStates.attackData.skillRange;
        else return false;
    }

    void GetNewWayPoint()
    {
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z+randomZ);
        
        NavMeshHit hit;
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, patrolRange, 1) ? hit.position : transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
    }

    void Hit()
    {
        if (attackTarget == null) return;
        if (!transform.IsFacingTarget(attackTarget.transform)) return;

        var targetStates = attackTarget.GetComponent<CharacterStates>();

        targetStates.TakeDamage(characterStates, targetStates);
    }

    public void EndNotify()
    {
        anim.SetBool("Win", true);
        playerDead = true;
        isChase = false;
        isWalk = false;
        attackTarget = null;
    }
}
