using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VoxelMaster;

public class PlayerCamera : MonoBehaviour
{
    public bool locked = true;
    public VoxelTerrain terrain;

    private Camera _camera;
    private float _lookAnglesx;
    private float _lookAnglesy;
    
    // Cookiecutter
    public CharacterController controller;
    public float playerSpeed = 16f;
    public float movementThreshold = 0.1f;
    public float gravityValue = -9.81f;
    public float jumpHeight = 3f;
    private bool groundedPlayer;
    private Vector3 playerVelocity;
    
    // Weapons and attacking
    public Transform weapon;
    private bool attacking;
    public float attackSpeed = 0.2f;
    public float attackCooldown = 0.3f;
    public float attackDistance = 3.0f;
    public float attackWidth = 1.0f;
    public float placementDistance = 4.0f;
    public float blockBreakDistance = 4.0f;
    
    // Inventory
    private Stack<short> _blockStack;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _blockStack = new Stack<short>();

        // Cookiecutter
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
    }

    private void Update()
    {
        HandleMouse();
        HandleMovement();
        if (!attacking)
        {
            HandleBlocks();
        }

        if (Input.GetMouseButton(0) && !attacking)
        {
            attacking = true;
            StartCoroutine(HandleAttack(
                Quaternion.Euler(80f, 0, 0.0f) 
                * weapon.localRotation, 
                weapon.localPosition + (Vector3.forward * (attackDistance)) 
                                     + (Vector3.left * attackWidth), 
                attackSpeed, 
                MidAttack, 
                weapon.localRotation.eulerAngles,
                weapon.localPosition)
            );
        }
    }

    private void MidAttack(Vector3 originalRot, Vector3 originalPos)
    {
        StartCoroutine(HandleAttack(Quaternion.Euler(originalRot), originalPos, 
            attackCooldown, EndAttack, Vector3.zero, Vector3.zero));
    }

    private void EndAttack(Vector3 ignored, Vector3 ignored2)
    {
        attacking = false;
    }

    IEnumerator HandleAttack(Quaternion endRotation, Vector3 endPosition, float duration, Action<Vector3, Vector3> callback, 
        Vector3 callbackRot, Vector3 callbackPos)
    {
        float time = 0;
        Vector3 startPosition = weapon.localPosition;
        Quaternion startRotation = weapon.localRotation;

        while (time < duration)
        {
            weapon.localRotation = Quaternion.Lerp(startRotation, endRotation, time / duration);
            weapon.localPosition = Vector3.Lerp(startPosition, endPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        weapon.localRotation = endRotation;
        weapon.localPosition = endPosition;
        
        if(callback != null) callback(callbackRot, callbackPos);
    }


    private void HandleMouse()
    {
        var finalLocked = (!Input.GetKey(KeyCode.Tab) && locked);
        Cursor.lockState = (finalLocked ? CursorLockMode.Locked : CursorLockMode.None);
        Cursor.visible = !finalLocked;

        _lookAnglesx += Input.GetAxis("Mouse X");
        _lookAnglesy += Input.GetAxis("Mouse Y");
        _lookAnglesy = Mathf.Clamp(_lookAnglesy, -89, 89);

        transform.eulerAngles = new Vector3(-_lookAnglesy, _lookAnglesx, 0);
    }

    private void HandleMovement()
    {
        
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.1f;
        }

        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (direction.magnitude >= movementThreshold)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camera.transform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * (playerSpeed * Time.deltaTime));
        }

        // Changes the height position of the player..
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
        
    }

    private void HandleBlocks()
    {
        Ray r = _camera.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        RaycastHit hit;
        Physics.Raycast(r, out hit);
        bool collisionArea = !ReferenceEquals(hit.collider, null);

        // if (collisionArea)
        // {
        //     Vector3 final = hit.point + (hit.normal * 0.5f);
        //     if (terrain.GetBlock(final).id != -1)
        //     {
        //         terrain.RemoveBlockAt(final);
        //         terrain.FastRefresh();
        //     }
        // }
        
        
        if (Input.GetMouseButton(0)) // Destroy block
        {
            
            if (collisionArea)
            {
                StartCoroutine(BlockBreakAfterTime(attackSpeed, hit));
            }
        }
        else if (Input.GetMouseButtonDown(1) && _blockStack.Count > 0) // Add block from _blockList
        {
            if (collisionArea)
            {
                Vector3 final = hit.point + (hit.normal * 0.5f);
                if (Vector3.Distance(final, transform.position) <= placementDistance)
                {
                    terrain.SetBlockID(final, _blockStack.Pop());
                    terrain.FastRefresh();
                }
            }
        }
    }
    
    IEnumerator BlockBreakAfterTime(float time, RaycastHit hit)
    {
        yield return new WaitForSeconds(time);
 
        if (!ReferenceEquals(hit.collider, null))
        {
            Vector3 final = hit.point - (hit.normal * 0.5f);
            if (Vector3.Distance(final, transform.position) <= blockBreakDistance)
            {
                _blockStack.Push(terrain.GetBlock(final).id);
                terrain.RemoveBlockAt(final);
                terrain.FastRefresh();
            }
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 200, 20), "Health");
        GUI.Box(new Rect(10, 35, 200, 20), "Magic");
    }
}