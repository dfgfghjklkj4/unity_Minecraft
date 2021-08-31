using MyNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;


public enum BlockNameEnum
{
     Air,
    Water,
    Bedrock,
    Grass,
    Cobblestone,
    Daisy,
    Diamond_Ore,
    Dirt,
    Gravel,
    Iron_Ore,
    Orange_Tulip,
    Pink_Tulip,
    Plank,
    Red_Tulip,
    Sand,
    Sandstone,
    Stone,
    Tall_Grass,
    Yellow_Flower,
}

[CreateAssetMenu(fileName = "New Block Type", menuName = "Block Type")]
public class BlockType : ScriptableObject
{

    public static BlockType Air;
    public static BlockType Water;
    public static BlockType Bedrock;
    public static BlockType Grass;
    public static BlockType Cobblestone;

    public static BlockType Daisy;
    public static BlockType Diamond_Ore;
    public static BlockType Dirt;
    public static BlockType Gravel;
    public static BlockType Iron_Ore;
    public static BlockType Orange_Tulip;
    public static BlockType Pink_Tulip;
    public static BlockType Plank;
    public static BlockType Red_Tulip;

    public static BlockType Sand;
    public static BlockType Sandstone;
    public static BlockType Stone;
    public static BlockType Tall_Grass;
    public static BlockType Yellow_Flower;
  
    /*------------------------ MEMBER ------------------------*/

    public BlockNameEnum blockName;
    // Position of each of the 6 faces in the block atlas.
    // Top, Bottom, Front, Back, Left, Right
    public Vector2Int[] atlasPositions;
    public bool isTransparent;
    public bool isPlant = false;
    public bool isBillboard = false;
    public bool affectedByGravity = false;
    public bool isSourceBlock = false;
    public bool isWater = false;
    public bool mustBeOnGrassBlock = false;
    public AudioClip digClip = null;
    public AudioClip[] stepClips;
    public ParticleSystem.MinMaxGradient breakParticleColors;
    public int blockBlastResistance = 0;
    public bool isAir=false;
    [HideInInspector]
    public bool side;
    /*------------------------ STATIC ------------------------*/

    // Maps the names of block types to their corresponding object.
    public static Dictionary<BlockNameEnum, BlockType> NameToBlockType=new Dictionary<BlockNameEnum, BlockType>();
    public static Dictionary<BlockNameEnum, MeshData_> NameToFullMeshDate = new Dictionary<BlockNameEnum, MeshData_>();
    public static Dictionary<BlockType, MeshData_> BlockTypeToFullMeshDate = new Dictionary<BlockType, MeshData_>();

   // [HideInInspector]
   // public Vector2[] uvs;


    [HideInInspector]
    public MeshData_ md;
    public static void LoadBlockTypes()
    {
        // Load all the BlockType assets from the Resources folder.
        BlockType[] typeArray = Resources.LoadAll<BlockType>("Block Types");
        bool[] visible = new bool[6];
        AtlasReader ar = new AtlasReader(ChunkManager.Instance.blockAtlas, 8);
        for (int i = 0;i < visible.Length; i++)
        {
            visible[i] = true;
        }
  
        for (int i = 0; i < typeArray.Length; i++)
    
        {
         
            BlockType type= typeArray[i];
           // Debug.Log(type.name);
            if (NameToBlockType.ContainsKey(type.blockName))
            {
                Debug.LogError(type.blockName + "  加载错误 ");
            }
            else
            {
               
                NameToBlockType.Add(type.blockName, type);

                MeshData_ md = MeshData_.Get24();
         
                var index = 0;
           type.GenerateMeshInit(visible, ar,ref md,ref index);
                 
                        NameToFullMeshDate.Add(type.blockName, md);
                BlockTypeToFullMeshDate.Add(type,md);
            //  Debug.Log(md.vertexCount + " _vertices " + md.TriangleCount+ " _triangles " + type.blockName);

            }
        
        }
        Air = NameToBlockType[BlockNameEnum.Air];
        Water = NameToBlockType[BlockNameEnum.Water];

        Bedrock = NameToBlockType[BlockNameEnum.Bedrock];

        Grass = NameToBlockType[BlockNameEnum.Grass];

        Cobblestone = NameToBlockType[BlockNameEnum.Cobblestone];

        Daisy = NameToBlockType[BlockNameEnum.Daisy];
        Diamond_Ore = NameToBlockType[BlockNameEnum.Diamond_Ore];
        Dirt = NameToBlockType[BlockNameEnum.Dirt];
        Gravel = NameToBlockType[BlockNameEnum.Gravel];
        Iron_Ore = NameToBlockType[BlockNameEnum.Iron_Ore];
        Orange_Tulip = NameToBlockType[BlockNameEnum.Orange_Tulip];
        Pink_Tulip = NameToBlockType[BlockNameEnum.Pink_Tulip];
        Plank = NameToBlockType[BlockNameEnum.Plank];
        Red_Tulip = NameToBlockType[BlockNameEnum.Red_Tulip];
        Sand = NameToBlockType[BlockNameEnum.Sand];
        Sandstone = NameToBlockType[BlockNameEnum.Sandstone];
        Stone = NameToBlockType[BlockNameEnum.Stone];
        Tall_Grass = NameToBlockType[BlockNameEnum.Tall_Grass];

        Yellow_Flower = NameToBlockType[BlockNameEnum.Yellow_Flower];

    }

