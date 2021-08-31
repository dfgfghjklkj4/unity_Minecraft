using MyNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
//using System.Linq;
//using System.Numerics;
using System.Security;
using System.Security.Permissions;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;


public struct PerlinNoisePreCompute
{
    public int waterLevel ;
   // public int ironDepth = 5;
    public int baseNoise;
    public  float ridgeMask;
  public  bool isRavine ;
    //public  bool isInnerRavine;

    public int wpx, wpz;
    public float p1;
    public   void PerlinCompute(int x,  int z )
    {
        waterLevel = 65;
       // Random.State prevState = ChunkManager.prevState;
        //   UnityEngine.Random.InitState(seed);
        // Vector2Int offset = new Vector2Int(Random.Range(-100, 100), Random.Range(-100, 100));
        //  Vector2Int offset = ChunkManager.offset;
        // Random.state = prevState;

        float ox = x / 32f + ChunkManager.offset.x;
        float oz = z / 32f + ChunkManager.offset.y;

        float p0 = Mathf.PerlinNoise(ox, oz);
        p0 = Mathf.Pow(p0, 1.5f);
        if (float.IsNaN(p0) || p0 < 0)
        {
            p0 = 0f;
        }

         baseNoise = Mathf.FloorToInt(p0 * 20);
        baseNoise += 63;

    

        //山脉
         ridgeMask = Mathf.PerlinNoise((x + ChunkManager.offset.x) / 60f, (z + ChunkManager.offset.y) / 60f);

        //   int noise = baseNoise;

        //   bool isLake = baseNoise <= waterLevel;

        if (ridgeMask < 0.3f)
        {
            //  ChunkManager.Instance.c1++;
            float ridgeNoise =WorldChunk. RidgeNoise(x / 20f, z / 20f);
            bool isInnerRavine = ridgeNoise < 0.1f && baseNoise > waterLevel + 1;
            isRavine = ridgeNoise < 0.15f && baseNoise > waterLevel + 1;

            if (isInnerRavine)
            {
             //   noise -= 16;
                baseNoise -= 16;
            }
            else if (isRavine)
            {
                // noise -= Mathf.RoundToInt(16 * (1 - Mathf.InverseLerp(0.1f, 0.15f, ridgeNoise)));
                baseNoise -= Mathf.RoundToInt(16 * (1 - Mathf.InverseLerp(0.1f, 0.15f, ridgeNoise)));
            }
        }


      

      //  p1 = Mathf.PerlinNoise((x + ChunkManager.offset.x) / 6f + 0.5f, (z + ChunkManager.offset.y) / 6f + 0.5f);

        return;
    }


}

public enum BlockFlag:byte
{
    defailt,
    destroy,
    touched,
    add
}

public class BlockTag
{
    public BlockType blcok;
    public BlockType newblcok;
    public bool isVisiable;
    public BlockFlag flag;
    public int VectCount;
    public int verticStartIndex;
    public int Index;
}

public struct BlockPos
{
   public Vector3Int pos;
    public BlockType block;
}


/*
 public sealed class CustomWorldChunkComparer : EqualityComparer<WorldChunk>
{


    public override bool Equals(WorldChunk x, WorldChunk y)
    {
        if (x.ID != y.ID)
            return false;
        return true;
    }

    public override int GetHashCode(WorldChunk obj)
    {
        return obj.ID.GetHashCode();
    }
}

     */

public class WorldChunk : MonoBehaviour
{

    public static Dictionary<Vector2Int, float> highDic = new Dictionary<Vector2Int, float>();
    public Vector3Int ID;

    Vector3Int boundSize, boundSmallOneSize;
    public bool isAir;

    public WorldChunk NeighborUp, NeighborDown, NeighborLeft, NeighborRight, NeighborForward, NeighborBack;


    //   public bool IsModified;


  
    public bool Initialized ;

    public bool fullShutoutDown;//完全遮住下面的chunk
    public bool fullShutoutDownByUpChunk;//完全遮住下面的chunk 是因为上面的chunk遮住了 
    public bool computedTerrainDate;//是否计算过地形数据

    [HideInInspector]
    public bool isEmperty = true;

    public bool active;

    public bool buildMesh;//已经计算过网格 但不一定有网格
    public bool IsLoaded;//计算过并且有网格
    public bool removeFlag;
   

    public MeshFilter OpaqueMeshFilter, WaterMeshFilter, FoliageMeshFilter;
    public MeshRenderer OpaqueMeshRenderer, WaterMeshRenderer, FoliageRenderer;
    public MeshCollider OpaqueMeshCol, WaterMeshCol, FoliageMeshCol;
  
    public BlockType[,,] Blocks;


    // public bool[,,] isExternalBlock;

    public int meshesIndex;

    public MeshData_ coubinMD;
    public static bool[] visibility = new bool[6];
    public static Vector3Int[] neighborsArray = new Vector3Int[6];
    public static bool[] neighborsBlock = new bool[6];



    public static Bounds bound;
  
    public static Vector3 boundCenter ;
  public static Vector3Int _size;
    public static Vector3Int _sizeSmallOne;
  
    public HashSet<Vector3Int> AddBlocks ;
    public HashSet<Vector3Int> TouchedBlocks ;
    public List<Vector3Int> DestroyBlocks ;

    // public List<MeshData> ModifyMeshDate = new List<MeshData>();
  //public PerlinNoisePreCompute[,] highMap;//计算x z坐标使用的高度图数据 计算y时不用再计算了 
    public static Queue<PerlinNoisePreCompute[,]>  highMapPool=new Queue<PerlinNoisePreCompute[,]>(2048);
    public static Dictionary<Vector2Int, PerlinNoisePreCompute[,]> hightMapDic = new Dictionary<Vector2Int, PerlinNoisePreCompute[,]>(1024);

