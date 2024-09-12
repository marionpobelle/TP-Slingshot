using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[SelectionBase]
public class CrosshairController : MonoBehaviour
{
    public event Action<Vector3> OnSlingshotFired;
    public float CurrentStretchAmout; // Set by the providers
    
    [SerializeField] List<EnemyHandler> potentialLockOnEnemies = new List<EnemyHandler>();
    [SerializeField] GameData gameData;
    [SerializeField] EnemyHandler enemyToLock;
    [SerializeField] EnemyHandler lockedOnEnemy;
    [SerializeField] Transform projectileStartPos;
    [SerializeField] float projectileDuration = .3f;
    [SerializeField] AnimationCurve projectileAdditionalHeightCurve;
    [SerializeField] float projectileAdditionalHeightMultiplier = 1;
    [SerializeField] Transform projectileInstance;
    [SerializeField] RotateProjectile rotateProjectile;

    bool isLockedOn = false;
    bool isLockChanging = false;
    bool isShootingProjectile = false;
    float projectileStartTime;
    float enemyLockTimer;
    float nextAllowedFire;
    Vector3 projectileTargetPos;
    Vector3 projectileHitDirection;

    public bool IsLockedOn => isLockedOn;
    public float DistanceFromEnemy;
    public float CurrentEnemyRatio;

    public void Fire()
    {
        if (nextAllowedFire > Time.time)
            return;

        nextAllowedFire = Time.time + gameData.CooldownBetweenShotsInSeconds;

        Debug.Log("Slingshot fired to : " + lockedOnEnemy, this);
        AkSoundEngine.PostEvent("SLG_Fire", gameObject);

        if (lockedOnEnemy)
        {
            lockedOnEnemy.HitEnemy();
            //Launch projectile to enemy
        }

        //Raycast and launch projectile to hit

        ShootProjectile();
        OnSlingshotFired?.Invoke(transform.position);
    }

    void ShootProjectile()
    {
        if (lockedOnEnemy)
        {
            projectileTargetPos = lockedOnEnemy.transform.position;
            //Stop enemy from moving
            //When done, destroy enemy
        }
        else
        {
            projectileHitDirection = Quaternion.Euler(45, 0, 0) * transform.forward;
            if (Physics.Raycast(transform.position + new Vector3(0, 0, 1), Quaternion.Euler(45, 0, 0) * transform.forward, out var hit))
            {
                //Debug.Log("Hit : " + hit.collider.gameObject, this);
                projectileTargetPos = hit.point;
            }
            else
            {
                //Debug.Log("No hit", this);
                projectileTargetPos = transform.position;
            }
        }

        rotateProjectile.StartRotation();
        isShootingProjectile = true;
        projectileStartTime = Time.time;
    }

    private void Update()
    {
        LockOnLogic();
        ProjectileLogic();
    }

    private void ProjectileLogic()
    {
        if (!isShootingProjectile || projectileInstance == null)
            return;

        float posInSimulation = Mathf.InverseLerp(
                    projectileStartTime,
                    projectileStartTime + projectileDuration,
                    Time.time);

        Vector3 projectilePosition =
            Vector3.Lerp(
                projectileStartPos.position,
                projectileTargetPos,
                posInSimulation);

        projectilePosition.y += projectileAdditionalHeightCurve.Evaluate(posInSimulation) * projectileAdditionalHeightMultiplier;

        projectileInstance.position = projectilePosition;

        if (posInSimulation >= 1)
        {
            isShootingProjectile = false;
            projectileInstance.position = projectileStartPos.position;
            rotateProjectile.StopRotation();
            //spawn projectile splatter
        }
    }

    private void LockOnLogic()
    {
        if (isLockedOn)
        {
            //If the enemy is out of the potentially locked enemis but we are still locked on them, begin delocking them
            if (!isLockChanging && !potentialLockOnEnemies.Contains(lockedOnEnemy))
            {
                isLockChanging = true;
                enemyLockTimer = Time.time + gameData.delockDelay;
            }
            //if we were delocking the enemy, but we got it back before the delay
            else if (isLockChanging && potentialLockOnEnemies.Contains(lockedOnEnemy))
            {
                isLockChanging = false;
            }
            //If the enemy is still lockable and the lock on delay is complete
            else if (lockedOnEnemy == null || (isLockChanging && Time.time > enemyLockTimer))
            {
                lockedOnEnemy = null;
                isLockChanging = false;
                isLockedOn = false;
            }
        }
        else
        {
            //If we are not locking on to anyone and one is available
            if (!isLockChanging && potentialLockOnEnemies.Count != 0)
            {
                isLockChanging = true;
                enemyToLock = potentialLockOnEnemies[0];
                enemyLockTimer = Time.time + gameData.lockOnDelay;
                return;
            }
            //If we were locking on to someone but it got away before
            else if (isLockChanging && !potentialLockOnEnemies.Contains(enemyToLock))
            {
                isLockChanging = false;
                enemyToLock = null;
            }
            //If the enemy is still lockable and the lock on delay is complete
            else if (isLockChanging && Time.time > enemyLockTimer)
            {
                lockedOnEnemy = enemyToLock;
                enemyToLock = null;
                isLockChanging = false;
                isLockedOn = true;
            }
        }
    }

    public void AddEnemyToPotentialLockList(EnemyHandler enemyHandler)
    {
        if (!potentialLockOnEnemies.Contains(enemyHandler))
            potentialLockOnEnemies.Add(enemyHandler);
    }

    public void RemoveEnemyFromPotentialLockList(EnemyHandler enemyHandler)
    {
        if (potentialLockOnEnemies.Contains(enemyHandler))
            potentialLockOnEnemies.Remove(enemyHandler);
    }

    public void UpdateDistanceValue(float distanceFromEnemy)
    {
        DistanceFromEnemy = distanceFromEnemy;
    }

    public void UpdateCurrentScaleRatio(float currentEnemyRatio)
    {
        CurrentEnemyRatio = currentEnemyRatio;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(projectileTargetPos, 0.1f);
        Gizmos.DrawRay(projectileTargetPos, projectileHitDirection);
    }
}