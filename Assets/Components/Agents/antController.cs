using Antymology.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

interface IAntNode
{
    void setWorld(WorldManager controller);
}

public class AntHealth : Leaf_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        //Ensures that health above 255 doesn't get wrapped and instead is treated as 255
        return (byte)Math.Min(controller.GetCurrentProcessingAnt().currentHealth, byte.MaxValue);
    }

    public override string GetSubtreeString()
    {
        return "ANT_HEALTH";
    }
}

public class AntsHere : Leaf_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "ANTS_HERE";
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        return (byte)controller.CountAntsAtBlock(ant.blockPos.x, ant.blockPos.z);
    }
}

public class MoveForward : Leaf_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "MOVE_FORWARD";
    }
}

public class TurnRight : Leaf_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "TURN_RIGHT";
    }
}

public class TurnLeft : Leaf_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "TURN_LEFT";
    }
}

public class Consume : Leaf_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "CONSUME";
    }
}

public class Dig : Leaf_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "DIG";
    }
}

public class TransferEnergy : Internal_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "TRANSFER ( " + Children[0].GetSubtreeString() + " ) ";
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BYTE };
    }
}

public class DepositPheromone : Internal_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "DEPOSIT ( ID:" + Children[0].GetSubtreeString() + " VAL: " + Children[1].GetSubtreeString() + " )";
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE, ValueType.HBYTE };
    }
}

public class GetValue : Internal_node, IBoolReturn, IByteReturn, IHByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        return ant.values[parameters[0]];
    }

    public override string GetSubtreeString()
    {
        return "GET_VALUE ( " + Children[0].GetSubtreeString() + " )";
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE };
    }
}

public class SetValue : Internal_node, IAction, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "SET_VALUE ( ID:" + Children[0].GetSubtreeString() + " VAL: " + Children[1].GetSubtreeString() + " )";
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE, ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE };
    }
}

/*
 * Sensors
 */
public class SensePheromone : Internal_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        return (byte)Math.Min(controller.pheromoneAtBlock(ant.blockPos.x, ant.blockPos.z, parameters[0]), byte.MaxValue);
    }

    public override string GetSubtreeString()
    {
        return "SENSE_PHEROMONE ( ID:" + Children[0].GetSubtreeString() + " POS:" + Children[1].GetSubtreeString() + " )";
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE, ValueType.HBYTE };
    }
}

public class SenseBlockBelow : Leaf_node, IHByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        AbstractBlock block = controller.GetBlock(ant.blockPos.x, ant.blockPos.y - 1, ant.blockPos.z);
        if (block is AcidicBlock)
            return 0;
        if (block is ContainerBlock)
            return 1;
        if (block is GrassBlock)
            return 2;
        if (block is MulchBlock)
            return 3;
        if (block is NestBlock)
            return 4;
        if (block is StoneBlock)
            return 5;
        return 6; //just in case
    }

    public override string GetSubtreeString()
    {
        return "SENSE_BELOW";
    }
}

public class SenseBlockAhead : Leaf_node, IHByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        Vector3Int toCheck = ant.blockPos + ant.heading;
        AbstractBlock block = controller.GetBlock(toCheck.x, toCheck.y, toCheck.z);
        if (block is AcidicBlock)
            return 0;
        if (block is ContainerBlock)
            return 1;
        if (block is GrassBlock)
            return 2;
        if (block is MulchBlock)
            return 3;
        if (block is NestBlock)
            return 4;
        if (block is StoneBlock)
            return 5;
        return 6; //just in case
    }

    public override string GetSubtreeString()
    {
        return "SENSE_AHEAD";
    }
}

/*
 * Queen specific functions
 */
public class CreateNest : Leaf_node, IAntNode, IAction
{
    WorldManager ant;
    public override string GetSubtreeString()
    {
        return "CREATE_NEST";
    }

    public void setWorld(WorldManager antController)
    {
        ant = antController;
    }
}

