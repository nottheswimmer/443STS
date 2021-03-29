using UnityEngine;
using System.Collections;
using System.Numerics;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class WanderingAI : MonoBehaviour {
 
    public float runSpeed = 7;
    public float walkSpeed = 3;
    public float jumpForce = 300;
    public float timeBeforeNextJump = 1.2f;
    public float timeBeforeNextWaypoint = 3.0f;
    public float targetDetectionDist = 5f;
    public float health = 10f;

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
    
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        MoveEntity();
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

        bool targetInRange = !ReferenceEquals(target, null) && targetDetectionDist > Vector3.Distance(anim.transform.position, target.position);
        
        movement = new Vector3(0.4f, 0.0f, 0.4f);
        
        // If the player is in range, run!!!
        if (targetInRange)
        {
            var lookingAt = target.position;
            lookingAt.y = rb.transform.position.y;
            transform.LookAt(lookingAt);
            transform.rotation *= Quaternion.Euler(0,180f,0);
            movement = Vector3.forward * runSpeed;
            movement.y = 0;
            // Reset the waypoint when chased. Seems weird otherwise.
            wayPoint = Vector3.zero;
            canSetNewWayPoint = Time.time + timeBeforeNextWaypoint;
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
            anim.SetInteger("Walk", 1);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 0.15f);
        }
        else {
            anim.SetInteger("Walk", 0);
        }
        
        if (wantsToJump && Time.time > canJump)
        {
            rb.AddForce(0, jumpForce, 0);
            canJump = Time.time + timeBeforeNextJump;
            anim.SetTrigger("jump");
        }
        
        transform.Translate(movement * Time.deltaTime);
        
    }

    public void ApplyDamage(float damage)
    {
        // Hurt it
        health -= damage;
        
        // Force a jump
        rb.AddForce(0, jumpForce, 0);
        canJump = Time.time + timeBeforeNextJump;
        anim.SetTrigger("jump");
        
        // Kill it if it's dead
        if (health <= 0)
        {

            // Detach head and add it to gore object
            var head = gameObject.transform.Find("Head");
            
            // Penguins have no head, strictly speaking
            if (head == null)
                head = gameObject.transform.Find("Body").Find("WingL");
            
            var headParent = Instantiate(goreObject, head.position, Quaternion.identity, goreObject.transform.parent);
            head.SetParent(headParent.transform);
            var bc = headParent.AddComponent<BoxCollider>();
            var mr = head.GetComponent<MeshRenderer>();
            bc.size = new Vector3(
                mr.bounds.size.x * transform.localScale.x,
                mr.bounds.size.y * transform.localScale.y,
                mr.bounds.size.z * transform.localScale.z
                );
            headParent.GetComponent<Rigidbody>().AddForce(Vector3.up * 3.0f);

            // Destroy what's left
            Destroy(gameObject);
        }
    }

}