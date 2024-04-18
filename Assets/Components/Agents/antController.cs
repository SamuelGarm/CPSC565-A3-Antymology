using Antymology.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

interface IAntNode
{
    void setAnt(antController antController);
}

public class AntHealth : Leaf_node, IByteReturn, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        throw new NotImplementedException();
    }

    public override string GetSubtreeString()
    {
        return "ANT_HEALTH";
    }
}

public class AntsHere : Leaf_node, IByteReturn, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "ANTS_HERE";
    }

    public byte Evaluate(List<byte> parameters)
    {
        throw new NotImplementedException();
    }
}

public class MoveForward : Leaf_node, IAction, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
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
    antController controller;
    public void setAnt(antController antController)
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
    antController controller;
    public void setAnt(antController antController)
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
    antController controller;
    public void setAnt(antController antController)
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
    antController controller;
    public void setAnt(antController antController)
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
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "TRANSFER ( " + Children[0].GetSubtreeString() + " ) ";
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BYTE };
    }
}

public class DepositPheromone : Internal_node, IAction, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "DEPOSIT ( ID:" + Children[0].GetSubtreeString() + " VAL: " + Children[1].GetSubtreeString() + " )";
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE, ValueType.HBYTE };
    }
}

public class GetValue : Internal_node, IBoolReturn, IByteReturn, IHByteReturn, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        throw new NotImplementedException();
    }

    public override string GetSubtreeString()
    {
        return "GET_VALUE ( " + Children[0].GetSubtreeString() + " )";
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE };
    }
}

public class SetValue : Internal_node, IAction, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public override string GetSubtreeString()
    {
        return "SET_VALUE ( ID:" + Children[0].GetSubtreeString() + " VAL: " + Children[1].GetSubtreeString() + " )";
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE, ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE };
    }
}

/*
 * Sensors
 */
public class SensePheromone : Internal_node, IByteReturn, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        throw new NotImplementedException();
    }

    public override string GetSubtreeString()
    {
        return "SENSE_PHEROMONE ( ID:" + Children[0].GetSubtreeString() + " POS:" + Children[1].GetSubtreeString() + " )";
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.HBYTE, ValueType.HBYTE };
    }
}

public class SenseBlockBelow : Leaf_node, IHByteReturn, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        throw new NotImplementedException();
    }

    public override string GetSubtreeString()
    {
        return "SENSE_BELOW";
    }
}

public class SenseBlockAhead : Leaf_node, IHByteReturn, IAntNode
{
    antController controller;
    public void setAnt(antController antController)
    {
        controller = antController;
    }

    public byte Evaluate(List<byte> parameters)
    {
        throw new NotImplementedException();
    }

    public override string GetSubtreeString()
    {
        return "SENSE_AHEAD";
    }
}



public class antController : MonoBehaviour
{
    
    public WorldManager world;

    byte[] values;
    int maxHealth = 100;
    int currentHealth = 0;
    int healthDec = -1;

    private Vector3Int blockPos = Vector3Int.zero;

    enum antType { WORKER, QUEEN }
    antType type;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        blockPos = Vector3Int.FloorToInt(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        int y = 0;
        while (world.GetBlock(blockPos.x, y, blockPos.z) is not AirBlock)
        {
            y++;
        }
        blockPos.y = y;
        transform.position = blockPos - new Vector3(0,0.5f,0);
    }

    bool digBelow()
    {
        return true;
    }
}