        public void UnUseSet()
    {
        if (removeFlag==false)
        {
         return;
        }
        removeFlag = false;
        if (!isAir)
        {
          

            if (OpaqueMeshFilter.sharedMesh.vertexCount>0)
            {
              //OpaqueMeshCol.enabled = false;
                OpaqueMeshRenderer.renderingLayerMask = 0;
                OpaqueMeshFilter.sharedMesh.Clear();
            }
            if (WaterMeshFilter.sharedMesh.vertexCount > 0)
            {
              //WaterMeshCol.enabled = false;
                WaterMeshRenderer.renderingLayerMask = 0;
                WaterMeshFilter.sharedMesh.Clear();
            }
            if (FoliageMeshFilter.sharedMesh.vertexCount > 0)
            {
              //FoliageMeshCol.enabled = false;
                FoliageRenderer.renderingLayerMask = 0;
                FoliageMeshFilter.sharedMesh.Clear();
            }
      
            //  uu++;       
        //    if (highMap!=null)
       //   {
         //  highMapPool.Enqueue(highMap);
                
        //       highMap = null;
       //    }
            Initialized = false;

            fullShutoutDown = false; ;
            fullShutoutDownByUpChunk = false;
            computedTerrainDate = false;


            isEmperty = true;

          //  active = false;

            buildMesh = false;
            IsLoaded = false;
            RemoveNeighbors();
            pool.Push(this);
            ChunkManager._chunks.Remove(ID);
        }

       
    }

    void RemoveNeighbors()
    {
        if (NeighborUp)
        {
            NeighborUp.NeighborDown = null;
            NeighborUp = null;
        }
        if (NeighborDown)
        {
            NeighborDown.NeighborUp = null;
            NeighborDown = null;
        }
        if (NeighborLeft)
        {
            NeighborLeft.NeighborRight = null;
            NeighborLeft = null;
        }
        if (NeighborRight)
        {
            NeighborRight.NeighborLeft = null;
            NeighborRight = null;
        }
        if (NeighborForward)
        {
            NeighborForward.NeighborBack = null;
            NeighborForward = null;
        }
        if (NeighborBack)
        {
            NeighborBack.NeighborForward = null;
            NeighborBack = null;
        }
    }

    public void StartBuildMesh()
    {
      
        if (isEmperty)
        
            return;
        
       
        //六面邻居的地形数据加载
        loadNeighborsBlocks();
    
     BuildOpaqueMesh();
   BuildWaterMesh();
   BuildFoliageMesh();
     
     
       // if (IsLoaded)
       //  gameObject.SetActive(true);

            buildMesh = true;
       
    }
    public bool IsRebuildOpaqueMesh, IsRebuildWaterMesh, IsRebuildFoliageMesh;
    public void ReBuildMesh()
    {

     if (isEmperty)
           return;
        //六面邻居的地形数据加载
     loadNeighborsBlocks();
    
     //   if(IsRebuildOpaqueMesh)
        BuildOpaqueMesh();
     //   if (IsRebuildWaterMesh)
          BuildWaterMesh();
     //   if (IsRebuildFoliageMesh)
          BuildFoliageMesh();


      //  if (IsLoaded)
          //  gameObject.SetActive(true);

        buildMesh = true;
     

    }


    public void Initialize(Vector3Int minCorner)
    {
      
     

        if (Initialized)
      
            return ;

        ID = minCorner;
        this.transform.position = minCorner;

        isEmperty = true;

     
        if (!active)
        {

            AddBlocks = new HashSet<Vector3Int>();
            TouchedBlocks = new HashSet<Vector3Int>();
            DestroyBlocks = new List<Vector3Int>(512);



            // gameObject.name = ID.ToString();
            Mesh m1 = new Mesh();
            OpaqueMeshFilter.sharedMesh = m1;
            //    dataArray = Mesh.AllocateWritableMeshData(1);
            // data = dataArray[0];
            OpaqueMeshCol.sharedMesh = OpaqueMeshFilter.sharedMesh;




            Mesh m2 = new Mesh();
            WaterMeshFilter.sharedMesh = m2;
            // WaterMeshCol = WaterMeshFilter.GetComponent<MeshCollider>();
            WaterMeshCol.sharedMesh = WaterMeshFilter.sharedMesh;

            Mesh m3 = new Mesh();
            FoliageMeshFilter.sharedMesh = m3;
            //  FoliageMeshCol = FoliageMeshFilter.GetComponent<MeshCollider>();
            FoliageMeshCol.sharedMesh = FoliageMeshFilter.sharedMesh;
            active = true;
        }

        //  ModifyBlock=GetMBList();
        //  ModifyPos = GetMBPosList(out mbIndex);

        if (Blocks == null)
        {

            Blocks = new BlockType[ChunkManager.Instance. chunkSize.x, ChunkManager.Instance.chunkSize.y, ChunkManager.Instance.chunkSize.z];


        }
      //  Blocks = new BlockType[_size.x, _size.y, _size.z];
      
        Initialized = true;

        Vector3Int wp = new Vector3Int(minCorner.x, minCorner.y + ChunkManager.Instance.chunkSize.y, minCorner.z);
        // /*
        if (!NeighborUp)
        {
            if (minCorner.y < ChunkManager.maxHight - ChunkManager.Instance.chunkSize.y)
            {

                
                if (!ChunkManager._chunks.TryGetValue(wp, out NeighborUp))
                {
                    NeighborUp = WorldChunk.GetChunk();
                    NeighborUp.Initialize(wp);

                }

            }
            else
            {
                NeighborUp = ChunkManager.AirChunk;
            }
        }
    
        NeighborUp.NeighborDown = this;


     //  if (!ChunkManager._chunks.ContainsKey(minCorner))
         //  {
        ChunkManager._chunks.Add(minCorner, this);
    // }
      
     
        return ;
    }



