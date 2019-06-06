﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Combat : MonoBehaviour
{
    public GameObject attackObj;
    public GameManager.Weapon weapon = GameManager.Weapon.PillGun;
    GameObject weaponObj;
    public Transform weaponHolder;
    Animator anim;
    NavMeshAgent agent;

    public float attackSpeed;
    public float attackRange;

    bool attackReady = true;
    bool isPlayer = false;

    void Start() {
        anim = GetComponent<Animator>();
        if(gameObject.tag=="Player") {
            isPlayer=true;
        } else {
            agent = GetComponent<NavMeshAgent>();
        }
        weapon = PickRandomWeapon();
        SetWeapon();
    }

    private void Update() {
        if(isPlayer && attackReady && Input.GetButtonUp("Fire1")) {
            Attack(null);
            anim.SetTrigger("Hit");
            attackReady = false;
        } 
    }

    public void SetWeapon() {
        if(weaponObj) {Destroy(weaponObj);}
        if(weapon==GameManager.Weapon.Machete) {
            weaponObj = Instantiate(Resources.Load("Machete"), transform.position, Quaternion.identity) as GameObject;
            anim.SetLayerWeight(1, 0f);
            anim.SetLayerWeight(2, 1f);
        } else if(weapon==GameManager.Weapon.PillGun) {
            weaponObj = Instantiate(Resources.Load("PillGun"), transform.position, Quaternion.identity) as GameObject;
            anim.SetLayerWeight(1, 1f);
            anim.SetLayerWeight(2, 0f);
        }
        if(!weaponObj) {return;}
        weaponObj.transform.position=weaponHolder.transform.position;
        weaponObj.transform.SetParent(weaponHolder);
        weaponObj.transform.localRotation = Quaternion.identity;
        weaponObj.transform.Rotate(weaponObj.GetComponent<Weapon>().weaponRot);
    }

    public void Attack(GameObject target) {
        if(weaponObj==null) {return;}
        Weapon w = weaponObj.GetComponent<Weapon>();
        if(isPlayer || (attackReady && isInLoS(target))) {
            attackReady=false;
            anim.SetTrigger("Hit");
            StartCoroutine(UseWeapon(target, w)); //Don't want npc to move when in attack range
            if(!isPlayer) {
                agent.isStopped=true;
                agent.SetDestination(transform.position);
            }
        } else {
            agent.isStopped=false;
            agent.SetDestination(target.transform.position);
        }
    }

    IEnumerator UseWeapon(GameObject target, Weapon _w) {
        yield return new WaitForSeconds(_w.speed);
        GameObject attack = Instantiate(_w.attackObj, _w.attackSource.position, Quaternion.identity);
        Attack attackComp = attack.GetComponent<Attack>();
        attackComp.attacker = this.gameObject;
        Vector3 trajectory = Vector3.zero;
        if(target!=null) {
            attack.transform.LookAt(target.transform.position);
            trajectory = ComputeTrajectory(GetDistanceFrom(target), attackComp.velocity, 0f); //Calculate how high to look based on distance and projectile speed
        }
        if(isPlayer) {
            attack.transform.rotation = Camera.main.transform.rotation;
        } else {
            attack.transform.Rotate(trajectory, Space.Self);
        }
        Invoke("ResetAttackTimer", _w.rate);   
    }

    private Vector3 ComputeTrajectory(float x, float v, float y) {
        float discriminant = Mathf.Abs(Mathf.Pow(v, 4)-10*(10*Mathf.Pow(x,2)+2*y*Mathf.Pow(v,2)));
        float denominator = -10*x;
        float numerator = Mathf.Pow(v, 2)-Mathf.Sqrt(discriminant);
        return new Vector3(Mathf.Atan(numerator/denominator) * Mathf.Rad2Deg, 0, 0);
    }

    private void ResetAttackTimer() {
        attackReady = true;
    }

        bool isInLoS(GameObject losTarget) {
        //Check In Range
        if(GetDistanceFrom(losTarget)> weaponObj.GetComponent<Weapon>().range) {
            return false;
        }
        //Check Field of View
        if(Vector3.Angle(GetDirectionTo(losTarget), transform.forward) > 60.0) {
            return false;
        }
        //Check Obstacles
        RaycastHit hit;
        if(Physics.Raycast(transform.position, GetDirectionTo(losTarget), out hit, GetDistanceFrom(losTarget)-2f)) {
            return false;
        }
        return true;
    }

    float GetDistanceFrom(GameObject distTarget) {
        if(distTarget==null) {
            return Mathf.Infinity;
        }
        return Vector3.Distance(transform.position, distTarget.transform.position);
    }

    Vector3 GetDirectionTo(GameObject dirTarget) {
        return dirTarget.transform.position-transform.position;
    }

    GameManager.Weapon PickRandomWeapon() {
        float rand = Random.Range(0f, 2f);
        return rand<=0.2f ? GameManager.Weapon.PillGun : GameManager.Weapon.Machete;
    }

}

