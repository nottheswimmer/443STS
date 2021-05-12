using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxelMaster;
using Random = UnityEngine.Random;

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
    
    // Sound
    public AudioSource audioSource;
    public AudioClip hitEffect;
    public AudioClip blockBreakingEffect;
    public AudioClip blockPlacingEffect;
    
    // Stats
    public int level = 1;
    public float defense = 1f;
    public float attackPower = 1f;
    public float maxHealth = 100f;
    public float healthRegenRate = 0.01f;
    public float magicRegenRate = 0.01f;
    private float currentHealth;
    public float maxMagic = 100f;
    private float currentMagic;
    public float currentExperience = 0f;
    public float experienceToNextLevel = 100f;

    // Inventory
    private Stack<short> _blockStack;

    public bool IsAttacking()
    {
        return attacking;
    }
    

    private void Start()
    {
        currentHealth = maxHealth;
        currentMagic = maxMagic;
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
        currentHealth = Math.Min(maxHealth, currentHealth + (currentHealth * healthRegenRate * Time.deltaTime));
        currentMagic = Math.Min(maxMagic, currentMagic + (currentMagic * magicRegenRate * Time.deltaTime));
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
            playerVelocity.y = -0.5f;
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
                    audioSource.PlayOneShot(blockPlacingEffect);
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
                var block = terrain.GetBlock(final).id;
                if (block != 4) {  // No bedrock
                    _blockStack.Push(block);
                    terrain.RemoveBlockAt(final);
                    terrain.FastRefresh();
                }
                audioSource.PlayOneShot(blockBreakingEffect);
            }
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 200 * (currentHealth / maxHealth), 20), Math.Round(currentHealth) + " HP");
        GUI.Box(new Rect(10, 35, 200 * (currentMagic/maxMagic), 20), Math.Round(currentMagic) + " MP");
        GUI.Box(new Rect(10, 60, 200, 20), "Level " + level);
    }

    public void GainExperience(float exp)
    {
        Debug.Log("Gained " + exp + " experience points!");
        currentExperience += exp;
        if (experienceToNextLevel <= currentExperience)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentExperience -= experienceToNextLevel;
        experienceToNextLevel += experienceToNextLevel * 0.5f;
        level += 1;
        
        // Roll stats
        attackSpeed += 0.01f;
        healthRegenRate += 0.01f;
        magicRegenRate += 0.01f;
        defense += defense * Random.Range(0f, 0.2f);
        attackPower += attackPower * Random.Range(0f, 0.2f);
        maxHealth += maxHealth * Random.Range(0f, 0.2f);
        maxMagic += maxMagic * Random.Range(0f, 0.2f);
    }

    void Die()
    {
        Debug.Log("You died!");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var mainMenu = SceneManager.GetActiveScene().buildIndex - 1;
        SceneManager.LoadScene(mainMenu);
    }

    public void TakeHit(float enemyPower)
    {
        if (enemyPower == 0)
        {
            return;
        }

        currentHealth -= enemyPower;
        if (currentHealth <= 0)
        {
            Die();
        }
        audioSource.PlayOneShot(hitEffect);
        Debug.Log("currentHealth at " + currentHealth);
    }
}