    public static BlockType GetBlockType(BlockNameEnum name)
    {
        return NameToBlockType[name];
    }


    public static List<BlockType> PlantBlocks ;
    public static List<BlockType> GetPlantBlockTypes()
    {
        if (PlantBlocks!=null)
        {
            return PlantBlocks;
        }
      
        PlantBlocks = new List<BlockType>();
        foreach (var type in NameToBlockType.Values)
        {
            if (type.isPlant) {
                PlantBlocks.Add(type);
            }   
        }
        return PlantBlocks;
    }

    public static bool IsAirBlock(BlockType block)
    {
        if (block == null)
            return true;
        else
        {
            if (block.blockName == BlockNameEnum.Air)
                return true;
            else
                return false;
        }
       
    

    }
    /////////////////////////////////////////////////////
    /*------------------------ STATIC VARIABLES ------------------------*/

    public static readonly Vector3[] FACE_DIRECTIONS = {
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    public int  GenerateMesh(bool[] faceIsVisible, AtlasReader atlasReader,  MeshData_ md,ref int startIndex)
    {
      
        if (isBillboard)
        {

            return GenerateBillboardFaces(atlasReader,ref md,ref startIndex);
            
        }
        else
        {

            return GenerateCubeFaces(faceIsVisible, atlasReader, md,ref startIndex);
           
        }
    }



     int GenerateMeshInit(bool[] faceIsVisible, AtlasReader atlasReader, ref MeshData_ md, ref int startIndex)
    {

        if (isBillboard)
        {

            return GenerateBillboardFacesInit(atlasReader, ref md, ref startIndex);

        }
        else
        {

            return GenerateCubeFacesInit(faceIsVisible, atlasReader, ref md, ref startIndex);

        }
    }


    static Vector3[] baseVertices =
      {
            new Vector3(1.0f, -1.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, -1.0f, 0.0f)
        };
    static Color[] baseColors = {
            Color.black,
            Color.red,
            Color.red,
            Color.black,
        };
    static Quaternion[] rotations =
      {
            Quaternion.AngleAxis(45f, Vector3.up),
            Quaternion.AngleAxis(-45f, Vector3.up),
            Quaternion.AngleAxis(135f, Vector3.up),
            Quaternion.AngleAxis(-135f, Vector3.up)
        };
   static int[] triangles = {
            0, 1, 2, 0, 2, 3,
            0+4, 1+4, 2+4, 0+4, 2+4, 3+4,
            0+8, 1+8, 2+8, 0+8, 2+8, 3+8,
            0+12, 1+12, 2+12, 0+12, 2+12, 3+12
        };
    public int GenerateBillboardFaces(AtlasReader atlasReader, ref MeshData_ md,ref int startIndex)
    {


      
        Array.Copy(this.md.vertexDate, 0, md.vertexDate, startIndex,16);

    
        startIndex += 16;
        return 16;
    }







    public static bool ffff = true;
    public int GenerateCubeFaces( bool[] faceIsVisible, AtlasReader atlasReader,  MeshData_ md,ref  int startIndex )
    {

        int vc = 0;
        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
            if (faceIsVisible[i] == false)
            {
                continue; // Don't bother making a mesh for a face that can't be seen.
            }

           // faceIsVisible[i] = true;
            int iiii = i * 4;
         md.vertexDate[startIndex] = this.md.vertexDate[iiii];
         md.vertexDate[startIndex+1] = this.md.vertexDate[iiii + 1];
         md.vertexDate[startIndex+2] = this.md.vertexDate[iiii + 2];
         md.vertexDate[startIndex+3] = this.md.vertexDate[iiii + 3];
         //  Array.Copy(this.md.vertexDate, iiii, md.vertexDate, startIndex,4);
           // Buffer.BlockCopy(this.md.vertexDate, iiii, md.vertexDate, startIndex, 32);
         startIndex += 4;
              vc += 4;




        }

      
        return vc;
    }


    public int GenerateWaterFaces(bool[] faceIsVisible, AtlasReader atlasReader, MeshData_ md, ref int startIndex)
    {

        int vc = 0;
        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
          //  if (faceIsVisible[i] == false)
         //   {
         //       continue; // Don't bother making a mesh for a face that can't be seen.
         //   }

            // faceIsVisible[i] = true;
            int iiii = i * 4;
            md.vertexDate[startIndex] = this.md.vertexDate[iiii];
            md.vertexDate[startIndex + 1] = this.md.vertexDate[iiii + 1];
            md.vertexDate[startIndex + 2] = this.md.vertexDate[iiii + 2];
            md.vertexDate[startIndex + 3] = this.md.vertexDate[iiii + 3];
            //  Array.Copy(this.md.vertexDate, iiii, md.vertexDate, startIndex,4);
            // Buffer.BlockCopy(this.md.vertexDate, iiii, md.vertexDate, startIndex, 32);
            startIndex += 4;
            vc += 4;

            return vc;


        }


