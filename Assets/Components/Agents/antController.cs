using Antymology.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using UnityEngine.UIElements.Experimental;
using System.Linq;

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
        return (byte)controller.CountAntsAtBlock(ant.Position.x, ant.Position.z);
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
        return ant.values[parameters[0] & 0b00001111];
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
public class SensePheromoneAround : Internal_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        //Forward, Right, Up offsets
        Vector2Int[] senseOffsets = new Vector2Int[]
        {
            new Vector2Int( 1, 0 ) ,
            new Vector2Int( 1, 1 ) ,
            new Vector2Int( 0, 1 ) ,
            new Vector2Int( -1, 1) ,
            new Vector2Int( -1, 0) ,
            new Vector2Int( -1, -1) ,
            new Vector2Int( 0, -1) ,
            new Vector2Int( 1, -1)
        };

        Vector2Int offsets = senseOffsets[(byte)(parameters[1] & 0b00000111)];
        Vector3Int senseLocation = ant.Position;
        Vector3Int forward = ant.heading;
        Vector3Int right = new Vector3Int(forward.z, 0, -forward.x);

        senseLocation += forward * offsets[0];
        senseLocation += right * offsets[1];

        return (byte)Math.Min(controller.pheromoneAtBlock(senseLocation.x, senseLocation.z, (byte)(parameters[0] & 0b00001111)), byte.MaxValue);
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

public class SensePheromoneHere : Internal_node, IByteReturn, IAntNode
{
    WorldManager controller;
    public void setWorld(WorldManager antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        return (byte)Math.Min(controller.pheromoneAtBlock(ant.Position.x, ant.Position.z, (byte)(parameters[0] & 0b00001111)), byte.MaxValue);
    }

    public override string GetSubtreeString()
    {
        return "SENSE_PHEROMONE_HERE";
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE };
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
        AbstractBlock block = controller.GetBlock(ant.Position.x, ant.Position.y - 1, ant.Position.z);
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
        Vector3Int toCheck = ant.Position + ant.heading;
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

public class QueenHere : Leaf_node, IBoolReturn, IAntNode
{
    WorldManager controller;
    public byte Evaluate(List<byte> parameters)
    {
        AntController ant = controller.GetCurrentProcessingAnt();
        List<AntController> ants = controller.antsAtBlock(ant.Position.x, ant.Position.y, ant.Position.z);
        foreach(AntController e in ants)
        {
            if (e.type == AntController.antType.QUEEN)
                return 1;
        }
        return 0;
    }

    public override string GetSubtreeString()
    {
        return "QUEEN_HERE";
    }

    public void setWorld(WorldManager controller)
    {
        this.controller = controller;
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
    int maxHealth = 0;
    public int currentHealth = 0;
    int healthDec = -1;

    public bool printBrain = false;
    public bool printMutationHistory = false;

    public Vector3Int heading;
    public Vector3Int Position = Vector3Int.zero;

    public enum antType { WORKER, QUEEN };
    public antType type = antType.WORKER;

    ASTEvaluator evaluator = null;


    private void Awake()
    {
        maxHealth = ConfigurationManager.Instance.Max_ant_health;
        currentHealth = maxHealth;
        Position = Vector3Int.FloorToInt(transform.localPosition);
        heading = new Vector3Int(1, 0, 0);
        brain = new AST();
        evaluator = new ASTEvaluator(brain);
    }

    public void Update()
    {
        if(printBrain)
        {
            printBrain = false;
            Debug.Log(brain.root.GetSubtreeString());
        }
        if (printMutationHistory)
        {
            printMutationHistory = false;
            string History = "";
            foreach (string e in brain.MutationHistory)
                History += e + Environment.NewLine + Environment.NewLine;
            Debug.Log(History);
        }
    }

    public void SetBrain(AST brain)
    {
        this.brain = brain;
        evaluator = new ASTEvaluator(brain);
    }

    public void MoveToBlockCoord(Vector3Int block)
    {
        Position.x = Math.Clamp(block.x, 0, ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter - 1);
        Position.y = Math.Max(block.y,0);
        Position.z = Math.Clamp(block.z, 0, ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter - 1);
        transform.localPosition = Position; 
    }

    //will cause the action to parse its brain until the next ant action node is encountered
    public void Step()
    {
        //handle health
        currentHealth += healthDec;
        //standing on a acid block is more costly
        if (world.GetBlock(Position.x, Position.y - 1, Position.z) is AcidicBlock)
            currentHealth += healthDec;

        Tuple<Type, byte[]> frame = evaluator.getNextAction();
        if (frame == null)
            return;
        Type action = frame.Item1;
        byte[] arguments = frame.Item2;

        if (action == typeof(MoveForward))
        {
            Vector3Int bCoordAhead = Position + heading;
            int nextPosHeight = 0;
            while (world.GetBlock(bCoordAhead.x, nextPosHeight, bCoordAhead.z) is not AirBlock)
                nextPosHeight++;

            if (Math.Abs(Position.y - nextPosHeight) <= 2)
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
            if(world.GetBlock(Position.x, Position.y - 1, Position.z) is MulchBlock && world.antsAtBlock(Position.x, Position.y, Position.z).Count == 1)
            {
                world.SetBlock(Position.x, Position.y - 1, Position.z, new AirBlock());
                currentHealth = maxHealth;
            }
        }
        else if (action == typeof(Dig))
        {
            if (world.GetBlock(Position.x, Position.y - 1, Position.z) is NestBlock)
                world.nestBlocks--;
            if (world.GetBlock(Position.x, Position.y - 1, Position.z) is not ContainerBlock)
                world.SetBlock(Position.x, Position.y - 1, Position.z, new AirBlock());
        }
        else if (action == typeof(TransferEnergy))
        {
            List<AntController> ants = world.antsAtBlock(Position.x, Position.y, Position.z);
            int amountPerAnt = currentHealth / ants.Count;
            foreach(AntController ant in ants)
                ant.currentHealth += amountPerAnt;
            currentHealth -= amountPerAnt * ants.Count;
        }
        else if (action == typeof(DepositPheromone))
        {
            world.addPheromone(Position.x, Position.z, arguments[0], arguments[1]);
        }
        else if (action == typeof(SetValue))
        {
            values[arguments[0] & 0b00001111] = arguments[1];
        }
        else if (action == typeof(CreateNest))
        {
            currentHealth -= maxHealth / 3;
            world.SetBlock(Position.x, Position.y, Position.z, new NestBlock());
            world.nestBlocks++;
        }
    }
}