    //获取邻居块
    public void InitializeNeighbors( )
    {
      
        Vector3Int wp;
      
              if (!NeighborDown)
        {
            wp = new Vector3Int(ID.x, ID.y- ChunkManager.Instance.chunkSize.y, ID.z );
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborDown))
            {
                NeighborDown = WorldChunk.GetChunk();
                NeighborDown.Initialize(wp);
            }

        }
        NeighborDown.NeighborUp = this;
     //    */


        if (!NeighborForward)
        {
            wp = new Vector3Int(ID.x, ID.y, ID.z + ChunkManager.Instance.chunkSize.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborForward))
            {
                NeighborForward = WorldChunk.GetChunk();
                NeighborForward.Initialize(wp);
            }

        }
        NeighborForward.NeighborBack = this;
        if (!NeighborBack)
        {
            wp = new Vector3Int(ID.x, ID.y, ID.z - ChunkManager.Instance.chunkSize.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborBack))
            {
            
                NeighborBack = WorldChunk.GetChunk();
                NeighborBack.Initialize(wp);
            }

        }
        NeighborBack.NeighborForward = this;
        if (!NeighborLeft)
        {
            wp = new Vector3Int(ID.x - ChunkManager.Instance.chunkSize.x, ID.y, ID.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborLeft))
            {
                NeighborLeft = WorldChunk.GetChunk();
                NeighborLeft.Initialize(wp);
            }

        }
        NeighborLeft.NeighborRight = this;

        if (!NeighborRight)
        {
            wp = new Vector3Int(ID.x + ChunkManager.Instance.chunkSize.x, ID.y, ID.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborRight))
            {
                NeighborRight = WorldChunk.GetChunk();
                NeighborRight.Initialize(wp);
            }

        }
        NeighborRight.NeighborLeft = this;

        return;
    }



    //0.0005
    public void CreateTerrainDate(bool ignorShutoutDown=false)
    {
       
        if (computedTerrainDate)

            return;

        if (!ignorShutoutDown)
        {
            //这里只考虑了顶部被完全遮挡的情况 如果靠近相机的时候还要考虑四周是否被完全遮挡住 的情况
            if (NeighborUp.fullShutoutDown || NeighborUp.fullShutoutDownByUpChunk)
            {
                fullShutoutDownByUpChunk = true;
               //  isEmperty = true;
                return;
            }

        }

        bool eee = false;

        fullShutoutDown = true;
        // isEmperty = true;
      //  PerlinNoisePreCompute[,] highMap;
      
          Vector2Int mapid = new Vector2Int(ID.x,ID.z);

     PerlinNoisePreCompute[,] highMap ;
        if (hightMapDic.TryGetValue(mapid,out highMap))
        {

        }
        else
        {
            if (highMapPool.Count>0)
            {
                highMap = highMapPool.Dequeue();
            }
            else
            {
                highMap = new PerlinNoisePreCompute[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];
                Debug.LogError("xxxxxxxxx");
            }
            //与高度无关的计算只计算一次
            for (int x = 0; x < _size.x; x++)
            {
                int wpx = x + ID.x;
                for (int z = 0; z < _size.z; z++)
                {
                    int wpz = z + ID.z;
                  
                    // pnpc.PerlinCompute(wpx, wpz);
                   
                        PerlinNoisePreCompute pnpc2 = new PerlinNoisePreCompute();
                        pnpc2.PerlinCompute(wpx, wpz);
                    pnpc2.wpx = wpx;
                    pnpc2.wpz = wpz;
                    highMap[x, z] = pnpc2;
                
                }
            }

            hightMapDic.Add(mapid, highMap);
        }
         
      //   */

        PerlinNoisePreCompute pnpc ;
        bool e = true; ;
        for (int x = 0; x <  _size.x; x++)
        {
          //  int wpx = x + ID.x;
            for (int z = 0; z < _size.z; z++)
            {
          //    int wpz = z + ID.z;
                pnpc = highMap[x, z];
             

                for (int y = _sizeSmallOne.y; y > -1; y--)
                //for (int y = 0; y < _size.y; y++)
                {
                    BlockType type = GetBlockType(pnpc.wpx, y + ID.y, pnpc. wpz, pnpc);
                    if (type!= BlockType.Air)
                    {
                        if (!type.isTransparent)
                        {
                            if (!eee)

                                eee = true;

                        }
                        if (isEmperty)
                        {
                            isEmperty = false;
                        }
                    }
                    Blocks[x, y, z] = type;
                }
                if (eee)
                {
                    //发现了不透明的方块 
                    eee = false;
                
                }
                else
                {
               
                    //如果整个竖排从下到上都没有不透明的方块 就不能完全遮住下面的chunk
                    if (fullShutoutDown)
                        fullShutoutDown = false;
                }


            }
        }
    
        computedTerrainDate = true;
    
     
    }





    public void Active()
    {
      if (!active)
        {
        
               AddBlocks = new HashSet<Vector3Int>();
               TouchedBlocks = new HashSet<Vector3Int>();
             DestroyBlocks = new List<Vector3Int>(512);



            // gameObject.name = ID.ToString();
            Mesh m1 = new Mesh();
           OpaqueMeshFilter.sharedMesh = m1;
        //    dataArray = Mesh.AllocateWritableMeshData(1);
           // data = dataArray[0];
              OpaqueMeshCol.sharedMesh = OpaqueMeshFilter.sharedMesh;




            Mesh m2 = new Mesh();
           WaterMeshFilter.sharedMesh = m2;
            // WaterMeshCol = WaterMeshFilter.GetComponent<MeshCollider>();
            WaterMeshCol.sharedMesh = WaterMeshFilter.sharedMesh;

            Mesh m3 = new Mesh();
              FoliageMeshFilter.sharedMesh = m3;
            //  FoliageMeshCol = FoliageMeshFilter.GetComponent<MeshCollider>();
            FoliageMeshCol.sharedMesh = FoliageMeshFilter.sharedMesh;
            active = true;
        }
      
    }

  
  

    public static float RidgeNoise(float x, float y) {
        return Mathf.Abs(Mathf.PerlinNoise(x,y)-0.5f) * 2.0f;
    }


    


     BlockType GetBlockType(int x, int y, int z, PerlinNoisePreCompute pnpc)
    {

    
        int baseNoise = pnpc.baseNoise;

        BlockType type = BlockType.Air;
        int waterLevel = pnpc.waterLevel;
        // int waterLevel = 65;
    //  int ironDepth = 5;

        //山脉
      //  float ridgeMask = pnpc.ridgeMask;


        bool isLake = baseNoise <= waterLevel;

        bool isRavine = false;
     
        if (y <= 0)
        {
         
           // type.blockName = BlockNameEnum.Bedrock;
            type = BlockType.Bedrock;
        }
        else if (y >= baseNoise - 8 && y <= baseNoise && isLake && isRavine == false)
        {
       //     ChunkManager.Instance.c2++;

            type = (y >= baseNoise - 3) ? BlockType.Sand : BlockType.Sandstone;
        }
        else if (y < baseNoise - 3 || (isRavine && y < baseNoise && y < baseNoise))
        {
            //   ChunkManager.Instance.c3++;
            type = BlockType.Stone;
            return type;
            if (y < baseNoise)
            {
                //  ChunkManager.Instance.c3++;
                //  type = BlockType.Air;

                if (y < baseNoise - 35 && Random.value < 0.001f)
                {
                    // ChunkManager.Instance.c1++;
                    type = BlockType.Diamond_Ore;
                }

                else if (type == BlockType.Air && y <= baseNoise - 6)
                {

                    // ChunkManager.Instance.c2++;
                    //float p1 = Mathf.PerlinNoise((x + ChunkManager.offset.x) / 6f + 0.5f, (z + ChunkManager.offset.y) / 6f + 0.5f);
                    float p1 = pnpc.p1;

                    float p2 = Mathf.PerlinNoise(y / 6f, 0);
                    float p3 = p1 + p2;
                    if (p3 > 1.3f)
                    {
                        type = BlockType.Gravel;
                    }
                }

                else if (type == BlockType.Air && y <= baseNoise - 5)
                {
                    //  ChunkManager.Instance.c3++;
                    //  ChunkManager.Instance.c1++;
                    float p1 = Mathf.PerlinNoise((x + ChunkManager.offset.x) / 4f + 100, (z + ChunkManager.offset.y) / 4f + 100);
                    //  float p1 = pnpc.pp1;
                    float p2 = Mathf.PerlinNoise(y / 4f, 0);

                    float p3 = p1 + p2;
                    if (p3 > 1.4f)
                    {
                    //    GameObject.Instantiate(chunkManager.blue, new Vector3(x, y, z), Quaternion.identity).transform.SetSiblingIndex(0);
                        //  ChunkManager.Instance.c4++;
                        type = BlockType.Iron_Ore;
                    }

                }
                if (type == BlockType.Air)

                    type = BlockType.Stone;

                // type = type ?? BlockType.GetBlockType(BlockNameEnum.Stone);
            }


        }

        else if (isLake && y <= waterLevel && isRavine == false)
        {
          //  ChunkManager.Instance.c4++;

            type = BlockType.Water;
        }

        else if (y < baseNoise)
        {
            type = BlockType.Dirt;
        }

        else if (y == baseNoise && y > waterLevel)
        {
            type = BlockType.Grass;
        }
        else if (y == baseNoise + 1 && y > waterLevel + 1 && isRavine == false)
        {
           float plantProbability = 1f - Mathf.InverseLerp(waterLevel, waterLevel + 30, y);
            plantProbability = Mathf.Pow(plantProbability, 7f);
           plantProbability*=0.5f;

            if (Random.value < plantProbability)
            {

                float p = Random.value;

                type = BlockType.Tall_Grass;

                if (p < 0.25f)
                {
                    var plantTypes = BlockType.GetPlantBlockTypes();
                    type = plantTypes[Random.Range(0, plantTypes.Count)];
                }


            }
        }

     

       return type;
    }


   
    public void BuildOpaqueMesh()
    {
        meshesIndex = 0;
         coubinMD = MeshData_.GetMax();
      for (int x = 0; x < _size.x; x++)
        {
            for (int z = 0; z < _size.z; z++)
            {
            for (int y = 0; y< _size.y; y++)
                    {
                    BlockType block = Blocks[x, y, z];
                  
                     if (block.isTransparent)
                         continue;
                  
                        bool quauCount = GetVisibility(x,y,z,  visibility);
                        if (quauCount )
                        {

                            int index = meshesIndex;
                            int vc = block.GenerateCubeFaces(visibility, ChunkManager._atlasReader,  coubinMD, ref meshesIndex);
                            for (int ii = 0; ii < vc; ii++)
                            {
                                int newIndex = index + ii;
                                Vector3 vp = coubinMD.vertexDate[newIndex].vertice;
                                vp.Set(vp.x+x,vp.y+y,vp.z+z);
                                coubinMD.vertexDate[newIndex].vertice = vp ;

                              
                            }
                        }
                }
            }
        }

            if (meshesIndex == 0)
            {

            MeshData_.ReturnMax(coubinMD);
            return ;
        }

        IsLoaded = true;


     
        Mesh mesh = OpaqueMeshFilter.sharedMesh;
        MeshData_.Combine(this, ref mesh);
   
        OpaqueMeshCol.sharedMesh = OpaqueMeshFilter.sharedMesh;
      
        ChunkManager.Instance.totolVc += OpaqueMeshFilter.sharedMesh.vertexCount;
        MeshData_.ReturnMax(coubinMD);
        //  if (!OpaqueMeshRenderer.gameObject.activeSelf)
        // OpaqueMeshRenderer. gameObject.SetActive(true);

        if (OpaqueMeshRenderer.renderingLayerMask == 0)
            OpaqueMeshRenderer.renderingLayerMask = 1;
        return ;
    }


    public void BuildWaterMesh()
    {
        meshesIndex = 0;
        coubinMD = MeshData_.GetMax();


        //  /*
        for (int x = 0; x < _size.x; x++)
        {
            for (int z = 0; z < _size.z; z++)
            {
                 for (int y = 0; y< _size.y; y++)
                {
                    BlockType block = Blocks[x, y, z];
                    if (!block.isWater)
                        continue;
                    bool quauCount = GetVisibility_water(x, y, z, visibility);
                    if (quauCount)
                    {
                        int index = meshesIndex;
                        int vc = block.GenerateWaterFaces(visibility, ChunkManager._atlasReader, coubinMD, ref meshesIndex);
                        for (int ii = 0; ii < vc; ii++)
                        {
                            int newIndex = index + ii;
                            Vector3 vp = coubinMD.vertexDate[newIndex].vertice;
                            vp.Set(vp.x + x, vp.y + y, vp.z + z);
                            coubinMD.vertexDate[newIndex].vertice = vp;
                        }
                    }
                 
                }
            }
        }

        if (meshesIndex == 0)
        {
            MeshData_.ReturnMax(coubinMD);
            return;
        }
        if (!IsLoaded)
            IsLoaded = true;


        Mesh mesh = WaterMeshFilter.sharedMesh;
        MeshData_.Combine(this, ref mesh);

       WaterMeshCol.sharedMesh = mesh;
    
        ChunkManager.Instance.totolVc += WaterMeshFilter.sharedMesh.vertexCount;
        MeshData_.ReturnMax(coubinMD);
        //  if (!WaterMeshRenderer.gameObject.activeSelf)

        // WaterMeshRenderer.gameObject.SetActive(true);
        if (WaterMeshRenderer.renderingLayerMask == 0)
            WaterMeshRenderer.renderingLayerMask = 1;
        return;
    }

    public void BuildFoliageMesh()
    {
        meshesIndex = 0;
        coubinMD = MeshData_.GetMax();


        int ccc = 0;
        //  /*
        for (int x = 0; x < _size.x; x++)
        {
            for (int z = 0; z < _size.z; z++)
            {
                for (int y = 0; y< _size.y; y++)
                    {
                    BlockType block = Blocks[x, y, z];
                    if (!block.isBillboard)
                        continue;
                    bool quauCount = GetVisibility(x, y, z, visibility);
                    if (quauCount)
                    {
                        ccc++;
                        int index = meshesIndex;
                        int vc = block.GenerateMesh(visibility, ChunkManager._atlasReader, coubinMD, ref meshesIndex);
                        for (int ii = 0; ii < vc; ii++)
                        {
                            int newIndex = index + ii;
                            Vector3 vp = coubinMD.vertexDate[newIndex].vertice;
                            vp.Set(vp.x + x, vp.y + y, vp.z + z);
                            coubinMD.vertexDate[newIndex].vertice = vp;

                         
                        }
                    }
                }
            }
        }


        if (meshesIndex == 0)
        {

            MeshData_.ReturnMax(coubinMD);
            return;
        }
        if (!IsLoaded)
            IsLoaded = true;


        Mesh mesh = FoliageMeshFilter.sharedMesh;
        MeshData_.Combine(this, ref mesh);

        FoliageMeshCol.sharedMesh = mesh;
     
        ChunkManager.Instance.totolVc += FoliageMeshFilter.sharedMesh.vertexCount;
        MeshData_.ReturnMax(coubinMD);
        //  if (!FoliageMeshFilter.gameObject.activeSelf)

        //  FoliageMeshFilter.gameObject.SetActive(true);
        if (FoliageRenderer.renderingLayerMask == 0)
            FoliageRenderer.renderingLayerMask = 1;
        return;
    }


    protected bool GetVisibility(int x, int y, int z, bool[] visibility_ )
    {
     
        for (int i = 0; i < visibility.Length; i++)
        {
            if (visibility_[i])

                visibility_[i] = false;

        }
   
        // Up, Down, Front, Back, Left, Right


        //chunk 外围的方块
        // if(block.side)
        if (x == 0 || y == 0 || z == 0 || x == _sizeSmallOne.x || y == _sizeSmallOne.y || z == _sizeSmallOne.z)
        {
            

            if (y == _sizeSmallOne.y)
            {
                if (!NeighborUp.isEmperty)
                {
                    visibility_[0] = NeighborUp.Blocks[x, 0, z].isTransparent;
                    neighborsBlock[0] = true;

                }
                else
                {
                    visibility_[0] = true;
                    neighborsBlock[0] = true;
                }

            }
            ////////////////////
            if (y == 0)
            {


                if (NeighborDown)
                {


                    if (!NeighborDown.isEmperty)
                        visibility_[1] = NeighborDown.Blocks[x, _sizeSmallOne.y, z].isTransparent;
                    else
                        visibility_[1] = false;
                    neighborsBlock[1] = true;




                }
                else
                {
                    // ChunkManager.Instance.c4++;
                    //  Vector3Int wp = new Vector3Int(x, y, z);
                    // wp = this.LocalToWorldPosition(wp);
                    //visibility_[1] = WorldChunk.GetBlockType(wp.x, wp.y, wp.z, _seed).isTransparent;
                    // neighborsBlock[1] = true;

                    visibility_[1] = false;
                    neighborsBlock[1] = true;
                }


            }

            ////////////////////
            if (x == _sizeSmallOne.x)
            {


                if (!NeighborRight.isEmperty)
                    visibility_[2] = NeighborRight.Blocks[0, y, z].isTransparent;
                else if (NeighborRight.fullShutoutDown|| NeighborRight.fullShutoutDownByUpChunk)
                    visibility_[2] = false;
                else 
                    visibility_[2] = true;
                neighborsBlock[2] = true;


            }

            if (x == 0)
            {



                if (!NeighborLeft.isEmperty)
                    visibility_[3] = NeighborLeft.Blocks[_sizeSmallOne.x, y, z].isTransparent;
                else if (NeighborLeft.fullShutoutDown || NeighborLeft.fullShutoutDownByUpChunk)
                    visibility_[3] = false;
                else
                    visibility_[3] = true;
                neighborsBlock[3] = true;

            }

            if (z == 0)
            {


                if (!NeighborBack.isEmperty)
                    visibility_[5] = NeighborBack.Blocks[x, y, _sizeSmallOne.z].isTransparent;
                else if (NeighborBack.fullShutoutDown || NeighborBack.fullShutoutDownByUpChunk)
                    visibility_[5] = false;
                else
                    visibility_[5] = true;
                neighborsBlock[5] = true;



            }
            if (z == _sizeSmallOne.z)
            {

               

                if (!NeighborForward.isEmperty)
                    visibility_[4] = NeighborForward.Blocks[x, y, 0].isTransparent;
                else if (NeighborForward.fullShutoutDown || NeighborForward.fullShutoutDownByUpChunk)
                    visibility_[4] = false;
                else
                    visibility_[4] = true;
                neighborsBlock[4] = true;

            }




            if (neighborsBlock[0])
                neighborsBlock[0] = false;
            else
                visibility_[0] = Blocks[x, y + 1, z].isTransparent;

            if (neighborsBlock[1])
                neighborsBlock[1] = false;
            else
                visibility_[1] = Blocks[x, y - 1, z].isTransparent;

            if (neighborsBlock[2])
                neighborsBlock[2] = false;
            else
                visibility_[2] = Blocks[x + 1, y, z].isTransparent;

            if (neighborsBlock[3])
                neighborsBlock[3] = false;
            else
                visibility_[3] = Blocks[x - 1, y, z].isTransparent;

            if (neighborsBlock[4])
                neighborsBlock[4] = false;
            else
                visibility_[4] = Blocks[x, y, z + 1].isTransparent;

            if (neighborsBlock[5])
                neighborsBlock[5] = false;
            else
                visibility_[5] = Blocks[x, y, z - 1].isTransparent;







            for (int ni = 0; ni < 6; ni++)
            {
                if (visibility_[ni])
                    return true;
            }

            return false;
        }

        visibility_[0] = Blocks[x, y + 1, z].isTransparent;

        visibility_[1] = Blocks[x, y - 1, z].isTransparent;

        visibility_[2] = Blocks[x + 1, y, z].isTransparent;

        visibility_[3] = Blocks[x - 1, y, z].isTransparent;

        visibility_[4] = Blocks[x, y, z + 1].isTransparent;

        visibility_[5] = Blocks[x, y, z - 1].isTransparent;
      
        for (int ni = 0; ni < 6; ni++)
        {
            if (visibility_[ni])
                return true;
        }

        return false;

    }

    protected bool GetVisibility_water(int x, int y, int z, bool[] visibility_ )
    {

    
            if (y == _sizeSmallOne.y)
            {
                if (!NeighborUp.isEmperty)
                {
                    var temp2 = NeighborUp.Blocks[x, 0, z];
                    if (temp2.isWater)

                        return false;
                    else
                    {
                        return true;
                    }

                    visibility_[0] = NeighborUp.Blocks[x, 0, z].isTransparent;
                    neighborsBlock[0] = true;

                }
                else
                {
                    return true;
                    visibility_[0] = true;
                    neighborsBlock[0] = true;
                }

            }
       

        var temp = Blocks[x, y + 1, z];
        if (temp.isWater)
        
            return false;
        else
        {
            return true;
        }

        bool v = Blocks[x, y + 1, z].isTransparent;
        visibility_[0] = v;


        v = Blocks[x, y - 1, z].isTransparent;
        visibility_[1] = v;


        v = Blocks[x + 1, y, z].isTransparent;
        visibility_[2] = v;


        v = Blocks[x - 1, y, z].isTransparent;
        visibility_[3] = v;


        v = Blocks[x, y, z + 1].isTransparent;
        visibility_[4] = v;


        v = Blocks[x, y, z - 1].isTransparent;
        visibility_[5] = v;


        for (int ni = 0; ni < 6; ni++)
        {
            if (visibility_[ni])
                return true;
        }

        return false;

    }



    public void loadNeighborsBlocks()
    {
        if (!NeighborRight.computedTerrainDate)
            NeighborRight.CreateTerrainDate();

        if (!NeighborLeft.computedTerrainDate)
            NeighborLeft.CreateTerrainDate();

        if (!NeighborForward.computedTerrainDate)
            NeighborForward.CreateTerrainDate();

        if (!NeighborBack.computedTerrainDate)
            NeighborBack.CreateTerrainDate();

        if (!NeighborUp.computedTerrainDate)
            NeighborUp.CreateTerrainDate();


      //  if (NeighborDown)
     //   {
            if (!NeighborDown.computedTerrainDate)
                NeighborDown.CreateTerrainDate();
    //    }
          
     
        //  block.side = false;
        // BlockType ntype;

   
        

     
        return ;

    }




   
    public Vector3Int WorldToLocalPosition(Vector3Int worldPos)
    {
        return worldPos - ID;
    }

    private Vector3Int LocalToWorldPosition(Vector3Int localPos)
    {
        return localPos + ID;
    }

    private bool LocalPositionIsInRange(Vector3Int localPos)
    {
        //  return localPos.x >= 0 && localPos.z >= 0 && localPos.x < _size.x && localPos.z < _size.z && localPos.y >= 0 && localPos.y < _size.y;
        if (localPos.x>_sizeSmallOne.x|| localPos.y > _sizeSmallOne.y || localPos.z > _sizeSmallOne.z || localPos.x < 0 || localPos.y < 0 || localPos.z < 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }








    public Vector3 Center()
    {
        return new Vector3(_size.x / 2f + ID.x, _size.y / 2f + ID.y, _size.z / 2f + ID.z);
    }

    //chunk坐标
    public BlockType GetBlockAtWorldPosition(Vector3Int worldPos)
    {
      

        if (isAir)
        {
            return BlockType.Air;
        }
      
        Vector3Int localPos = WorldToLocalPosition(worldPos);
      return  GetBlockAtChunkPos(localPos. x, localPos. y, localPos. z);
    }


    //本地chunk的内部坐标索引
    public BlockType GetBlockAtChunkPos(int x, int y, int z)
    {

        if (!computedTerrainDate)
        {
            CreateTerrainDate(true);
        }
        return Blocks[x, y, z];
    }

 
  
        //射线一点做多可以打到四个方块的接触点 可以返回四个 也可以只检查其中一个
    public static    Vector3Int RoundToInt(Vector3 hitpoint, Vector3 rayPos)
    {
        Vector3Int vi = new Vector3Int();
        //默认 从上向下打 0.5四舍五入为1 要减掉一点点
      


        if (hitpoint.x%0.5f==0)
        {
            if (rayPos.x>=hitpoint.x)
                vi.x = Mathf.FloorToInt(hitpoint.x);
            else
                vi.x = Mathf.FloorToInt(hitpoint.x+1);
        }
        else
        {
            vi.x = (int)Math.Round(hitpoint.x, MidpointRounding.AwayFromZero);
        }

        if (hitpoint.y % 0.5f == 0)
        {
            if (rayPos.y >= hitpoint.y)
                vi.y = Mathf.FloorToInt(hitpoint.y);
            else
                vi.y = Mathf.FloorToInt(hitpoint.y + 1);
        }
        else
        {
            vi.y = (int)Math.Round(hitpoint.y, MidpointRounding.AwayFromZero);
        }

        if (hitpoint.z % 0.5f == 0)
        {
            if (rayPos.z >= hitpoint.z)
                vi.z = Mathf.FloorToInt(hitpoint.z);
            else
                vi.z = Mathf.FloorToInt(hitpoint.z + 1);
        }
        else
        {
            vi.z = (int)Math.Round(hitpoint.z, MidpointRounding.AwayFromZero);
        }
        return vi;

        if (rayPos.y >= hitpoint.y)
        {
            hitpoint.y -= 0.001f;
        }
        else
        {
            print(123);
            hitpoint.y += 0.001f;
        }

        vi.x = (int)Math.Round(hitpoint.x, MidpointRounding.AwayFromZero);
        vi.y = (int)Math.Round(hitpoint.y, MidpointRounding.AwayFromZero);
        vi.z = (int)Math.Round(hitpoint.z, MidpointRounding.AwayFromZero);
        return vi;
    }


    public void DesttroyBlocksFun()
    {


        foreach (var lpos in DestroyBlocks)
        {
            bool haveWaterAround = false;//周围有没有水

            int x = lpos.x;int y = lpos.y;int z = lpos.z;
           


            if (lpos.y == _sizeSmallOne.y)
            {
                if (!haveWaterAround)
                {
                    if (NeighborUp.GetBlockAtChunkPos( lpos.x,0, lpos.z).isWater)
                        haveWaterAround = true;
                }
                //   hightmap[lpos.x, lpos.z]--;
                if (!NeighborUp.isEmperty)
                {

                    ChunkManager.TouchedChunks.Add(NeighborUp);
                    NeighborUp.TouchedBlocks.Add(new Vector3Int(lpos.x, 0, lpos.z));

                }
            

            }
            if (lpos.y == 0)
            {


                if (!NeighborDown)
                {


                    var wp = new Vector3Int(ID.x, ID.y - ChunkManager.Instance.chunkSize.y, ID.z);
                    if (!ChunkManager._chunks.TryGetValue(wp, out NeighborDown))
                    {
                        NeighborDown = WorldChunk.GetChunk();
                        NeighborDown.Initialize(wp);
                        NeighborDown.InitializeNeighbors();

                        NeighborDown.CreateTerrainDate();

                    }
                    NeighborDown.NeighborUp = this;





                }
                ChunkManager.TouchedChunks.Add(NeighborDown);
                NeighborDown.TouchedBlocks.Add(new Vector3Int(lpos.x, _sizeSmallOne.y, lpos.z));


            }

            if (lpos.x == 0)
            {
                if (!haveWaterAround)
                {
                    if (NeighborLeft.GetBlockAtChunkPos(_sizeSmallOne.x, lpos.y, lpos.z).isWater)
                        haveWaterAround = true;
                }
                ChunkManager.TouchedChunks.Add(NeighborLeft);
                NeighborLeft.TouchedBlocks.Add(new Vector3Int(_sizeSmallOne.x, lpos.y, lpos.z));
                if (Blocks[lpos.x + 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }
            }
            else if (lpos.x == _sizeSmallOne.x)
            {

                if (!haveWaterAround)
                {
                    if (NeighborRight.GetBlockAtChunkPos(0, lpos.y, lpos.z).isWater)
                        haveWaterAround = true;
                }
                ChunkManager.TouchedChunks.Add(NeighborRight);
                NeighborRight.TouchedBlocks.Add(new Vector3Int(0, lpos.y, lpos.z));
                   if (Blocks[lpos.x - 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }

            }
            else
            {
                if (Blocks[lpos.x + 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }
                else if (Blocks[lpos.x - 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }
            }


            if (lpos.z == 0)
            {

                if (!haveWaterAround)
                {
                    if (NeighborBack.GetBlockAtChunkPos(lpos.x, lpos.y, _sizeSmallOne.z).isWater)
                        haveWaterAround = true;
                }

                ChunkManager.TouchedChunks.Add(NeighborBack);
                NeighborBack.TouchedBlocks.Add(new Vector3Int(lpos.x, lpos.y, _sizeSmallOne.z));

                if (Blocks[lpos.x, lpos.y, lpos.z + 1].isWater)
                {
                    haveWaterAround = true;
                }

            }
           else if (lpos.z == _sizeSmallOne.z)
            {
                if (!haveWaterAround)
                {
                    if (NeighborForward.GetBlockAtChunkPos(lpos.x, lpos.y, 0).isWater)
                        haveWaterAround = true;
                }
                ChunkManager.TouchedChunks.Add(NeighborForward);
                NeighborForward.TouchedBlocks.Add(new Vector3Int(lpos.x, lpos.y, 0));

                if (Blocks[lpos.x, lpos.y, lpos.z - 1].isWater)
                {
                    haveWaterAround = true;
                }

            }
            else
            {
                   if (Blocks[lpos.x, lpos.y, lpos.z + 1].isWater)
                {
                    haveWaterAround = true;
                }
                 if (Blocks[lpos.x , lpos.y, lpos.z - 1].isWater)
                {
                    haveWaterAround = true;
                }
            }




        
            var block = Blocks[lpos.x, lpos.y, lpos.z];
            // if (lpos.y != 0)
          //  if (hightmap[lpos.x, lpos.z] > 0)
         //   {
        //        hightmap[lpos.x, lpos.z]--;
        //    }
            if (haveWaterAround)
            {
                Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Water;
                //还要考虑四周是不是有空方块
                WaterFlowCheck(lpos.x, lpos.y, lpos.z);
            }
            else
            {
                Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Air;
            }








        }

        ChunkManager.TouchedChunks.Add(this);

        DestroyBlocks.Clear();

        return;
    }



    public void WaterFlowCheck( int x,int y,int z)
    {
        //可以通过设置气压来模拟喷泉
      //  if (y == _sizeSmallOne.y)
      //  {
          

     //   }

        if (y == 0)
        {
            if (!NeighborDown.computedTerrainDate)
                NeighborDown.CreateTerrainDate(true);
            
            if (NeighborDown.Blocks[x, _sizeSmallOne.y, z].isAir)
            {
                NeighborDown.Blocks[x, _sizeSmallOne.y, z] = BlockType.Water;
                NeighborDown.WaterFlowCheck(x, _sizeSmallOne.y, z);
             //  GameObject.Instantiate(chunkManager.ggg, NeighborDown. LocalToWorldPosition(new Vector3Int(x, _sizeSmallOne.y, z)), Quaternion.identity);
            }


            ChunkManager.TouchedChunks.Add(NeighborDown);
            NeighborDown.TouchedBlocks.Add(new Vector3Int(x, _sizeSmallOne.y, z));

        }
        else
        {
           
            if (Blocks[x, y-1, z ].isAir)
            {
                Blocks[x, y-1, z ] = BlockType.Water;
           //     GameObject.Instantiate(chunkManager.ggg,LocalToWorldPosition(new Vector3Int(x,y-1,z)),Quaternion.identity);
                WaterFlowCheck(x, y-1, z);
            }
        }

        if (x==0)
        {
            if (!NeighborLeft.computedTerrainDate)
                NeighborLeft.CreateTerrainDate(true);
            if (NeighborLeft.Blocks[_sizeSmallOne.x, y, z].isAir)
                {
                    NeighborLeft.Blocks[_sizeSmallOne.x, y, z] = BlockType.Water;
                    NeighborLeft.WaterFlowCheck(_sizeSmallOne.x, y, z);
                }

                ChunkManager.TouchedChunks.Add(NeighborLeft);
                NeighborLeft.TouchedBlocks.Add(new Vector3Int(_sizeSmallOne.x, y, z));
            }
            else if (x == _sizeSmallOne.x)
        {
            if (!NeighborRight.computedTerrainDate)
                NeighborRight.CreateTerrainDate(true);
            if (NeighborRight.Blocks[0, y, z].isAir)
                {
                    NeighborRight.Blocks[0, y, z] = BlockType.Water;
                    NeighborRight.WaterFlowCheck(0, y, z);
                }




                ChunkManager.TouchedChunks.Add(NeighborRight);
                NeighborRight.TouchedBlocks.Add(new Vector3Int(0, y, z));
            }
            else
            {
                if (Blocks[x + 1, y, z].isAir)
                {
                    Blocks[x + 1, y, z] = BlockType.Water;
                    WaterFlowCheck(x + 1, y, z);
                }
                if (Blocks[x - 1, y, z].isAir)
                {
                    Blocks[x - 1, y, z] = BlockType.Water;
                    WaterFlowCheck(x - 1, y, z);
                }
            }


            if (z==0)
        {
            if (!NeighborBack.computedTerrainDate)
                NeighborBack.CreateTerrainDate(true);
            if (NeighborBack.Blocks[x, y, _sizeSmallOne.z].isAir)
                {
                    NeighborBack.Blocks[x, y, _sizeSmallOne.z] = BlockType.Water;
                    NeighborBack.WaterFlowCheck(x, y, _sizeSmallOne.z);
                }


                ChunkManager.TouchedChunks.Add(NeighborBack);
                NeighborBack.TouchedBlocks.Add(new Vector3Int(x, y, _sizeSmallOne.z));

            }
            else if (z== _sizeSmallOne.z)
        {
            if (!NeighborForward.computedTerrainDate)
                NeighborForward.CreateTerrainDate(true);
            if (NeighborForward.Blocks[x, y, 0].isAir)
                {
                    NeighborForward.Blocks[x, y, 0] = BlockType.Water;
                    NeighborForward.WaterFlowCheck(x, y, 0);
                }


                ChunkManager.TouchedChunks.Add(NeighborForward);
                NeighborForward.TouchedBlocks.Add(new Vector3Int(x, y, 0));
            }
            else
            {
                if (Blocks[x, y, z + 1].isAir)
                {
                    Blocks[x, y, z + 1] = BlockType.Water;
                    WaterFlowCheck(x, y, z + 1);
                }
                if (Blocks[x, y, z - 1].isAir)
                {
                    Blocks[x, y, z - 1] = BlockType.Water;
                    WaterFlowCheck(x, y, z - 1);
                }
            }

            
        
    }



    public void AddBlocksFun()
    {
     
    }


    ////////////////////////////////////////
    public static Stack<WorldChunk> pool = new Stack<WorldChunk>(10000);
    public static WorldChunk GetChunk()
    {
        WorldChunk chunk;
        if (pool.Count>0)
        {
            chunk = pool.Pop();
        }
        else
        {
         ///   print(1234);
            chunk = Instantiate<WorldChunk>(ChunkManager.Instance.ChunkPrefab);
        }
        return chunk;
    }

   
}
