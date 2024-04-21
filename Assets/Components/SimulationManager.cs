using Antymology.Terrain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{

    public GameObject WorldInstance;

    private List<WorldManager> managers = new List<WorldManager>();
    private List<GameObject> worlds = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateWorlds();
        InitializeWorlds();
    }


    private float stepTime = 0;
    public bool run = false;
    public bool step = false;

    // Update is called once per frame
    private void Update()
    {
        if (!run && !step)
        {
            stepTime = 0;
            return;
        }
        stepTime += Time.deltaTime;
        if (stepTime > 0.1 || step)//ConfigurationManager.Instance.minTimeDelta)
        {
            step = false;
            stepTime = 0;
            Step();
            int antCount = 0;
            foreach(WorldManager manager in managers)
            {
                antCount += manager.ants.Count;
            }
            //Debug.Log("Ants in simulation: " + antCount);
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
                Debug.Log("Everyone is dead :(" + Environment.NewLine + "Best world made " + mostNestBlocks + "Nest blocks!");
                Debug.Log("Worker brain: " + Environment.NewLine + bestWorkerBrain.root.GetSubtreeString());
                Debug.Log("Queen brain: " + Environment.NewLine + bestQueenBrain.root.GetSubtreeString());
                MutateAndReset();
            }
        }
    }

    void MutateAndReset()
    {
        //choose n best worlds
        List<WorldManager> selected = managers.OrderByDescending(t => t.nestBlocks).Take(ConfigurationManager.Instance.Selection_pool_size).ToList();
        List<Tuple<AST, AST>> brains = new List<Tuple<AST, AST>>();
        foreach (WorldManager manager in selected)
            brains.Add(new Tuple<AST, AST>(new AST(manager.queenAST), new AST(manager.workerAST)));

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
            managers[i * (ConfigurationManager.Instance.Selected_offspring_count + 1)].SetBrains(brains[i].Item1, brains[i].Item2);
            for (int j = 1; j < ConfigurationManager.Instance.Selected_offspring_count + 1; j++)
            {
                brains[i].Item1.RandomMutation();
                brains[i].Item2.RandomMutation();
                managers[i * (ConfigurationManager.Instance.Selected_offspring_count + 1) + j].SetBrains(brains[i].Item1, brains[i].Item2);
            }
        }

        InitializeWorlds();
    }

    void Step()
    {
        for (int i = managers.Count - 1; i >= 0; i--) 
        {
            WorldManager manager = managers[i];
            manager.Step();
        }
    }

    void InitializeWorlds()
    {
        foreach(WorldManager manager in managers)
            manager.CreateAndInitializeAnts();
    }

    void GenerateWorlds()
    {
        Vector3Int instanceSize = new Vector3Int(ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter);
        for(int i = 0; i < ConfigurationManager.Instance.Selection_pool_size; i++)
        {
            for(int j = 0; j < ConfigurationManager.Instance.Selected_offspring_count + 1; j++)
            {
                GameObject instance = Instantiate(WorldInstance, new Vector3(0, 0, 0), Quaternion.identity);
                worlds.Add(instance);
                instance.name = "Instance " + (i * (ConfigurationManager.Instance.Selected_offspring_count + 1) + j);
                instance.transform.position = new Vector3(i * (instanceSize.x + 2), 0, j * (instanceSize.z + 2));
                WorldManager manager = instance.GetComponent<WorldManager>();
                managers.Add(manager);
                for (int a = 0; a < 10; a++)
                {
                    manager.queenAST.ExpandMutate();
                    manager.workerAST.ExpandMutate();
                }
            }
        }
    }
}
