using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

using System.Data;
using MyNamespace;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using System.Security.Permissions;
using System.Net.Sockets;

public class ChunkManager : MonoBehaviour
{
    public static WorldChunk AirChunk;
    public static float groundHight;
    public static float maxHight =81;///82
    public static int waterLevel = 65;
    public static int ironDepth = 5;
    //  private static readonly float CHUNK_UPDATE_INTERVAL = 0.25f;
  //  public EntityManager entityManager;
    public bool useRandomSeed ;
    public int seed;

    public PlayerController player;
    public Vector3Int loadDistance ;
    Vector3Int loadDistancebigone, loadDistancebigtwo;
    public Vector3Int chunkSize;

    public  Texture2D blockAtlas;

    public WorldChunk ChunkPrefab;
  

 
    public static Dictionary<Vector3Int, WorldChunk> _chunks; // All chunks that have been initialized
   public static Dictionary<Vector3Int, WorldChunk> loadingChunks; // All chunks that have been load

    public static Vector3Int ChunkSize, LoadDistance;

    public HashSet<Vector3Int> LoadChunkQueue;
    public List<WorldChunk> UnloadChunkQueue;

    //  private Vector3Int _prevPlayerChunk;


   public static ChunkManager Instance;

    // public static HashSet<WorldChunk> ModifyChunks = new HashSet<WorldChunk>();

