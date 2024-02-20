using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using System.Reflection;
using System.Linq;
using UnityEditor.Build.Reporting;

/*
 * Abstract classes for general node implementaiton interfaces
 */



// Base class for AST nodes
public abstract class ASTNode
{
    //return a string that describes the subtree starting at this node
    public abstract string GetSubtreeString();
}

//functions do not control program flow. They are statements
public abstract class Function_node : ASTNode
{
    public List<Function_node> Children = new List<Function_node>();
    public abstract byte Evaluate(List<byte> parameters);
}

//terminal will have no children
public abstract class Terminal_node : ASTNode 
{
    public abstract byte GetValue();
}

//If something is a action it affects the agent and will cause the iterator to yeild return
public abstract class Action_node : Function_node { }

/*
 * More specific nodes that don't fall into functions or terminal nodes neatly
 */
//Sequence node is the foundation of the code as it allows multiple statements of any type to be placed in succession
public class SequenceNode : ASTNode
{
    public List<ASTNode> Children { get; set; } = new List<ASTNode>();
    //adds a new statement to the sequence
    public void AddStatement(ASTNode statement)
    {
        Children.Add(statement);
    }

    public override string GetSubtreeString()
    {
        string subtreeString = "";
        foreach (ASTNode statement in Children)
        {
            subtreeString += statement.GetSubtreeString() + Environment.NewLine;
        }
        return subtreeString;
    }
}

//if else nodes will evaluate a condition then branch accordingly. 
public class IfElseNode : ASTNode
{
    public List<ASTNode> Children { get; set; } = new List<ASTNode>();
    public IfElseNode(Terminal_node condition, SequenceNode ifBody, SequenceNode elseBody)
    {
        Children.Add(condition);
        Children.Add(ifBody);
        Children.Add(elseBody);
    }

    public override string GetSubtreeString()
    {
        string conditionBody = Children[0].GetSubtreeString();

        string ifBody = "";
        foreach (string line in Children[1].GetSubtreeString().Split(Environment.NewLine))
        {
            ifBody += "| " + line + Environment.NewLine;
        }

        string elseBody = "";
        foreach (string line in Children[2].GetSubtreeString().Split(Environment.NewLine))
        {
            elseBody += "| " + line + Environment.NewLine;
        }

        return "If ( " + ifBody + " )" + Environment.NewLine + ifBody + "Else" + Environment.NewLine + elseBody;
    }
}

/*
 * Implementations
 */

//Value nodes contain a single Byte value
public class Value_node : Terminal_node
{
    public byte Value { get; }
    public Value_node(byte _value)
    {
        Value = _value;
    }
    public override string GetSubtreeString()
    {
        return Value.ToString() + Environment.NewLine;
    }

    public override byte GetValue()
    {
        return Value;
    }
}

public class AND_node : Function_node
{

    public override string GetSubtreeString()
    {
        string LStatement = "";
        foreach (string line in Children[1].GetSubtreeString().Split(Environment.NewLine))
        {
            LStatement += "| " + line + Environment.NewLine;
        }

        string RStatement = "";
        foreach (string line in Children[2].GetSubtreeString().Split(Environment.NewLine))
        {
            RStatement += "| " + line + Environment.NewLine;
        }

        return LStatement + " && " + RStatement;
    }

    public override byte Evaluate(List<byte> parameters)
    {
        byte mask = 0b00000001;
        byte val1 = (byte)(parameters[0] & mask);
        byte val2 = (byte)(parameters[1] & mask);
        return (byte)(val1 & val2);
    }
}

public class OR_node : Function_node
{

    public override string GetSubtreeString()
    {
        string LStatement = "";
        foreach (string line in Children[1].GetSubtreeString().Split(Environment.NewLine))
        {
            LStatement += "| " + line + Environment.NewLine;
        }

        string RStatement = "";
        foreach (string line in Children[2].GetSubtreeString().Split(Environment.NewLine))
        {
            RStatement += "| " + line + Environment.NewLine;
        }

        return LStatement + " && " + RStatement;
    }

    public override byte Evaluate(List<byte> parameters)
    {
        byte mask = 0b00000001;
        byte val1 = (byte)(parameters[0] & mask);
        byte val2 = (byte)(parameters[1] & mask);
        return (byte)(val1 & val2);
    }
}


public class AST : MonoBehaviour
{
    public List<Type> node_pool;

    private ASTNode root;

    void Start()
    {
        root = new SequenceNode(); //a squence node is ALWAYS the root
        //collect all the non-abstract classes that derive from AST node in the program
        node_pool = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ASTNode))).ToList();
        Debug.Log("Found " + node_pool.Count + " Nodes"); 
    }

}

public class ASTEvaluator
{

    private struct Node_frame
    {
        public List<byte> childrenValues; //the value that each child has returned
        public ASTNode node; //the node that this frame is assosiated with
    }
    //store results of calls in a stack. This is used to evaluate nodes with multiple childnode values needed
    private Stack<Node_frame> FrameStack = new Stack<Node_frame>();

    public ASTEvaluator(ASTNode initial)
    {
        Node_frame frame = new Node_frame();
        frame.childrenValues = new List<byte>();
        frame.node = initial;
        FrameStack.Push(frame);
    }

    //iterate through the tree evaluating as much as possible until a action_node is reached and the node is returned
    public Action_node getNextAction()
    {
        //the top frame of the stack is the current node
        Node_frame currentFrame = FrameStack.Peek();
        if(currentFrame.node.GetType().IsSubclassOf(typeof(Terminal_node)))
        {

        }
        return null;
    }
}