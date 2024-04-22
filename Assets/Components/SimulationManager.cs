using Antymology.Terrain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{

    public GameObject WorldInstance;

    private List<WorldManager> managers = new List<WorldManager>();
    private List<GameObject> worlds = new List<GameObject>();
    private bool readyToRun = false;

    public float dt = 0.5f;
    public bool run = false;
    public bool step = false;

    //display variables
    public int disp_TotalAntCount = 0;
    public int[] disp_WorldAntCounts;
    public int[] disp_WorldNestCounts;

    // Start is called before the first frame update
    void Start()
    {
        disp_WorldAntCounts = new int[ConfigurationManager.Instance.Selection_pool_size * (ConfigurationManager.Instance.Selected_offspring_count + 1)];
        disp_WorldNestCounts = new int[ConfigurationManager.Instance.Selection_pool_size * (ConfigurationManager.Instance.Selected_offspring_count + 1)];
        int InstnaceDiameter = ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter;
        int InstnaceHeight = ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter;
        Camera.main.transform.position = new Vector3(-5, InstnaceHeight + 20, -5);
        Camera.main.transform.LookAt(new Vector3(InstnaceDiameter * 2, InstnaceHeight - 7, InstnaceDiameter * 2));

        GenerateWorlds();
        //after generating the worlds perform expansions of the brains to give some random starting behaviour
        foreach(WorldManager manager in managers)
            for (int a = 0; a < 10; a++)
            {
                manager.queenAST.ExpandMutate();
                manager.workerAST.ExpandMutate();
            }
        InitializeWorlds();
        readyToRun = true;
    }


    private float stepTime = 0;
    

    // Update is called once per frame
    private void Update()
    {
        if(!readyToRun)
        {
            return;
        }
        if (!run && !step)
        {
            stepTime = 0;
            return;
        }
        stepTime += Time.deltaTime;
        if (stepTime > dt || step || ConfigurationManager.Instance.SimulationOnly)//ConfigurationManager.Instance.minTimeDelta)
        {
            step = false;
            stepTime = 0;
            Step();
            int antCount = 0;
            int i = 0;
            foreach (WorldManager manager in managers)
            {
                disp_WorldAntCounts[i] = manager.ants.Count;
                disp_WorldNestCounts[i] = manager.nestBlocks;
                antCount += manager.ants.Count;
                i++;
            }
            disp_TotalAntCount = antCount;
            
            if (antCount == 0)
            {
                int mostNestBlocks = 0;
                AST bestWorkerBrain = null;
                AST bestQueenBrain = null;
                foreach (WorldManager manager in managers)
                {
                    if(manager.nestBlocks >= mostNestBlocks)
                    {
                        mostNestBlocks = manager.nestBlocks;
                        bestWorkerBrain = manager.workerAST;
                        bestQueenBrain = manager.queenAST;
                    }
                }
                Debug.Log("Everyone is dead :(" + Environment.NewLine + "Best world made " + mostNestBlocks + " Nest blocks!");
                Debug.Log("Worker brain: " + Environment.NewLine + bestWorkerBrain.root.GetSubtreeString());
                Debug.Log("Queen brain: " + Environment.NewLine + bestQueenBrain.root.GetSubtreeString());
                MutateAndReset();
            }
        }
    }

    void MutateAndReset()
    {
        readyToRun = false;
        //choose n best worlds
        List<WorldManager> selected = managers.OrderByDescending(t => t.nestBlocks).Take(ConfigurationManager.Instance.Selection_pool_size).ToList();
        List<Tuple<AST, AST>> brains = new List<Tuple<AST, AST>>();
        foreach (WorldManager manager in selected)
            brains.Add(new Tuple<AST, AST>(manager.queenAST, manager.workerAST)); 

        //remove all worlds and recreate them
        foreach (GameObject world in worlds)
            Destroy(world);
        worlds.Clear();
        managers.Clear();
        GenerateWorlds();

        //make mutations of the selected and apply them to the new worlds
        for (int i = 0; i < ConfigurationManager.Instance.Selection_pool_size; i++)
        {
            //add the original to make this a + strategy
            managers[i * (ConfigurationManager.Instance.Selected_offspring_count + 1)].SetBrains(new AST(brains[i].Item1), new AST(brains[i].Item2));
            for (int j = 1; j < ConfigurationManager.Instance.Selected_offspring_count + 1; j++)
            {
                AST queenBrain = new AST(brains[i].Item1);
                AST workerBrain = new AST(brains[i].Item2);
                queenBrain.RandomMutation();
                workerBrain.RandomMutation();
                managers[i * (ConfigurationManager.Instance.Selected_offspring_count + 1) + j].SetBrains(queenBrain, workerBrain);
            }
        }

        InitializeWorlds();
        readyToRun = true;
    }

    void Step()
    {
        for (int i = managers.Count - 1; i >= 0; i--) 
        {
            WorldManager manager = managers[i];
            manager.Step();
        }
    }

    /// <summary>
    /// Performs any initialization to set up each world
    /// Generates ants in each world
    /// </summary>
    void InitializeWorlds()
    {
        foreach(WorldManager manager in managers)
            manager.CreateAndInitializeAnts();
    }

    /// <summary>
    /// Creates (offspringCount+1) * selectionSize instances of the world. It positions them in the world in a grid
    /// Each of these worlds has a manager which is added to the 'managers' list
    /// </summary>
    void GenerateWorlds()
    {
        Vector3Int instanceSize = new Vector3Int(ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter);
        for(int i = 0; i < ConfigurationManager.Instance.Selection_pool_size; i++)
        {
            for(int j = 0; j < ConfigurationManager.Instance.Selected_offspring_count + 1; j++)
            {
                //instantiate calls the awake function and initialized class variables immedietly
                GameObject instance = Instantiate(WorldInstance, new Vector3(0, 0, 0), Quaternion.identity);
                worlds.Add(instance);
                instance.name = "Instance " + (i * (ConfigurationManager.Instance.Selected_offspring_count + 1) + j);
                instance.transform.position = new Vector3(i * (instanceSize.x + 2), 0, j * (instanceSize.z + 2));
                WorldManager manager = instance.GetComponent<WorldManager>();
                managers.Add(manager);
            }
        }
    }
}
