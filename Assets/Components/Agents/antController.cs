using Antymology.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

interface IAntNode
{
    void setAnt(WorldManager controller);
}

public class AntHealth : Leaf_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setAnt(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        //Ensures that health above 255 doesn't get wrapped and instead is treated as 255
        return (byte)Math.Min(controller.getCurrentProcessingAnt().currentHealth, byte.MaxValue);
    }

    public override string GetSubtreeString()
    {
        return "ANT_HEALTH";
    }
}

public class AntsHere : Leaf_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setAnt(WorldManager antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "ANTS_HERE";
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.getCurrentProcessingAnt();
        return (byte)controller.CountAntsAtBlock(ant.blockPos.x, ant.blockPos.y, ant.blockPos.z);
    }
}

public class MoveForward : Leaf_node, IAction, IAntNode
{
    WorldManager controller;
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.getCurrentProcessingAnt();
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
    public void setAnt(WorldManager antController)
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
    public void setAnt(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.getCurrentProcessingAnt();
        return (byte)Math.Min(controller.pheromoneAtBlock(ant.blockPos.x, ant.blockPos.y, ant.blockPos.z), byte.MaxValue);
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
    public void setAnt(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.getCurrentProcessingAnt();
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
    public void setAnt(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.getCurrentProcessingAnt();
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

    public void setAnt(WorldManager antController)
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

    public Vector3Int heading = new Vector3Int(0,1,0);
    public Vector3Int blockPos = Vector3Int.zero;

    enum antType { WORKER, QUEEN };
    antType type = antType.WORKER;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        blockPos = Vector3Int.FloorToInt(transform.position);
    }

    // Update is called once per frame
    //void Update()
    //{
    //    int y = 0;
    //    while (world.GetBlock(blockPos.x, y, blockPos.z) is not AirBlock)
    //    {
    //        y++;
    //    }
    //    blockPos.y = y;
    //    transform.position = blockPos - new Vector3(0,0.5f,0);
    //}

    //will cause the action to parse its brain until the next ant action node is encountered
    void step()
    {
        //SIMULATION STEPS
        //1. Save state for every ant like phereomone concentrations and ant count. This avoids effects of sequential processing
        //2. Move the AST evaluator to the next action node and execute it
    }

    bool digBelow()
    {
        return true;
    }
}