    private void Awake() {
      
        ChunkSize = chunkSize;
        LoadDistance = loadDistance;
        WorldChunk._size = ChunkSize;
        int lw = (loadDistance.x * 2 + 1) * (loadDistance.z * 2 + 1);
        for (int i = 0; i < lw; i++)
        {
            WorldChunk.highMapPool.Enqueue(new PerlinNoisePreCompute[chunkSize.x,chunkSize.z]);
        }
      
     WorldChunk. _sizeSmallOne = new Vector3Int(chunkSize.x - 1, chunkSize.y - 1, chunkSize.z - 1);
        WorldChunk._size = chunkSize;
        WorldChunk. boundCenter = new Vector3(chunkSize.x / 2 - 0.5f, chunkSize.y / 2 - 0.5f, chunkSize.z / 2 - 0.5f);
        WorldChunk. bound = new Bounds(WorldChunk.boundCenter, chunkSize);
        AirChunk = this.GetComponent<WorldChunk>();
        AirChunk.computedTerrainDate = true;
        AirChunk.isEmperty = true;
        AirChunk.isAir = true;
        AirChunk.fullShutoutDown = false;
        AirChunk.NeighborUp = AirChunk;
        AirChunk.Initialized = true;
        AirChunk.Blocks = new BlockType[ chunkSize.x, chunkSize.y, chunkSize.z];
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    AirChunk. Blocks[x, y, z] = BlockType.Air;
                }
            }
        }
      MeshData_. InitCacah();
     Instance =this;
        BlockType.LoadBlockTypes();
    }
    public static AtlasReader _atlasReader;
    private void Start()
    {

        loadDistancebigone = new Vector3Int(loadDistance.x+1, loadDistance.y + 1,loadDistance.z + 1);
        loadDistancebigtwo = new Vector3Int(loadDistance.x + 1, loadDistance.y + 1, loadDistance.z + 1);
        _atlasReader = new AtlasReader(blockAtlas, 8);
        Initialize();
    }

  //  public WorldChunk preChunk, currentChunk;
    public Vector3Int preChunkPos;
    public Vector3Int currentChunkPos;

    public static bool ChangePos;
    public static bool ChangeChunk;
    private void Update()
    {
        if (ChangePos)
            ChangePos = false;
        if (ChangeChunk)
            ChangeChunk = false;
      
        // var ChunkPos = Vector3Int.RoundToInt(player.feet.transform.position);
        //  currentChunkPos = GetNearestChunkPosition(ChunkPos);
        currentChunkPos = GetNearestChunkPosition(GetPlayerPosition());
        if (LoadChunkQueue.Count > 0)
        {
            LoadChunksYield();
          

        }
     


        


    }
    List<Vector2Int> templistkey = new List<Vector2Int>();
    private void FixedUpdate()
    { int c = WorldChunk.hightMapDic.Count -1024;
     //   print(templistkey.Count + "  " + WorldChunk.hightMapDic.Count + "  " + WorldChunk.highMapPool.Count);
        if (c>0)
        {
            int cc = 0;
            foreach (var ky in WorldChunk.hightMapDic)
            {
                cc++;
                templistkey.Add(ky.Key);
                if (cc==c)
                {
                    break;
                }
            }
            for (int i = 0; i < templistkey.Count; i++)
            {
                WorldChunk.highMapPool.Enqueue( WorldChunk.hightMapDic[templistkey[i]]);
                WorldChunk.hightMapDic.Remove(templistkey[i]);
            }
       //     print(templistkey .Count+ "  "+WorldChunk.hightMapDic.Count+"  " + WorldChunk.highMapPool.Count);
            templistkey.Clear();
        }
    }
    private void LateUpdate()
    {

        if (currentChunkPos != preChunkPos)
        {

            ChangeChunk = true;
            UpdateLoadedChunks();

            ChangePos = true;
        }
        ///////////////////////////////////
        if (destroyChunks.Count > 0 || addChunks.Count > 0)
        {
            ModifyAndUpdateBlocks();
        }



       

        if (currentChunkPos != preChunkPos)
            preChunkPos = currentChunkPos;
    }

    private void OnDisable()
    {
        MeshData_.triangles.Dispose();
        GC.Collect();
    }

    public static Random.State prevState;
    public static Vector2Int offset;
    public void Initialize()
    {


        var v = GetPlayerPosition();
      
        currentChunkPos = GetNearestChunkPosition(v);


            for (int i = 0; i < 2048; i++)
        {
            WorldChunk.highMapPool.Enqueue(new PerlinNoisePreCompute[chunkSize.x, chunkSize.z]);
        }

        if (useRandomSeed)
        {
            seed = UnityEngine.Random.Range(int.MinValue,int.MaxValue);
        }
       prevState = Random.state;
        UnityEngine.Random.InitState(seed);
         offset = new Vector2Int(Random.Range(-100, 100), Random.Range(-100, 100));

        _chunks = new Dictionary<Vector3Int, WorldChunk>(2048); // All chunks that have been initialized
   loadingChunks = new Dictionary<Vector3Int, WorldChunk>(2048); // All chunks that have been load

        LoadChunkQueue = new HashSet<Vector3Int>();

        UnloadChunkQueue = new List<WorldChunk>(128 );
     
        TouchedChunks = new HashSet<WorldChunk>();
    
  addChunks = new HashSet<WorldChunk>();
    
        modifyChunks = new HashSet<WorldChunk>();
   
        destroyChunks = new HashSet<WorldChunk>();
    
        newChunks = new HashSet<WorldChunk>();
   
        preChunkPos = Vector3Int.one * int.MaxValue;

        GetInRangeChunkPositions(currentChunkPos, loadDistance);
        
        LoadChunksImmediately();
     
    }
    public int totolVc;
    /// <summary>
    /// Load all the chunks in the chunk load queue immediately in this frame.
    /// (Useful when first loading the scene.)
    /// </summary>
    /// 
    private void LoadChunksImmediately()
    {
        MeshData_.tiem = 0;
  
        for (int i = 0; i < 10000; i++)
        {
         
            WorldChunk chunk = Instantiate<WorldChunk>(ChunkPrefab);
          
           chunk.Blocks = new BlockType[chunkSize.x, chunkSize.y, chunkSize.z];
            chunk.Active();
            chunk.hideFlags = HideFlags.HideInHierarchy;

            WorldChunk.pool.Push(chunk);
            // go.gameObject.SetActive(false);
        }
     
        float f = Time.realtimeSinceStartup;
        foreach (var item in LoadChunkQueue)
      
      
        {
            Vector3Int chunkID = item;

                WorldChunk chunk;
            if (_chunks.TryGetValue(chunkID, out chunk))
            {
             
            }
            else
            {
             
            
               chunk = WorldChunk.GetChunk();
            }

           
            chunk.Initialize(chunkID);

            chunk.InitializeNeighbors();

            chunk.CreateTerrainDate();
            LoadNextChunk(chunk);
          //   */

        }
        LoadChunkQueue.Clear();
        f = Time.realtimeSinceStartup - f;
      Debug.Log(f + "   总时间  "+loadingChunks.Count+" vc "+totolVc);
      
    }

    private void LoadChunksYield()
    {

        foreach (var item in LoadChunkQueue)
     
        {

            LoadNextChunk(GetChunk(item));
          
            
         
        }
        LoadChunkQueue.Clear();
      
    }



    /// <summary>
    /// (105, 69, -111)
    /// Load the next chunk in the chunk load queue.
    /// </summary>
    private bool LoadNextChunk(WorldChunk chunk)
    {
    
            if (chunk.buildMesh == false)
            {
              
              if (!chunk.NeighborUp.fullShutoutDown || !chunk.NeighborUp.fullShutoutDownByUpChunk)
            {
             
                chunk.StartBuildMesh();
                    if (chunk.IsLoaded)
                    {
                 
                    return true;
                   
                }
             
                    return false;
                }
          
            }

        return false;
    }

    public WorldChunk GetChunk(Vector3Int chunkID)
    {
      
        WorldChunk chunk;
        if (!_chunks.TryGetValue(chunkID, out chunk))
        {
         
            chunk = WorldChunk.GetChunk();
            chunk.Initialize(chunkID);
            chunk.InitializeNeighbors();
            chunk.CreateTerrainDate();


        }
        else
        {
         //   Debug.LogError("zzzzzzzzzzzzzzz");
           chunk.Initialize(chunkID);
            chunk.InitializeNeighbors();
            
  
            chunk.CreateTerrainDate();
        }
      
        return chunk;
    }

 //   public static CustomWorldChunkComparer cwc = new CustomWorldChunkComparer();
    public static HashSet<WorldChunk> TouchedChunks;
    public static HashSet<WorldChunk> addChunks;
    public static HashSet<WorldChunk> modifyChunks;
    public static HashSet<WorldChunk> destroyChunks;
    public static HashSet<WorldChunk> newChunks;
  

  //当前更新只支持每帧只能移动一个chunk大小的距离 
  //暂时都放在一帧里处理

    public void UpdateLoadedChunks()
    {
        //
        //    print(preChunkPos+"  " + currentChunkPos);
        for (int dx = -loadDistancebigtwo.x; dx <= loadDistancebigtwo.x; dx += 1)
        {
            for (int dz = -loadDistancebigtwo.z; dz <= loadDistancebigtwo.z; dz += 1)

            {

                if (dx == -loadDistancebigtwo.x || dx == loadDistancebigtwo.x || dz == -loadDistancebigtwo.z || dz == loadDistancebigtwo.z)
                {
                    for (int dy = loadDistancebigtwo.y; dy >= -loadDistancebigtwo.y; dy -= 1)
                    {

                        if (dy * chunkSize.y + preChunkPos.y > maxHight)
                        {
                            continue;
                        }
                        Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        Vector3Int pos = preChunkPos + Vector3Int.RoundToInt(offset);
                        // if (!UnloadChunkQueue.Contains(pos))
                        // {
                        //    UnloadChunkQueue.Add(pos);
                        // }
                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {

                            chunk.removeFlag = true ;
                            UnloadChunkQueue.Add(chunk);


                        }



                    }

                }//关掉变化前位置的chunk lod设置（离相机最近2圈chunk）
                /*
                    else if ((Mathf.Abs(dx) == 2 && Mathf.Abs(dz) <= 2) || (Mathf.Abs(dz) == 2 && Mathf.Abs(dx) <= 2))
                {

                    for (int dy = loadDistance.y; dy >= -loadDistance.y; dy -= 1)
                    {
                        if (dy * chunkSize.y + currentChunkPos.y > maxHight)
                        {
                            continue;
                        }
                        Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        Vector3Int pos = preChunkPos + Vector3Int.RoundToInt(offset);

                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {
                            if (chunk.IsLoaded)
                            {
                                if (chunk.OpaqueMeshRenderer.shadowCastingMode == ShadowCastingMode.On)
                                {
                                    chunk.OpaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                                    chunk.OpaqueMeshRenderer.receiveShadows = false;
                                }
                            }




                        }


                    }



                }
                 */

            }
        }

       

        ////////////////////////////////////////////////////////////////////
        for (int dx = -loadDistance.x; dx <= loadDistance.x; dx += 1)
        {
            for (int dz = -loadDistance.z; dz <= loadDistance.z; dz += 1)

            {
                //以当前chunk位置为中心 加载到最外边的chunk也就是变化的chunk（规定一帧最多加载一个chunk 所以变化的也就是最外边的一圈chunk）
                if (dx == -loadDistance.x || dx == loadDistance.x || dz == -loadDistance.z || dz == loadDistance.z)
                {
                    for (int dy = loadDistance.y; dy >= -loadDistance.y; dy -= 1)
                    {

                        if (dy * chunkSize.y + currentChunkPos.y > maxHight)
                        {
                            continue;
                        }
                        Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        Vector3Int pos = currentChunkPos + Vector3Int.RoundToInt(offset);

                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {
                            chunk.removeFlag = false;
                            if (!chunk.buildMesh)
                            {//这里大多是已经加载了地形数据的邻居块
                                LoadChunkQueue.Add(pos);
                            }
                        }
                        else
                        {//新加载的chunk
                            LoadChunkQueue.Add(pos);


                        }

                    }

                }//离相机最近的2圈chunk
                /*
                      else if (Mathf.Abs(dx) <= 2 && Mathf.Abs(dz) <= 2)
                {
                    
                    for (int dy = loadDistance.y; dy >= -loadDistance.y; dy -= 1)
                    {
                        if (dy * chunkSize.y + currentChunkPos.y > maxHight)
                        {
                            continue;
                        }
                        Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        Vector3Int pos = currentChunkPos + Vector3Int.RoundToInt(offset);
                        //  GameObject.Instantiate(red, pos, Quaternion.identity);

                        if (_chunks.TryGetValue(pos, out WorldChunk chunk))
                        {
                            if (chunk.IsLoaded)
                            {
                                if (chunk.OpaqueMeshRenderer.shadowCastingMode == ShadowCastingMode.Off)
                                {
                                    chunk.OpaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
                                    chunk.OpaqueMeshRenderer.receiveShadows = true;

                                }
                            }
                        }
                    }
                }
                 */

            }
        }

          for (int i = 0; i < UnloadChunkQueue.Count; i++)
            {
            UnloadChunkQueue[i].UnUseSet();
           
           }
         UnloadChunkQueue.Clear();

    }


    private void GetInRangeChunkPositions(Vector3Int centerChunkPos, Vector3Int radius)
    {
       // float maxDistSqrd = Mathf.Pow(radius.x * chunkSize.x, 2f);

        for (int dx = -radius.x; dx <= radius.x; dx += 1)
        {
            for (int dz = -radius.z; dz <= radius.z; dz += 1)

            {
                // for (int dy = -radius.y; dy <= radius.y; dy += 1)
                //从上向下加载
                for (int dy = radius.y; dy >= -radius.y; dy -= 1)
                {
                    if (dy * chunkSize.y + centerChunkPos.y > maxHight)
                    {
                        continue;
                    }
                    Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                    Vector3Int pos = centerChunkPos + Vector3Int.RoundToInt(offset);

                    if (!_chunks.ContainsKey(pos))
                    {
                        LoadChunkQueue.Add(pos);
                    }
                    else
                    {
                        WorldChunk chunk = _chunks[pos];
                        if (!chunk.buildMesh)
                        {
                            LoadChunkQueue.Add(pos);
                        }
                      
                    }
                }
            }
        }
    }




    Vector3Int offset2 = new Vector3Int(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2);
    public Vector3Int GetNearestChunkPosition(Vector3Int pos)
    {
    //   Vector3Int offset = new Vector3Int(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2);
        pos += offset2;

        int xi = pos.x / chunkSize.x;
        int yi = pos.y / chunkSize.y;
        int zi = pos.z / chunkSize.z;

        int x = xi * chunkSize.x;
        int y = yi * chunkSize.y;
        int z = zi * chunkSize.z;

        return new Vector3Int(x, y, z) - offset2;
    }


    public WorldChunk GetNearestChunk(Vector3Int pos)
    {
        Vector3Int nearestPos = GetNearestChunkPosition(pos);
      //  WorldChunk chunk;
        return GetChunk(nearestPos);

 
    }



    public BlockType GetBlockAtPosition(Vector3Int pos,ref WorldChunk chunk)
    {
         chunk = GetNearestChunk(pos);
        if (chunk == null)
        {
      Debug.LogError("加载了错误的chunk");
            return null;
        }
        return chunk.GetBlockAtWorldPosition(pos);
    }


     Vector3Int GetPlayerPosition()
    {
        Vector3Int pos = Vector3Int.CeilToInt(player.transform.position);
        if (pos.y> maxHight)
            pos.y = (int)maxHight;
        return pos;
    }
   
    public static float lod0Dis=60;
    public static float lod1Dis = 120;
    public static   Vector3Int lastAtChunkPos;

    public GameObject gp;

    public GameObject ggg;
    void ModifyAndUpdateBlocks()
    {
        foreach (var chunk in destroyChunks)
        {
            chunk.InitializeNeighbors();
            chunk. loadNeighborsBlocks();
            chunk.DesttroyBlocksFun();
            chunk.DestroyBlocks.Clear();

        }
        foreach (var chunk in TouchedChunks)
        {
           
            if (chunk.buildMesh)
            {
                chunk.InitializeNeighbors();
                chunk.ReBuildMesh();
            }
            else
            {
                //   print(chunk.ID);
                chunk.InitializeNeighbors();
                chunk.CreateTerrainDate(true);
                chunk.StartBuildMesh();
            }
            //  chunk.OpaqueMeshCol.sharedMesh.RecalculateBounds();
            //   chunk.OpaqueMeshCol.sharedMesh.a
            chunk.TouchedBlocks.Clear();
        }

        foreach (var chunk in addChunks)
        {

            if (chunk.buildMesh)
            {
                chunk.ReBuildMesh();
            }
            else
            {
                chunk.InitializeNeighbors();
                chunk.CreateTerrainDate(true);
                chunk.StartBuildMesh();
            }
            //  chunk.OpaqueMeshCol.sharedMesh.RecalculateBounds();
            //   chunk.OpaqueMeshCol.sharedMesh.a
        }
        destroyChunks.Clear();
        TouchedChunks.Clear();
        addChunks.Clear();
    }

 
}