        return vc;
    }

    /// <summary>
    /// ////////////////////
   
    int GenerateCubeFacesInit(bool[] faceIsVisible, AtlasReader atlasReader, ref MeshData_ md, ref int startIndex)
    {

        int vc = 0;
        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
          

            Vector2Int[] atlasPositions = this.atlasPositions;
            // int index = atlasPositions.Length == 1 ? 0 : i;

            int index01 = startIndex;
            int index02 = startIndex + 1;
            int index03 = startIndex + 2;
            int index04 = startIndex + 3;
            // GenerateBlockFace(FACE_DIRECTIONS[i], c, ref md);
            ////////////////////////////////////////////////////////////////////////



            Vector3 direction = FACE_DIRECTIONS[i];


            md.vertexDate[index01].normal = direction;
            md.vertexDate[index02].normal = direction;
            md.vertexDate[index03].normal = direction;
            md.vertexDate[index04].normal = direction;
            if (direction == Vector3.up)
            {
               
                md.vertexDate[index01].vertice = new Vector3(-0.5f, 0.5f, -0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, 0.5f, 0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, 0.5f, 0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, 0.5f, -0.5f);

            }
            else if (direction == Vector3.down)
            {
             
                md.vertexDate[index01].vertice = new Vector3(-0.5f, -0.5f, 0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, -0.5f, -0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, -0.5f, -0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, -0.5f, 0.5f);
            }
            else if (direction == Vector3.right)
            {
             
                md.vertexDate[index01].vertice = new Vector3(0.5f, -0.5f, -0.5f);
                md.vertexDate[index02].vertice = new Vector3(0.5f, 0.5f, -0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, 0.5f, 0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, -0.5f, 0.5f);
            }
            else if (direction == Vector3.left)
            {
            
                md.vertexDate[index01].vertice = new Vector3(-0.5f, -0.5f, 0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, 0.5f, 0.5f);
                md.vertexDate[index03].vertice = new Vector3(-0.5f, 0.5f, -0.5f);
                md.vertexDate[index04].vertice = new Vector3(-0.5f, -0.5f, -0.5f);
            }
            else if (direction == Vector3.forward)
            {
              
                md.vertexDate[index01].vertice = new Vector3(0.5f, -0.5f, 0.5f);
                md.vertexDate[index02].vertice = new Vector3(0.5f, 0.5f, 0.5f);
                md.vertexDate[index03].vertice = new Vector3(-0.5f, 0.5f, 0.5f);
                md.vertexDate[index04].vertice = new Vector3(-0.5f, -0.5f, 0.5f);
            }
            else if (direction == Vector3.back)
            {
              
                md.vertexDate[index01].vertice = new Vector3(-0.5f, -0.5f, -0.5f);
                md.vertexDate[index02].vertice = new Vector3(-0.5f, 0.5f, -0.5f);
                md.vertexDate[index03].vertice = new Vector3(0.5f, 0.5f, -0.5f);
                md.vertexDate[index04].vertice = new Vector3(0.5f, -0.5f, -0.5f);
            }

          
                int index2 = atlasPositions.Length == 1 ? 0 : i;
           
            if (!isWater)
            {
                atlasReader.GetUVs(atlasPositions[index2].x, atlasPositions[index2].y, ref md, startIndex);

            }
            else
            {//对水进行特殊处理 水使用自己单独的材质和贴图 
                md.vertexDate[0].uv = new Vector2(0,0);
                md.vertexDate[0 + 1].uv = new Vector2(0, 1);
                md.vertexDate[0 + 2].uv = new Vector2(1, 0);
                md.vertexDate[0 + 3].uv = new Vector2(1, 1);
            }




            //   atlasReader.GetUVs(atlasPositions[index].x, atlasPositions[index].y, ref md, startIndex);
            startIndex += 4;
            vc += 4;




        }

        this.md = md;
        return vc;
    }

    public int GenerateBillboardFacesInit(AtlasReader atlasReader, ref MeshData_ md, ref int startIndex)
    {


   
        md.vertexCount = 0;
        for (int i = 0; i < rotations.Length; i++)
        {
            Quaternion rotation = rotations[i];
            for (int j = 0; j < 4; j++)
            {
                md.vertexDate[rotations.Length * i + j + startIndex].vertice = rotation * baseVertices[j] * 0.5f;
            }
         
        }

        Vector3 normal = new Vector3(0f, 0f, 1f);
        for (int i = 0; i < 16; i++)
        {
            //  md.normals.Add(normal);
            md.vertexDate[startIndex + i].normal = normal;
        }

        Vector2Int atlasIndex = atlasPositions[0];
    
        int indextemp = startIndex;
        for (int i = 0; i < rotations.Length; i++)
        {
            //  List<Vector2> uv=new List<Vector2>();
            atlasReader.GetUVs(atlasIndex.x, atlasIndex.y, ref md, indextemp);
            // uvs.AddRange(uv);
            indextemp += 4;
        }



        startIndex += 16;
        this.md = md;
        return 16;
    }


    /*------------------------ STATIC METHODS ------------------------*/




  

}
