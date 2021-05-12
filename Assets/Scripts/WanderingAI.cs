using System;
using UnityEngine;
using System.Collections;
using System.Numerics;
using TMPro;
using TMPro.Examples;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class WanderingAI : MonoBehaviour
{
    public float runSpeed = 7;
    public float walkSpeed = 3;
    public float jumpForce = 300;
    public float timeBeforeNextJump = 1.2f;
    public float timeBeforeNextWaypoint = 3.0f;
    public float targetDetectionDist = 5f;
    public float targetAttackingDist = 2f;
    public float health = 10f;
    public float attackPower = 10f;
    public float experienceValue = 5f;

    // AnimationSet
    public int animationSet = 0;

    public PlayerCamera playerScript;
    public Transform target;
    [FormerlySerializedAs("gore")] public GameObject goreObject;
    private Vector3 wayPoint;
    private float canJump = 0f;
    private float canSetNewWayPoint = 0f;
    Animator anim;
    Rigidbody rb;

    public float jumpFreq = 0.001f;
    public float Range = 10f;
    private Vector3 movement;

    // Sound
    public AudioSource audioSource;
    public AudioClip dyingEffect;
    public AudioClip hitEffect;

    private bool attacking = false;

    // Behavior
    public bool friendly = true;
    
    // Misc
    private string prevAnim = "Idle";
    private double damageCooldown = 0f;
    private bool dead;


    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (dead)
        {
            
            if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "Die")
            {
                Destroy(gameObject);
            }
        }
        else
        {
            MoveEntity();
        }
            
        
    }

    void Wander()
    {
        wayPoint = new Vector3(Random.Range(transform.position.x - Range, transform.position.x + Range),
            1,
            Random.Range(transform.position.z - Range, transform.position.z + Range));
    }

    void MoveEntity()
    {

        bool wantsToJump = false;

        var dist = Double.MaxValue;
        if (!ReferenceEquals(target, null))
            dist = Vector3.Distance(anim.transform.position, target.position);

        bool targetInRange = targetDetectionDist > dist;
        bool targetInAttackingRange = targetAttackingDist > dist;
        var currentAnim = anim.GetCurrentAnimatorClipInfo(0)[0].clip.name;

        if (damageCooldown > 0)
        {
            damageCooldown -= Time.deltaTime;
        }
        
        if (damageCooldown <= 0 && targetInAttackingRange && prevAnim.StartsWith("Attack") && !currentAnim.StartsWith("Attack"))
        {
            playerScript.TakeHit(attackPower);
            damageCooldown = 1f;
        }

        movement = new Vector3(0.4f, 0.0f, 0.4f);

        // If the player is in range, run!!!
        if (!ReferenceEquals(target, null))
        {
            if (targetInRange)
            {
                var lookingAt = target.position;
                lookingAt.y = rb.transform.position.y;
                transform.LookAt(lookingAt);
                if (friendly)
                    transform.rotation *= Quaternion.Euler(0, 180f, 0);
                else if (targetInAttackingRange)
                {
                    attacking = true;

                    if (animationSet == 1)
                    {
                        Debug.Log(currentAnim);
                        if (currentAnim == "Walk W Root")
                        {
                            anim.Play("Idle");
                        }
                        else if (currentAnim == "Attack 01" || currentAnim == "Attack 02")
                        {
                        }
                        else
                        {
                            if (Random.value >= 0.5)
                            {
                                anim.Play("Attack 01");

                            }
                            else
                            {
                                anim.Play("Attack 02");
               
                            }
                        }
                    }
                }
                else
                {
                    attacking = false;
                }

                if (!attacking)
                {
                    movement = Vector3.forward * runSpeed;
                    movement.y = 0;
                    // Reset the waypoint when chased. Seems weird otherwise.
                    wayPoint = Vector3.zero;
                    canSetNewWayPoint = Time.time + timeBeforeNextWaypoint;
                }
            }
        }
        else
        {
            // Set a new waypoint if there isn't one or we're too close
            if ((wayPoint == Vector3.zero || Vector3.Distance(wayPoint, anim.transform.position) < 3)
                && Time.time > canSetNewWayPoint)
            {
                Wander();
                canSetNewWayPoint = Time.time + timeBeforeNextWaypoint;
            }

            // Look a the waypoint and advance if we've got something to do
            if (wayPoint != Vector3.zero)
            {
                wayPoint.y = rb.transform.position.y;
                transform.LookAt(wayPoint);
            }
            // Otherwise make sure there's no movement
            else
            {
                movement = Vector3.zero;
            }
        }


        if (movement != Vector3.zero)
        {
            if (animationSet == 1)
            {
                if (!attacking)
                {
                    if (movement == Vector3.forward * runSpeed)
                    {
                        
                    }
                    else
                    {
                        anim.Play("Walk W Root");
                    }
                }
            }
            else
            {
                anim.SetInteger("Walk", 1);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 0.15f);
        }
        else
        {
            if (animationSet == 1)
            {
            }
            else
                anim.SetInteger("Walk", 0);
        }

        if (wantsToJump && Time.time > canJump)
        {
            rb.AddForce(0, jumpForce, 0);
            canJump = Time.time + timeBeforeNextJump;
            anim.SetTrigger("jump");
        }

        transform.Translate(movement * Time.deltaTime);

        prevAnim = currentAnim;
    }

    public GameObject damageText;

    void ShowFloatingDamage(string text) //The method that shows the floating damage text
    {
        var go = Instantiate(damageText, transform.position,
            Quaternion.identity); //Instantiates the damage number above enemy 
        go.GetComponent<TextMeshProUGUI>().text =
            text; //Gets the TextMesh text component for the floating damage number
    }

    IEnumerator FlashRed(double damage)
    {
        if (animationSet == 1)
            anim.Play("Take Damage");
        ShowFloatingDamage("-" + damage);

        // Flash red

        // Cycle through each child object found with a MeshRenderer
        var colorList = new ArrayList();
        var matList = new ArrayList();
        var duration = 0.8f;

        MeshRenderer[] children = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in children)
        {
            // And for each child, cycle through each material

            foreach (Material mat in rend.materials)
            {
                // Change color
                colorList.Add(mat.GetColor("_Color"));
                matList.Add(mat);
                mat.SetColor("_Color", Color.red);
            }
        }

        var time = 0.0f;
        while (time < duration)
        {
            for (var i = 0; i < matList.Count; i++)
            {
                Material mat = (Material) matList[i];
                mat.SetColor("_Color", Color.Lerp(Color.red, (Color) colorList[i], time / duration));
            }

            time += Time.deltaTime;
            yield return null;
        }

        ShowFloatingDamage("");
    }

    public void ApplyDamage(float damage)
    {
        if (dead)
        {
            return;
        }
        
        // Hurt it
        health -= damage;

        // Force a jump
        if (rb.mass < damage)
        {
            rb.AddForce(0, jumpForce, 0);
            canJump = Time.time + timeBeforeNextJump;
            if (animationSet == 0)
            {
                anim.SetTrigger("jump");
            }
            else
            {
                anim.Play("Jump W Root");
            }
        }

        StartCoroutine(FlashRed(damage));

        // Kill it if it's dead
        if (health <= 0)
        {
            if (animationSet == 1)
            {
                anim.SetBool("Die", true);
                dead = true;
            }
            else
            {
                // Detach head and add it to gore object
                var head = gameObject.transform.Find("Head");

                // Penguins have no head, strictly speaking
                if (head == null)
                {
                    var body = gameObject.transform.Find("Body");
                    if (body != null)
                        head = body.Find("WingL");
                }

                if (head != null)
                {
                    var headParent = Instantiate(goreObject, head.position, Quaternion.identity,
                        goreObject.transform.parent);
                    head.SetParent(headParent.transform);
                    var bc = headParent.AddComponent<BoxCollider>();
                    var mr = head.GetComponent<MeshRenderer>();
                    bc.size = new Vector3(
                        mr.bounds.size.x * transform.localScale.x,
                        mr.bounds.size.y * transform.localScale.y,
                        mr.bounds.size.z * transform.localScale.z
                    );
                    headParent.GetComponent<Rigidbody>().AddForce(Vector3.up * 3.0f);
                }
            }

            // Give the player experience
            playerScript.GainExperience(experienceValue);

            // Destroy what's left
            if (animationSet == 0)
            {
                Destroy(gameObject);
            }

            audioSource.PlayOneShot(dyingEffect);
        }
        else
        {
            audioSource.PlayOneShot(hitEffect);
        }
    }
}