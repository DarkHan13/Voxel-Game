using System;
using UnityEngine;
using UnityEngine.UI;
public class FPS_Controller : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    public float flySpeed = 4.0f;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;
    
    // for creation and destuction of blocks
    public World _world;
    public Transform highLigthBlocks;
    public Transform placeBlocks;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Toolbar toolbar;

    [HideInInspector]
    public bool canMove = true;

    public bool canFly = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) _world.inUI = !_world.inUI;
        
        if (!_world.inUI)
        {
            GetPlayerInputs();
            placeCursorBlocks();
        }
    }

    private void placeCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = playerCamera.transform.position + (playerCamera.transform.forward * step);

            if (_world.CheckForVoxel(pos))
            {
                highLigthBlocks.position = new Vector3(Mathf.FloorToInt(pos.x) + 0.5f, Mathf.FloorToInt(pos.y) + 0.5f,
                    Mathf.FloorToInt(pos.z) + 0.5f);
                placeBlocks.position = lastPos + new Vector3(0.5f, 0.5f, 0.5f);
                
                highLigthBlocks.gameObject.SetActive(true);
                placeBlocks.gameObject.SetActive(true);
                
                return;
            }
            
            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y),
                Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }
        highLigthBlocks.gameObject.SetActive(false);
        placeBlocks.gameObject.SetActive(false);
    }

    void GetPlayerInputs()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        
        if (Input.GetKeyDown(KeyCode.E)) canFly = !canFly;
        
        if (!canFly)
        {

            if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
            {
                moveDirection.y = jumpSpeed;
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }
        }
        else
        {
            if (Input.GetButton("Jump"))
                moveDirection.y = flySpeed;
            else if (Input.GetButton("Sprint"))
            {
                moveDirection.y = -flySpeed;
            }
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!characterController.isGrounded && !canFly)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);


        
        // actions with block
        if (highLigthBlocks.gameObject.activeSelf)
        {
            // Destroy block
            if (Input.GetMouseButtonDown(0))
            {
                _world.GetChunkFromVector3(highLigthBlocks.position).EditVoxel(highLigthBlocks.position, 0);
            }
            
            // Create block
            if (Input.GetMouseButtonDown(1))
            {
                try
                {
                    if (toolbar.slots[toolbar.slotIndex].hasItem)
                    {
                        _world.GetChunkFromVector3(placeBlocks.position).EditVoxel(placeBlocks.position,
                            toolbar.slots[toolbar.slotIndex].itemSlot._stack.id);
                        toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("placeBlocks: " + placeBlocks);
                    Debug.Log("placeBlocks.position: " + placeBlocks.position);
                    Console.WriteLine(e);
                }
            }
        }
        
        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}
