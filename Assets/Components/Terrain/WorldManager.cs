using Antymology.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Antymology.Terrain
{
    public class WorldManager : Singleton<WorldManager>
    {

        #region Fields

        /// <summary>
        /// The prefab containing the ant.
        /// </summary>
        public GameObject antPrefab;

        /// <summary>
        /// The material used for eech block.
        /// </summary>
        public Material blockMaterial;

        /// <summary>
        /// The raw data of the underlying world structure.
        /// </summary>
        private AbstractBlock[,,] Blocks;

        /// <summary>
        /// Reference to the geometry data of the chunks.
        /// </summary>
        private Chunk[,,] Chunks;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private System.Random RNG;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private SimplexNoise SimplexNoise;

        private AST workerAST;
        private AST queenAST;

        /// <summary>
        /// for performance reasons I elected to make pheromones a 2D array rather than a proper 3D one
        /// </summary>
        private byte[,,] pheromones;

        /// <summary>
        /// Stores references to ants by column
        /// </summary>
        private List<AntController>[,] antsAtLocation;

        List<AntController> ants;

        AntController currentlyProcessing = null;

        public Material queenMaterial;
        public GameObject queenMarker;

        #endregion

        #region Initialization

        /// <summary>
        /// Awake is called before any start method is called.
        /// </summary>
        void Awake()
        {
            UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

            // Generate new random number generator
            RNG = new System.Random(ConfigurationManager.Instance.Seed);

            // Generate new simplex noise generator
            SimplexNoise = new SimplexNoise(ConfigurationManager.Instance.Seed);

            // Initialize a new 3D array of blocks with size of the number of chunks times the size of each chunk
            Blocks = new AbstractBlock[
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
                ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter,
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

            // Initialize a new 3D array of chunks with size of the number of chunks
            Chunks = new Chunk[
                ConfigurationManager.Instance.World_Diameter,
                ConfigurationManager.Instance.World_Height,
                ConfigurationManager.Instance.World_Diameter];

            pheromones = new byte[
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            16];

            antsAtLocation = new List<AntController>[
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];
            for (int i = 0; i < antsAtLocation.GetLength(0); i++)
                for (int j = 0; j < antsAtLocation.GetLength(1); j++)
                    antsAtLocation[i, j] = new List<AntController>();


                    ants = new List<AntController>();

            workerAST = new AST();
            workerAST.RegisterUserNodeType(typeof(AntHealth));
            workerAST.RegisterUserNodeType(typeof(AntsHere));
            workerAST.RegisterUserNodeType(typeof(MoveForward));
            workerAST.RegisterUserNodeType(typeof(TurnRight));
            workerAST.RegisterUserNodeType(typeof(TurnLeft));
            workerAST.RegisterUserNodeType(typeof(Consume));
            workerAST.RegisterUserNodeType(typeof(Dig));
            workerAST.RegisterUserNodeType(typeof(TransferEnergy));
            workerAST.RegisterUserNodeType(typeof(DepositPheromone));
            workerAST.RegisterUserNodeType(typeof(GetValue));
            workerAST.RegisterUserNodeType(typeof(SetValue));
            workerAST.RegisterUserNodeType(typeof(SensePheromone));
            workerAST.RegisterUserNodeType(typeof(SenseBlockBelow));
            workerAST.RegisterUserNodeType(typeof(SenseBlockAhead));
            //give it some starting nodes
            for (int i = 0; i < 5; i++)
                workerAST.ExpandMutate();

            queenAST = new AST();
            queenAST.RegisterUserNodeType(typeof(AntHealth));
            queenAST.RegisterUserNodeType(typeof(AntsHere));
            queenAST.RegisterUserNodeType(typeof(MoveForward));
            queenAST.RegisterUserNodeType(typeof(TurnRight));
            queenAST.RegisterUserNodeType(typeof(TurnLeft));
            queenAST.RegisterUserNodeType(typeof(Consume));
            queenAST.RegisterUserNodeType(typeof(Dig));
            queenAST.RegisterUserNodeType(typeof(TransferEnergy));
            queenAST.RegisterUserNodeType(typeof(DepositPheromone));
            queenAST.RegisterUserNodeType(typeof(GetValue));
            queenAST.RegisterUserNodeType(typeof(SetValue));
            queenAST.RegisterUserNodeType(typeof(SensePheromone));
            queenAST.RegisterUserNodeType(typeof(SenseBlockBelow));
            queenAST.RegisterUserNodeType(typeof(SenseBlockAhead));
            queenAST.RegisterUserNodeType(typeof(CreateNest)); //Unique to queen
            //give it some starting nodes
            for (int i = 0; i < 5; i++)
                queenAST.ExpandMutate();
            Debug.Log("Worker Brain:" + Environment.NewLine + workerAST.root.GetSubtreeString());
            Debug.Log("Queen Brain:" + Environment.NewLine + queenAST.root.GetSubtreeString());
        }

        /// <summary>
        /// Called after every awake has been called.
        /// </summary>
        private void Start()
        {
            GenerateData();
            GenerateChunks();

            Camera.main.transform.position = new Vector3(0 / 2, Blocks.GetLength(1), 0);
            Camera.main.transform.LookAt(new Vector3(Blocks.GetLength(0), 0, Blocks.GetLength(2)));

            GenerateAnts();
            FixAntPositions();
            UpdateAntLocations();
        }

        private float stepTime = 0;
        public bool run = false;
        public bool step = false;

        private void Update()
        {
            if (!run && !step)
            {
                stepTime = 0;
                return;
            }
            stepTime += Time.deltaTime;
            if (stepTime > 1 || step)//ConfigurationManager.Instance.minTimeDelta)
            {
                step = false;
                stepTime = 0;
                Step();
            }
        }

        private void Step()
        {
            foreach (IAntNode node in queenAST.nodes.OfType<IAntNode>().ToArray())
                node.setWorld(this);
            foreach (IAntNode node in workerAST.nodes.OfType<IAntNode>().ToArray())
                node.setWorld(this);
            EvaporatePheromones();
            UpdateAntLocations();
            for(int i = ants.Count-1; i>= 0; i--)
            {
                AntController ant = ants[i];
                currentlyProcessing = ant;
                ant.Step();
                if (ant.currentHealth <= 0)
                {
                    ants.Remove(ant);
                    if (ant.type == AntController.antType.QUEEN)
                        Destroy(queenMarker);
                    Destroy(ant.gameObject);
                }
            }
            FixAntPositions();
        }

        private void EvaporatePheromones()
        {
            for (int i = 0; i < pheromones.GetLength(0); i++)
                for (int j = 0; j < pheromones.GetLength(1); j++)
                    for(int z = 0; z < pheromones.GetLength(2); z++)
                    pheromones[i, j, z] = (byte)Math.Max(pheromones[i, j, z] - 1, 0);
        }

        private void FixAntPositions()
        {
            foreach(AntController ant in ants)
            {
                int y = 0;
                while (GetBlock(ant.blockPos.x, y, ant.blockPos.z) is not AirBlock)
                {
                    y++;
                }
                ant.blockPos.y = y;
                ant.transform.position = ant.blockPos - new Vector3(0,0.5f,0);
                if (ant.type == AntController.antType.QUEEN)
                    queenMarker.transform.position = ant.transform.position;
                //fix rotation
                ant.transform.rotation = Quaternion.LookRotation(ant.heading);
            }
        }

        private void UpdateAntLocations()
        {
            //clear lists 
            for (int i = 0; i < antsAtLocation.GetLength(0); i++)
                for (int j = 0; j < antsAtLocation.GetLength(1); j++)
                    antsAtLocation[i, j].Clear();

            foreach (AntController ant in ants)
                antsAtLocation[ant.blockPos.x, ant.blockPos.z].Add(ant);
        }

        public AntController GetCurrentProcessingAnt()
        {
            return currentlyProcessing;
        }

        public int CountAntsAtBlock(int x, int z)
        {
            return antsAtLocation[x,z].Count;
        }

        public List<AntController> antsAtBlock(int x, int y, int z)
        {
            return new List<AntController>(antsAtLocation[x, z]);
        }

        public int pheromoneAtBlock(int x, int z, byte type)
        {
            return pheromones[x,z,type];
        }

        public void addPheromone(int x, int z, byte type, byte amount)
        {
            pheromones[x, z, type] = (byte)Math.Min(pheromones[x, z, type] + amount, byte.MaxValue);
        }

        /// <summary>
        /// TO BE IMPLEMENTED BY YOU
        /// </summary>
       
        private void GenerateAnts()
        {
            for(int i = 0; i < 40; i++)
            {
                //spawn them roughly in the middle of the map
                int center = (ConfigurationManager.Instance.Chunk_Diameter * ConfigurationManager.Instance.World_Diameter) / 2;
                int spawnx = center + UnityEngine.Random.Range(-8, 8);
                int spawnz = center + UnityEngine.Random.Range(-8, 8);
                Vector3Int spawnPos = new Vector3Int(spawnx, 0, spawnz);
                GameObject antObject = Instantiate(antPrefab, spawnPos, Quaternion.identity);
                AntController antScript = antObject.GetComponent<AntController>();
                antScript.MoveToBlockCoord(spawnPos);
                Vector3Int[] headings = { new Vector3Int(1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, -1) };
                antScript.heading = headings[UnityEngine.Random.Range(0, 4)];
                if(antScript != null)
                {
                    ants.Add(antScript);
                    antScript.world = this;
                    if (i == 0)
                    {
                        antScript.brain = queenAST;
                        antScript.type = AntController.antType.QUEEN;
                        MeshRenderer render = antObject.GetComponentsInChildren<MeshRenderer>()[0];
                        Material[] materials = render.materials;
                        materials[0] = queenMaterial;
                        materials[1] = queenMaterial;
                        materials[2] = queenMaterial;
                        materials[3] = queenMaterial;
                        render.materials = materials;
                        queenMarker.transform.position = antObject.transform.position;
                    }
                    else
                    {
                        antScript.brain = workerAST;
                        antScript.type = AntController.antType.WORKER;
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves an abstract block type at the desired world coordinates.
        /// </summary>
        public AbstractBlock GetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate >= Blocks.GetLength(0) ||
                WorldYCoordinate >= Blocks.GetLength(1) ||
                WorldZCoordinate >= Blocks.GetLength(2)
            )
                return new AirBlock();

            return Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate];
        }

        /// <summary>
        /// Retrieves an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public AbstractBlock GetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate >= Blocks.GetLength(0) ||
                LocalYCoordinate >= Blocks.GetLength(1) ||
                LocalZCoordinate >= Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate >= Blocks.GetLength(0) ||
                ChunkYCoordinate >= Blocks.GetLength(1) ||
                ChunkZCoordinate >= Blocks.GetLength(2) 
            )
                return new AirBlock();

            return Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ];
        }

        /// <summary>
        /// sets an abstract block type at the desired world coordinates.
        /// </summary>
        public void SetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate, AbstractBlock toSet)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate > Blocks.GetLength(0) ||
                WorldYCoordinate > Blocks.GetLength(1) ||
                WorldZCoordinate > Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }

            Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate] = toSet;

            SetChunkContainingBlockToUpdate
            (
                WorldXCoordinate,
                WorldYCoordinate,
                WorldZCoordinate
            );
        }

        /// <summary>
        /// sets an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public void SetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate,
            AbstractBlock toSet)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate > Blocks.GetLength(0) ||
                LocalYCoordinate > Blocks.GetLength(1) ||
                LocalZCoordinate > Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate > Blocks.GetLength(0) ||
                ChunkYCoordinate > Blocks.GetLength(1) ||
                ChunkZCoordinate > Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }
            Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ] = toSet;

            SetChunkContainingBlockToUpdate
            (
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            );
        }

        #endregion

        #region Helpers

        #region Blocks

        /// <summary>
        /// Is responsible for generating the base, acid, and spheres.
        /// </summary>
        private void GenerateData()
        {
            GeneratePreliminaryWorld();
            GenerateAcidicRegions();
            GenerateSphericalContainers();
        }

        /// <summary>
        /// Generates the preliminary world data based on perlin noise.
        /// </summary>
        private void GeneratePreliminaryWorld()
        {

            for (int x = 0; x < Blocks.GetLength(0); x++)
                for (int z = 0; z < Blocks.GetLength(2); z++)
                {
                    /**
                     * These numbers have been fine-tuned and tweaked through trial and error.
                     * Altering these numbers may produce weird looking worlds.
                     **/
                    int stoneCeiling = SimplexNoise.GetPerlinNoise(x, 0, z, 10, 3, 1.2) +
                                       SimplexNoise.GetPerlinNoise(x, 300, z, 20, 4, 0) +
                                       10;
                    int grassHeight = SimplexNoise.GetPerlinNoise(x, 100, z, 30, 10, 0);
                    int foodHeight = SimplexNoise.GetPerlinNoise(x, 200, z, 20, 5, 1.5);

                    for (int y = 0; y < Blocks.GetLength(1); y++)
                    {
                        if (y <= stoneCeiling)
                        {
                            Blocks[x, y, z] = new StoneBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight)
                        {
                            Blocks[x, y, z] = new GrassBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight + foodHeight)
                        {
                            Blocks[x, y, z] = new MulchBlock();
                        }
                        else
                        {
                            Blocks[x, y, z] = new AirBlock();
                        }
                        if
                        (
                            x == 0 ||
                            x >= Blocks.GetLength(0) - 1 ||
                            z == 0 ||
                            z >= Blocks.GetLength(2) - 1 ||
                            y == 0
                        )
                            Blocks[x, y, z] = new ContainerBlock();
                    }
                }
        }

        /// <summary>
        /// Alters a pre-generated map so that acid blocks exist.
        /// </summary>
        private void GenerateAcidicRegions()
        {
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Acidic_Regions; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = -1;
                for (int j = Blocks.GetLength(1) - 1; j >= 0; j--)
                {
                    if (Blocks[xCoord, j, zCoord] as AirBlock == null)
                    {
                        yCoord = j;
                        break;
                    }
                }

                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HX < xCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HZ < zCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HY < yCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Acidic_Region_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                if (Blocks[CX, CY, CZ] as AirBlock != null)
                                    Blocks[CX, CY, CZ] = new AcidicBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Alters a pre-generated map so that obstructions exist within the map.
        /// </summary>
        private void GenerateSphericalContainers()
        {

            //Generate hazards
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Conatiner_Spheres; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = RNG.Next(0, Blocks.GetLength(1));


                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX < xCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ < zCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY < yCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Conatiner_Sphere_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                Blocks[CX, CY, CZ] = new ContainerBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a world coordinate, tells the chunk holding that coordinate to update.
        /// Also tells all 4 neighbours to update (as an altered block might exist on the
        /// edge of a chunk).
        /// </summary>
        /// <param name="worldXCoordinate"></param>
        /// <param name="worldYCoordinate"></param>
        /// <param name="worldZCoordinate"></param>
        private void SetChunkContainingBlockToUpdate(int worldXCoordinate, int worldYCoordinate, int worldZCoordinate)
        {
            //Updates the chunk containing this block
            int updateX = Mathf.FloorToInt(worldXCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            int updateY = Mathf.FloorToInt(worldYCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            int updateZ = Mathf.FloorToInt(worldZCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            Chunks[updateX, updateY, updateZ].updateNeeded = true;
            
            // Also flag all 6 neighbours for update as well
            if(updateX - 1 >= 0)
                Chunks[updateX - 1, updateY, updateZ].updateNeeded = true;
            if (updateX + 1 < Chunks.GetLength(0))
                Chunks[updateX + 1, updateY, updateZ].updateNeeded = true;

            if (updateY - 1 >= 0)
                Chunks[updateX, updateY - 1, updateZ].updateNeeded = true;
            if (updateY + 1 < Chunks.GetLength(1))
                Chunks[updateX, updateY + 1, updateZ].updateNeeded = true;

            if (updateZ - 1 >= 0)
                Chunks[updateX, updateY, updateZ - 1].updateNeeded = true;
            if (updateX + 1 < Chunks.GetLength(2))
                Chunks[updateX, updateY, updateZ + 1].updateNeeded = true;
        }

        #endregion

        #region Chunks

        /// <summary>
        /// Takes the world data and generates the associated chunk objects.
        /// </summary>
        private void GenerateChunks()
        {
            GameObject chunkObg = new GameObject("Chunks");

            for (int x = 0; x < Chunks.GetLength(0); x++)
                for (int z = 0; z < Chunks.GetLength(2); z++)
                    for (int y = 0; y < Chunks.GetLength(1); y++)
                    {
                        GameObject temp = new GameObject();
                        temp.transform.parent = chunkObg.transform;
                        temp.transform.position = new Vector3
                        (
                            x * ConfigurationManager.Instance.Chunk_Diameter - 0.5f,
                            y * ConfigurationManager.Instance.Chunk_Diameter + 0.5f,
                            z * ConfigurationManager.Instance.Chunk_Diameter - 0.5f
                        );
                        Chunk chunkScript = temp.AddComponent<Chunk>();
                        chunkScript.x = x * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.y = y * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.z = z * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.Init(blockMaterial);
                        chunkScript.GenerateMesh();
                        Chunks[x, y, z] = chunkScript;
                    }
        }

        #endregion

        #endregion
    }
}
