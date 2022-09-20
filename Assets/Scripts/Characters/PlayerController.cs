using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private CharacterStates characterStates;
    private GameObject attackTarget;
    private float lastAttactTime;

    private bool isDead;

    private float stopDistance;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStates = GetComponent<CharacterStates>();

        stopDistance = agent.stoppingDistance;
    }

    void Start()
    {
        MouseManager.Instance.OnMouseClicked += MoveToTarget;
        MouseManager.Instance.OnEnemyClicked += EventAttack;

        GameManager.Instance.RegisterPlayer(characterStates);
    }

    void Update()
    {
        isDead = characterStates.CurrentHealth == 0;
        if (isDead)
        {
            GameManager.Instance.NotifyObservers();
        }

        switchAnimation();
        lastAttactTime -= Time.deltaTime;
    }

    private void switchAnimation()
    {
        anim.SetFloat("Speed", agent.velocity.sqrMagnitude);
        anim.SetBool("Death", isDead);
    }

    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        if(isDead)return;

        agent.stoppingDistance = stopDistance;
        agent.isStopped = false;
        agent.destination = target;
    }

    private void EventAttack(GameObject target)
    {
        if (target == null) return;

        attackTarget = target;
        characterStates.isCritical = UnityEngine.Random.value < characterStates.attackData.criticalChance;
        StartCoroutine(MoveToAttackTarget());
    }

    IEnumerator MoveToAttackTarget()
    {
        agent.isStopped = false;
        agent.stoppingDistance = characterStates.attackData.attackRange;

        transform.LookAt(attackTarget.transform);

        while(Vector3.Distance(attackTarget.transform.position, transform.position) > characterStates.attackData.attackRange)
        {
            agent.destination = attackTarget.transform.position;
            yield return null;
        }

        agent.isStopped = true;

        if(lastAttactTime < 0)
        {
            anim.SetBool("Critical", characterStates.isCritical);
            anim.SetTrigger("Attack");
            lastAttactTime = characterStates.attackData.coolDown;
        }
    }

    void Hit()
    {
        var targetStates = attackTarget.GetComponent<CharacterStates>();

        targetStates.TakeDamage(characterStates, targetStates);
    }



}
