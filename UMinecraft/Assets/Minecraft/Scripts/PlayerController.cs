using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using System;
using Random = UnityEngine.Random;
using System.Diagnostics.Eventing.Reader;
using System.Security.Permissions;
using TMPro;
using System.Globalization;



public class PlayerController : MonoBehaviour
{
    public TMP_Text tm;
    public static PlayerController Instance;
    public CharacterController characterController;
    public FlyFree Fly;
    public GameObject plan;
    public Camera camera;
    public MonitorController monitorController;
    public HotbarController hotbarController;
    public PostProcessVolume postProcessVolume;
    public ChunkManager chunkManager;
    public Transform body;
    public Transform feet;
    public AudioSource stepAudioSource;
    public AudioClip teleportClip;
    public AudioClip hitClip;
    public AudioClip fallLandSmallClip;
    public AudioClip fallLandLargeClip;
    public AudioClip fallingClip;
    public GameObject canvas;
    [Range(0.0f, 100f)] public float baseSpeed = 10f;
    [Range(0.0f, 100f)] public float jumpPower = 3f;
    [Range(0.0f, 100f)] public float enderPearlThrowingPower = 10f;
    [Range(0.0f, 1.0f)] public float sidewaysSpeedModifier = 0.5f;
    [Range(0.0f, 2.0f)] public float waterSpeedModifier = 0.5f;
    public GameObject splashEffect;
    public GameObject explosionEffect;
    public ParticleSystem breakEffect;
    public GameObject teleportEffect;
    public GameObject growthEffect;

    public TNT tntPrefab;
    public GameObject enderPearlPrefab;

    public Color waterTintColor = Color.blue;
    //private Color initialSkyColor;
    public Material[] materialsToColor;

 

    private bool _shouldTeleport = false;
    private Vector3 _teleportDestination;
    public float _speed;

    private Vector3Int _prevPosition;
    private Vector3Int _currPosition;
    private BlockType _prevType;

    private Text[] _textComponents;

    private bool _isJumping;
    private float _jumpTimer;
    private float _jumpYVelocity;
    private float _terminalVelocity = 30.0f;
    [SerializeField] private float _jumpDuration;

    [SerializeField] private bool _isInWater;
    //[SerializeField] private bool _cameraIsInWater = false;

    // Mouse
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;

    private float rotY = 0.0f; // rotation around the up/y axis
    private float rotX = 0.0f; // rotation around the right/x axis

    private bool _prevIsGrounded = true;

    private Vector3Int _prevCameraBlockPos;

    private float _movementSincePrevStep = 0.0f;

    private FogSettings _initialFogSettings;

    private class FogSettings
    {
        public Color fogColor;
        public bool fog;
        public FogMode fogMode;
        public float fogDensity;

        public FogSettings()
        {
            this.fogColor = RenderSettings.fogColor;
            this.fog = RenderSettings.fog;
            this.fogMode = RenderSettings.fogMode;
            this.fogDensity = RenderSettings.fogDensity;
        }