/*
 * Controller class
 */
public class AntController : MonoBehaviour
{
    public AST brain;
    public WorldManager world;

    public byte[] values = {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
    int maxHealth = 100;
    public int currentHealth = 0;
    int healthDec = -1;

    public Vector3Int heading;
    public Vector3Int blockPos = Vector3Int.zero;

    public enum antType { WORKER, QUEEN };
    public antType type = antType.WORKER;

    ASTEvaluator evaluator;

    // Start is called before the first frame update
    void Start()
    {
        evaluator = new ASTEvaluator(brain);
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        blockPos = Vector3Int.FloorToInt(transform.position);
        heading = new Vector3Int(1, 0, 0);
    }

    public void MoveToBlockCoord(Vector3Int block)
    {
        blockPos.x = Math.Clamp(block.x, 0, ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter - 1);
        blockPos.y = Math.Max(block.y,0);
        blockPos.z = Math.Clamp(block.z, 0, ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter - 1);
        transform.position = blockPos - new Vector3(0, 0.5f, 0);
    }

    //will cause the action to parse its brain until the next ant action node is encountered
    public void Step()
    {
        //handle health
        currentHealth += healthDec;
        //standing on a acid block is more costly
        if (world.GetBlock(blockPos.x, blockPos.y - 1, blockPos.z) is AcidicBlock)
            currentHealth += healthDec;

        Tuple<Type, byte[]> frame = evaluator.getNextAction();
        Type action = frame.Item1;
        byte[] arguments = frame.Item2;

        //Debug.Log(action.ToString());

        if (action == typeof(MoveForward))
        {
            Vector3Int bCoordAhead = blockPos + heading;
            int nextPosHeight = 0;
            while (world.GetBlock(bCoordAhead.x, nextPosHeight, bCoordAhead.z) is not AirBlock)
                nextPosHeight++;

            if (Math.Abs(blockPos.y - nextPosHeight) <= 2)
                MoveToBlockCoord(new Vector3Int(bCoordAhead.x, nextPosHeight, bCoordAhead.z));
        } 
        else if(action == typeof(TurnRight)) {
            int[] old = { heading.x, heading.z };
            heading.x = old[1];
            heading.z = -old[0];
        }
        else if (action == typeof(TurnLeft))
        {
            int[] old = { heading.x, heading.z };
            heading.x = -old[1];
            heading.z = old[0];
        }
        else if (action == typeof(Consume))
        {
            if(world.GetBlock(blockPos.x, blockPos.y - 1, blockPos.z) is MulchBlock && world.antsAtBlock(blockPos.x, blockPos.y, blockPos.z).Count == 1)
            {
                world.SetBlock(blockPos.x, blockPos.y - 1, blockPos.z, new AirBlock());
                currentHealth = maxHealth;
            }
        }
        else if (action == typeof(Dig))
        {
            if (world.GetBlock(blockPos.x, blockPos.y - 1, blockPos.z) is not ContainerBlock)
                world.SetBlock(blockPos.x, blockPos.y - 1, blockPos.z, new AirBlock());
        }
        else if (action == typeof(TransferEnergy))
        {
            List<AntController> ants = world.antsAtBlock(blockPos.x, blockPos.y, blockPos.z);
            int amountPerAnt = currentHealth / ants.Count;
            foreach(AntController ant in ants)
                ant.currentHealth += amountPerAnt;
            currentHealth -= amountPerAnt * ants.Count;
        }
        else if (action == typeof(DepositPheromone))
        {
            world.addPheromone(blockPos.x, blockPos.z, arguments[0], arguments[1]);
        }
        else if (action == typeof(SetValue))
        {
            values[arguments[0]] = arguments[1];
        }
        else if (action == typeof(CreateNest))
        {
            currentHealth -= maxHealth / 3;
            world.SetBlock(blockPos.x, blockPos.y, blockPos.z, new NestBlock());
        }
    }
}