        public void Set()
        {
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
        }
    }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }
    void Start()
    {


        TNT.key = tntPrefab.GetHashCode();
      //  ObjPool_fw.SetPoolStartSize<TNT>(tntPrefab, 128);
      //  ObjPool_fw.SetPoolStartSize<Transform>(tntPrefab.explosionEffect.transform, 128);
      Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _textComponents = this.canvas.GetComponentsInChildren<Text>();

        //initialSkyColor = RenderSettings.fogColor;
        _initialFogSettings = new FogSettings();

        _speed = baseSpeed;

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Mouse Movement
        Vector3 rot = camera.transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;

        // PlacePlayerOnSurface(false);

       keycodes = new KeyCode[] {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9
        };
    }

    public void switchControl()
    {

    }

    public void BeginTeleport(Vector3 position)
    {
        _shouldTeleport = true;
        _teleportDestination = position;
        AudioSource.PlayClipAtPoint(hitClip, this.transform.position);
    }

    private void Teleport()
    {
        if (RaycastToBlock(100, out RaycastHit hit, true, out Vector3Int hitBlockPosition))
        {
            Teleport(hitBlockPosition);
        }
        else
        {
            Debug.Log("No block to teleport to :(");
        }

    }
    private void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        Vector3 destination = position + (transform.position - feet.position);
        this.transform.position = position + (transform.position - feet.position);
        GameObject effect = Instantiate(teleportEffect, this.camera.transform.position, Quaternion.identity);
    }

    void PlacePlayerOnSurface(bool playAudio)
    {
        WorldChunk chunk = null;
        Vector3 pos = transform.position;
        pos.y = 100;
        transform.position = pos;
        bool isOnGround = false;
        int iterations = 0;
        while (isOnGround == false)
        {
            iterations++;
            if (iterations > 100)
            {
                Debug.LogWarning("Ground too far from player.");
                break;
            }
            BlockType currBlock = chunkManager.GetBlockAtPosition(Vector3Int.RoundToInt(feet.position), ref chunk);
            if (BlockType.IsAirBlock(currBlock))
            {
                this.transform.position += Vector3.down;
                continue;
            }

            isOnGround = true;
        }

        transform.position += Vector3.up;

        if (playAudio)
        {
            AudioSource.PlayClipAtPoint(this.teleportClip, this.transform.position);
        }
    }

    private void ThrowEnderPearl()
    {
        GameObject go = Instantiate(enderPearlPrefab, camera.transform.position, Quaternion.identity) as GameObject;
        EnderPearlController pearl = go.GetComponent<EnderPearlController>();
        Vector3 cameraDirection = camera.transform.TransformDirection(Vector3.forward);
        Vector3 direction = Vector3.Lerp(cameraDirection, Vector3.up, 0.1f);
        pearl.Initialize(direction * enderPearlThrowingPower, this);
    }


    Vector3 lastPos;
    string m = "Speed  ";
    string m2 = " m/s";
      
    private void FixedUpdate()
    {
       
        if (lastPos!=transform.position)
        {
            float speed = Vector3.Distance(lastPos, transform.position)/Time.fixedDeltaTime;
         //   tm.text = m + speed + m2;
             lastPos = transform.position;
        }
      
    }
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.F1))
        {
            RenderSettings.fog = !RenderSettings.fog;
        }
        HandleHotbarSelection();



      
        
            monitorController.OnBlockTraveled();


            WorldChunk chunkfeet = null;
        // Check if feet in water
      
        Vector3Int pos = Vector3Int.RoundToInt(feet.transform.position);
        BlockType currentType = chunkManager.GetBlockAtPosition(pos, ref chunkfeet);
            // BlockType currentType = currBlock == null ? null : currBlock.type;
            if (_prevType != currentType)
            {
                BlockType blockTypeName = BlockType.IsAirBlock(currentType) ? null : currentType;
                if (blockTypeName != null && blockTypeName.blockName == BlockNameEnum.Water)
                {
                    OnEnterWater();
                }
                else if (!BlockType.IsAirBlock(_prevType) && _prevType.blockName == BlockNameEnum.Water)
                {
                    OnExitWater();
                }

                _prevType = currentType;
            }

            // Check if camera in water
           // currBlock = chunkManager.GetBlockAtPosition(Vector3Int.RoundToInt(camera.position));
            //currentType = currBlock == null ? null : currBlock.type;
            //if (currentType != null && currentType.name == "Water")
            //{
            //    OnCameraEnterWater();
            //} else
            //{
            //    OnCameraExitWater();
            //}



            Vector3Int cameraPos = Vector3Int.RoundToInt(camera.transform.position);
            if (cameraPos != _prevCameraBlockPos)
            {
                WorldChunk chunk = null;
                BlockType prevCameraBlock = chunkManager.GetBlockAtPosition(_prevCameraBlockPos, ref chunk);
                BlockType cameraBlock = chunkManager.GetBlockAtPosition(cameraPos, ref chunk);
                if (BlockType.IsAirBlock(cameraBlock) && (!BlockType.IsAirBlock(prevCameraBlock) && prevCameraBlock.blockName == BlockNameEnum.Water))
                {
                    OnCameraExitWater();
                }
                else if (BlockType.IsAirBlock(prevCameraBlock) && (!BlockType.IsAirBlock(cameraBlock) && cameraBlock.blockName == BlockNameEnum.Water))
                {
                    OnCameraEnterWater();
                }
                _prevCameraBlockPos = cameraPos;
            }

        


        ///////////////////////////////////////////////////////////////////////////

        if (_prevIsGrounded != characterController.isGrounded)
        {
            _prevIsGrounded = characterController.isGrounded;
            if (characterController.isGrounded == false)
            {
                _isJumping = true;
                _jumpTimer = 0.0f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        if (Input.GetMouseButtonDown(1))
        {
            HandleRightMouseClick();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            BreakBlock();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            LaunchTNT();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            ThrowEnderPearl();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            this.ToggleUI();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            postProcessVolume.enabled = !postProcessVolume.enabled;
        }

        if (_isInWater)
        {
            _jumpYVelocity /= 2.0f;
        }

        if (_isJumping)
        {
            _jumpTimer += Time.deltaTime;
        }

        if (characterController.isGrounded&& characterController.enabled)
        {
            if (_isJumping)
            {
                _isJumping = false;
                if (_jumpYVelocity < -15.0f)
                {
                    AudioClip clip = _jumpYVelocity < -29f ? fallLandLargeClip : fallLandSmallClip;
                    AudioSource.PlayClipAtPoint(clip, this.feet.position, 1.0f);
                }

              //  stepAudioSource.volume = 1.0f;
                if (stepAudioSource.isPlaying && stepAudioSource.clip != null)
                {
                    // Stop the falling air sound
                    stepAudioSource.Stop();
                }
            }
            _jumpYVelocity = 0.0f;
        }

        if (_isInWater)
        {
            _jumpTimer = 0.0f;
        }

        if (_isJumping&& characterController.enabled)
        {
            if (_jumpTimer > 0.8f && stepAudioSource.isPlaying == false)
            {
                if (characterController.enabled)
                {
                   
                        stepAudioSource.clip = fallingClip;
                        stepAudioSource.Play();
                    
                  
                }
               
            }
            if (stepAudioSource.clip != null)
            {
                stepAudioSource.volume = Mathf.InverseLerp(0.8f, 2.0f, _jumpTimer);
            }
        }

        if (Input.GetKey(KeyCode.Space) && _isInWater == false && characterController.isGrounded)
        {
            _isJumping = true;
            _jumpTimer = 0.0f;
            _jumpYVelocity = jumpPower;
        }

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            movement += this.transform.forward * _speed;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            movement += -this.transform.forward * _speed;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            movement += -this.transform.right * _speed * sidewaysSpeedModifier;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            movement += this.transform.right * _speed * sidewaysSpeedModifier;
        }


        movement = camera.transform.rotation * movement;

        float mag = movement.magnitude;

        movement.y = 0.0f;
        movement = movement.normalized * mag;

        _movementSincePrevStep += mag * Time.deltaTime;

        float gravity = -30f;
        _jumpYVelocity += gravity * Time.deltaTime;
        _jumpYVelocity = Mathf.Clamp(_jumpYVelocity, -_terminalVelocity, float.MaxValue);

        Vector3 jumpVelocity = Vector3.up * _jumpYVelocity;

        movement += jumpVelocity;

        if (Input.GetKey(KeyCode.Space) && _isInWater)
        {
            movement += Vector3.up * 4f;
        }
        else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift)) && _isInWater && characterController.isGrounded == false)
        {
            movement -= Vector3.up * 4f;
        }
        else if (_isJumping == false && characterController.isGrounded == false)
        {
            movement += Vector3.up * gravity * Time.deltaTime;
        }
        if (characterController.enabled)
        {
            characterController.Move(movement * Time.deltaTime);
        }



        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (characterController.enabled)
            {
                DoMouseRotation();
            }
            
        }

        PlayStepAudio();


    }
    public Vector3 V1;
    public Vector3Int V1I;
    public Vector3 V2;
    public Vector3Int V2I;
    private void LateUpdate()
    {
        V1I = Vector3Int.FloorToInt(V1);
     
        V2I.x =(int) Math.Round(V2.x, MidpointRounding.AwayFromZero);
        V2I.y = Mathf.CeilToInt(V2.y);
        V2I.z = Mathf.FloorToInt(V2.z);
        if (characterController.enabled == false)
        { // Re-enable character controller after having enough time to teleport
           // characterController.enabled = true;
        }
        if (_shouldTeleport)
        {
            Teleport(_teleportDestination);
            _shouldTeleport = false;
            _teleportDestination = Vector3.zero;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            PlacePlayerOnSurface(true);
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            Teleport();
        }
    }

    private void PlayStepAudio()
    {
        if (characterController.enabled == false)
        {
            return;
        }
        WorldChunk chunk = null;
        if (_movementSincePrevStep > 2f)
        {
            _movementSincePrevStep = 0.0f;
            Vector3Int positionBeneathFeet = _currPosition + Vector3Int.down * 2;
            BlockType blockBeneathFeet = chunkManager.GetBlockAtPosition(positionBeneathFeet, ref chunk);
            if (!BlockType.IsAirBlock(blockBeneathFeet))
            {
                AudioClip[] stepClips = blockBeneathFeet.stepClips;
                if (stepClips != null && stepClips.Length > 0)
                {
                    AudioClip clip = stepClips[Random.Range(0, stepClips.Length - 1)];
                    // stepAudioSource.clip = clip;
                    // stepAudioSource.Play();
                    AudioSource.PlayClipAtPoint(clip, feet.position);
                }

            }
        }
    }

    private void SetMaterialColors(Color color)
    {
        foreach (Material material in materialsToColor)
        {
            material.color = color;
        }
    }

    private void OnEnterWater()
    {
        _isInWater = true;
        _speed = baseSpeed * waterSpeedModifier; // Entering water
        GameObject effect = Instantiate(splashEffect, null) as GameObject;
        effect.transform.position = feet.position;
        Destroy(effect, 1.5f);
    }

    private void OnExitWater()
    {
        _isInWater = false;
        _speed = baseSpeed;
        SetMaterialColors(Color.white);
        //Camera.main.backgroundColor = initialSkyColor;
    }

    private void OnCameraEnterWater()
    {
        //SetMaterialColors(waterTintColor);
        camera.backgroundColor = waterTintColor;
        RenderSettings.fog = true;
        RenderSettings.fogColor = waterTintColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.2f;
    }

    private void OnCameraExitWater()
    {
        //SetMaterialColors(Color.white);
        //Camera.main.backgroundColor = initialSkyColor;

        _initialFogSettings.Set();
        //RenderSettings.fog = false;
        //RenderSettings.fogColor = initialSkyColor;
        //RenderSettings.fogMode = FogMode.Linear;
        //RenderSettings.fogStartDistance = 60;
        //RenderSettings.fogEndDistance = 65;
    }

    private void DoMouseRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
       camera.transform.rotation = localRotation;

      body.rotation = camera.transform.rotation;
        Vector3 angles = body.eulerAngles;
        angles.x = 0.0f;
        angles.z = 0.0f;
        body.eulerAngles = angles;
    }

    private void LaunchTNT()
    {
        TNT prefab = Instantiate(tntPrefab, null);
        prefab.gameObject.SetActive(true);
        prefab.transform.position = camera.transform.position + camera.transform.forward;
        TNT tnt = prefab.GetComponent<TNT>();
        tnt.Launch(camera.transform.forward);
        tnt.FireTNT();
    }

    private void ToggleUI()
    {
        foreach (Text text in _textComponents)
        {
            text.gameObject.SetActive(!text.gameObject.activeSelf);
        }
    }





    private void PlaceBlock(BlockType block)
    {
       
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, out RaycastHit hit, true, out blockPos))
        {
            Vector3Int feetPos = Vector3Int.RoundToInt(feet.position);
            if (blockPos == feetPos || blockPos == (feetPos + Vector3Int.up))
            {
                return; // Don't place block in feet or head space
            }
            // List<Vector3Int> positions = new List<Vector3Int>();
            //  List<BlockNameEnum> blocks = new List<BlockNameEnum>();

            //   positions.Add(blockPos);
            //   blocks.Add(block);
            WorldChunk chunk = chunkManager.GetNearestChunk(blockPos);

            //    mb.oldBlock = BlockNameEnum.Air;
            Vector3Int lPos = chunk.WorldToLocalPosition(blockPos);
            //   chunk.AddBlocks.Add(lPos);
            chunk.Blocks[lPos.x, lPos.y, lPos.z] = block;
            ChunkManager.addChunks.Add(chunk);
            PlayBlockSound(block, blockPos);

            monitorController.OnBlockPlaced();
        }
    }

    private void PlaceTNT()
    {
      
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f,out RaycastHit hit, true, out blockPos))
        {
            Vector3Int feetPos = Vector3Int.RoundToInt(feet.position);
            if (blockPos == feetPos || blockPos == (feetPos + Vector3Int.up))
            {
                return; // Don't place block in feet or head space
            }
          
            TNT tnt = ObjPool.GetComponent<TNT>(tntPrefab, blockPos, Quaternion.identity);
            tnt.init();
            // tnt.Launch(camera.forward);

            
            // monitorController.OnBlockPlaced();
        }
    }


    private BlockType GetTargetBlock(float maxDistance)
    {
        Vector3Int blockPos;
        if (RaycastToBlock(maxDistance, out RaycastHit hit, false, out blockPos))
        {
            WorldChunk chunk = null;
            BlockType targetBlock = chunkManager.GetBlockAtPosition(blockPos, ref chunk);
            return targetBlock;
        }

        return null;
    }

    private void PlaceSameBlock()
    {
    
        BlockType targetBlock = GetTargetBlock(10.0f);
        if (BlockType.IsAirBlock(targetBlock))
        {
            return;
        }
        BlockType targetType = targetBlock;

        if (targetType.isPlant)
        {
            return; // Don't place foliage blocks like grass and flowers.
        }

        PlaceBlock(targetType);
    }

    private void BreakBlock()
    {
      
        RaycastHit hit;
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, out  hit, false,out blockPos))
        {
            
            BreakBlock(hit, blockPos);
        }
    }

    public static Vector3 bp;
    public  void BreakBlock(RaycastHit hit, Vector3Int blockPos)
    {


       
    //  Vector3 direction = camera.transform.forward;
     // Vector3 offset = direction *- 0.01f;
   //  hit.point = hit.point + offset;
     //   Vector3Int blockPos = WorldChunk.RoundToInt(hit.point, rayPoint);
    //    Vector3Int blockPos = Vector3Int.RoundToInt(hit.point);
        
      //  blockPos.x = (int)Math.Round(hit.point.x, MidpointRounding.AwayFromZero);
      //  blockPos.y = (int)Math.Round(hit.point.y, MidpointRounding.AwayFromZero);
      //  blockPos.z = (int)Math.Round(hit.point.z, MidpointRounding.AwayFromZero);
        if (hit.collider.CompareTag("TNT"))
            {
                hit.collider.GetComponent<TNT>().FireTNT();
                return;
            }
            // List<Vector3Int> positions = new List<Vector3Int>();
            //   List<BlockNameEnum> blocks = new List<BlockNameEnum>();

            // positions.Add(blockPos);
            //   BlockNameEnum block = BlockNameEnum.Air;
            //   blocks.Add(block);
            WorldChunk chunk = null;
            BlockType breakingBlock = ChunkManager.Instance.GetBlockAtPosition(blockPos, ref chunk);

            if (!BlockType.IsAirBlock(breakingBlock))
        {
         
            ParticleSystem effect = ObjPool.GetComponent<ParticleSystem>(breakEffect, blockPos, Quaternion.identity,0.5f);
            if (breakingBlock== BlockType.Water)
            {
                effect.transform.Translate(0,0.5f,0);
            }
         
            else
            {
                PlayBlockSound(breakingBlock, blockPos);
            }
          


              
           ParticleSystem.MainModule main = effect.main;
            
               main.startColor = breakingBlock.breakParticleColors;
          //  effect.Play();
                monitorController.OnBlockDestroyed();

                Vector3Int localpos = chunk.WorldToLocalPosition(blockPos);

                //  chunk.isExternalBlock[localpos.x, localpos.y, localpos.z] = false;
                if (!chunk.DestroyBlocks.Contains(localpos))
                {
                    chunk.DestroyBlocks.Add(localpos);
                }
                ChunkManager.destroyChunks.Add(chunk);

                if (breakingBlock.blockName == BlockNameEnum.Diamond_Ore)
                {
                    monitorController.OnDiamondMined();
                }

            }


        
    }

    private  static void PlayBlockSound(BlockType blockTypeName, Vector3 position)
    {
        AudioClip clip = blockTypeName.digClip;
        if (clip != null)
        {
            
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }

    private bool RaycastToBlock(in float maxDistance, out RaycastHit hit, in bool getEmptyBlock, out Vector3Int hitBlockPosition)
    {
      
        // Does the ray intersect any objects excluding the player layer
        Vector3 direction = camera.transform.TransformDirection(Vector3.forward);
        if (Physics.Raycast(camera.transform.position, direction, out hit, Mathf.Infinity))
        {
            if (hit.distance <= maxDistance)
            {
                Vector3 offset = getEmptyBlock ? direction * -0.01f : direction * 0.01f;
                hitBlockPosition = Vector3Int.RoundToInt(hit.point + offset);
                return true;
            }
        }

        hitBlockPosition = Vector3Int.zero;
        return false;
    }




    /////////////////////// Hotbar ///////////////////////
    KeyCode[] keycodes;
    private void HandleHotbarSelection()
    {
      
        for (int i = 0; i < keycodes.Length; i++)
        {
            if (Input.GetKeyDown(keycodes[i]))
            {
                hotbarController.SelectItem(i);
                break;
            }
        }
    }

    private void HandleRightMouseClick()
    {
        switch (hotbarController.SeletedItem.Type)
        {
            case SlotItem.SlotItemType.EnderPearl:
                ThrowEnderPearl();
                break;
            case SlotItem.SlotItemType.TNT:
                PlaceTNT();
                // LaunchTNT();
                break;
            case SlotItem.SlotItemType.EndermanHead:
                Teleport();
                break;
            case SlotItem.SlotItemType.BoneMeal:
                UseBoneMeal();
                break;
            case SlotItem.SlotItemType.Stone:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Stone]);
                break;
            case SlotItem.SlotItemType.Sand:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Sand]);
                break;
            case SlotItem.SlotItemType.Gravel:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Gravel]);
                break;
            case SlotItem.SlotItemType.Bedrock:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Bedrock]);
                break;
            case SlotItem.SlotItemType.Dirt:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Dirt]);
                break;
            case SlotItem.SlotItemType.Cobblestone:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Cobblestone]);
                break;
            case SlotItem.SlotItemType.Plank:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Plank]);
                break;
            case SlotItem.SlotItemType.CopyBlock:
                PlaceSameBlock();
                break;
            default:
                PlaceSameBlock();
                break;
        }
    }

    private void UseBoneMeal()
    {
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, out RaycastHit hit, false, out blockPos))
        {
            WorldChunk chunk = null;
            BlockType hitBlock = chunkManager.GetBlockAtPosition(blockPos, ref chunk);
            bool isGrassBlock = !BlockType.IsAirBlock(hitBlock) && hitBlock.blockName == BlockNameEnum.Grass;
            bool isPlantBlock = !BlockType.IsAirBlock(hitBlock) && hitBlock.isPlant;
            if (isGrassBlock == false && isPlantBlock == false)
            {
                return;
            }

            List<Vector3Int> positions = GetBlockPositionsWithinRadius(blockPos, 5);

            //   List<Vector3Int> replacementPositions = new List<Vector3Int>();
            //  List<Block> replacementBlocks = new List<Block>();
            foreach (var pos in positions)
            {
                //  WorldChunk chunk = null;
                BlockType block = chunkManager.GetBlockAtPosition(pos, ref chunk);
                bool isAirBlock = BlockType.IsAirBlock(block);
                if (isAirBlock)
                {
                    BlockType blockBeneath = chunkManager.GetBlockAtPosition(pos + Vector3Int.down, ref chunk);
                    if (!BlockType.IsAirBlock(blockBeneath) && blockBeneath.blockName == BlockNameEnum.Grass && Random.value > 0.5f)
                    {
                        List<BlockType> blockTypeChoices;
                        if (isGrassBlock)
                        {
                            blockTypeChoices = BlockType.GetPlantBlockTypes();
                        }
                        else
                        {
                            blockTypeChoices = new List<BlockType>() { hitBlock }; // Duplicate the selected plant block
                        }

                        BlockType plantBlock = blockTypeChoices[Random.Range(0, blockTypeChoices.Count)];
                        // Block plantBlock = new Block(randomChoice);
                        // replacementBlocks.Add(plantBlock);
                        // replacementPositions.Add(pos);
                     
                    }
                }
            }


          
                GameObject effect = Instantiate(growthEffect, null) as GameObject;
                effect.transform.position = blockPos;
            
        }
    }

    private List<Vector3Int> GetBlockPositionsWithinRadius(Vector3Int center, int radius)
    {
        List<Vector3Int> positions = new List<Vector3Int>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    float dist = Mathf.Sqrt(x * x + y * y + z * z);
                    if (dist <= radius)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z) + center;
                        positions.Add(pos);
                    }
                }
            }
        }
        return positions;
    }



}
