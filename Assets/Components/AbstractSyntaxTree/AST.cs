using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;

/*
 * Abstract classes for general node implementaiton interfaces
 */

public enum ValueType
{
    NONE,
    //WILD,
    BOOL,
    HBYTE,
    BYTE
}

//return type interfaces. 
public interface IReturnable 
{ 
    public byte Evaluate(List<byte> parameters);
}
public interface IBoolReturn : IReturnable { }
public interface IByteReturn : IReturnable { }
public interface IHByteReturn : IReturnable { }
public interface INoneReturn : IReturnable { }
public interface IConstant { }


// Base class for AST nodes
public abstract class AST_node
{
    public List<AST_node> Children  = new List<AST_node>();

    //prevent this class being made outside this file
    //the constructor will automatically create children to ensure valid state
    internal AST_node() 
    {
        generateChildren(0);
    }

    internal AST_node(int treeDepth)
    {
        generateChildren(treeDepth);
    }

    void generateChildren(int remainingDepth)
    {
        List<ValueType> childrenReturnTypes = getChildrenTypes();
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (ValueType type in childrenReturnTypes)
        {
            IEnumerable<Type> implementingTypes = null;
            if (remainingDepth <= 0)
            {
                //fill with constant nodes
                if (type == ValueType.BOOL)
                    implementingTypes = new List<Type>() { typeof(Constant_bool) };
                else if(type == ValueType.HBYTE)
                    implementingTypes = new List<Type>() { typeof(Constant_hbyte) };
                else if (type == ValueType.BYTE)
                    implementingTypes = new List<Type>() { typeof(Constant_byte) };
                else if(type == ValueType.NONE)
                    implementingTypes = new List<Type>() { typeof(SequenceNode) };
            }
            else
            {
                if (type == ValueType.NONE)
                    implementingTypes = types.Where(t => !typeof(IReturnable).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(AST_node)));
                else if (type == ValueType.BOOL)
                    implementingTypes = types.Where(t => typeof(IBoolReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(AST_node)));
                else if (type == ValueType.BYTE)
                    implementingTypes = types.Where(t => typeof(IByteReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(AST_node)));
                else if (type == ValueType.HBYTE)
                    implementingTypes = types.Where(t => typeof(IHByteReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(AST_node)));
                //else if (type == ValueType.WILD)
                //    implementingTypes = types.Where(t => typeof(IReturnable).IsAssignableFrom(t) && t.IsClass && t.IsSubclassOf(typeof(AST_node)));
                //check to ensure we found a node
                if (implementingTypes == null || implementingTypes.Count() == 0)
                    throw new Exception("CRITICAL ERROR: Tried to find AST_node with " + type.ToString() + " return but none were found");
            }
            Type node = implementingTypes.ElementAt(UnityEngine.Random.Range(0, implementingTypes.Count()));
            if(typeof(IConstant).IsAssignableFrom(node))
                Children.Add((AST_node)Activator.CreateInstance(node));
            else
                Children.Add((AST_node)Activator.CreateInstance(node, remainingDepth-1));
        }
    }

    //this function is called during construction to initialize default children. It can return an empty list
    protected abstract List<ValueType> getChildrenTypes();

    //return a string that describes the subtree starting at this node
    public abstract string GetSubtreeString();

    //helper methods
    protected string AddScopeLine(string input)
    {
        string[] lines = input.Split(Environment.NewLine);
        string output = "";
        for(int i = 0; i < lines.Length - 1; i++)
            output += "| " + lines[i] + Environment.NewLine;
        return output;
    }
}

//If something is a action it affects the agent and will cause the iterator to yeild return
public abstract class Action_node : AST_node {}

/*
 * Constant nodes
 */
sealed class Constant_bool : AST_node, IBoolReturn, IConstant
{
    private readonly byte Value;
    public Constant_bool(byte value) { this.Value = value; }

    public Constant_bool()
    {
        this.Value = (byte)UnityEngine.Random.Range(0,2);
    }

    public byte Evaluate(List<byte> parameters)
    {
        return Value;
    }

    public override string GetSubtreeString()
    {
        return Value == 0 ? "FALSE" : "TRUE";
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>();
    }
}

sealed class Constant_byte : AST_node, IByteReturn, IConstant
{
    private readonly byte Value;
    public Constant_byte(byte value) { this.Value = value; }

    public Constant_byte()
    {
        this.Value = (byte)UnityEngine.Random.Range(0, 256);
    }

    public byte Evaluate(List<byte> parameters)
    {
        return Value;
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>();
    }

    public override string GetSubtreeString()
    {
        return Value.ToString();
    }
}

sealed class Constant_hbyte : AST_node, IHByteReturn, IConstant
{
    private readonly byte Value;
    public Constant_hbyte(byte value) { this.Value = value; }

    public Constant_hbyte()
    {
        this.Value = (byte)UnityEngine.Random.Range(0, 16);
    }

    public byte Evaluate(List<byte> parameters)
    {
        return Value;
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>();
    }

    public override string GetSubtreeString()
    {
        return Value.ToString();
    }
}


/*
 * Flow nodes
*/
//Sequence node is the foundation of the code as it allows multiple statements of any type to be placed in succession
sealed public class SequenceNode : AST_node
{
    public SequenceNode(int treeDepth) : base(treeDepth) {}

    public SequenceNode() : base(0) {}

    //adds a new statement to the sequence
    public void AddStatement(AST_node statement)
    {
        Children.Add(statement);
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>();
    }

    public override string GetSubtreeString()
    {
        string subtreeString = "";
        foreach (AST_node statement in Children)
        {
            subtreeString += statement.GetSubtreeString() + Environment.NewLine;
        }
        return "SEQ" + Environment.NewLine + AddScopeLine(subtreeString);
    }
}

//if else nodes will evaluate a condition then branch accordingly. 
sealed public class IfElseNode : AST_node
{
    public IfElseNode()
    {
    }

    public IfElseNode(int treeDepth) : base(treeDepth)
    {
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BOOL, ValueType.NONE, ValueType.NONE };
    }

    public override string GetSubtreeString()
    {
        string conditionBody = Children[0].GetSubtreeString();
        string ifBody = Children[1].GetSubtreeString();
        string elseBody = Children[2].GetSubtreeString();
        return "IF ( " + conditionBody + " )" + Environment.NewLine + AddScopeLine(ifBody) + "Else" + Environment.NewLine + AddScopeLine(elseBody);
    }
}

/*
 * Logical nodes
 */
sealed public class AND_node : AST_node, IBoolReturn
{
    public AND_node()
    {
    }

    public AND_node(int treeDepth) : base(treeDepth)
    {
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BOOL, ValueType.BOOL };
    }

    public override string GetSubtreeString()
    {
        string LStatement = Children[0].GetSubtreeString();
        string RStatement = Children[1].GetSubtreeString();

        return "(" + LStatement + " AND " + RStatement + ")";
    }

    public byte Evaluate(List<byte> parameters)
    {
        byte mask = 0b00000001;
        byte val1 = (byte)(parameters[0] & mask);
        byte val2 = (byte)(parameters[1] & mask);
        return (byte)(val1 & val2);
    }
}

sealed public class OR_node : AST_node, IBoolReturn
{
    public OR_node()
    {
    }

    public OR_node(int treeDepth) : base(treeDepth)
    {
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BOOL, ValueType.BOOL };
    }

    public override string GetSubtreeString()
    {
        string LStatement = Children[0].GetSubtreeString();
        string RStatement = Children[1].GetSubtreeString();

        return "(" + LStatement + " OR " + RStatement + ")";
    }

    public byte Evaluate(List<byte> parameters)
    {
        byte mask = 0b00000001;
        byte val1 = (byte)(parameters[0] & mask);
        byte val2 = (byte)(parameters[1] & mask);
        return (byte)(val1 | val2);
    }
}

sealed public class NOT_node : AST_node, IBoolReturn
{
    public NOT_node()
    {
    }

    public NOT_node(int treeDepth) : base(treeDepth)
    {
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BOOL };
    }

    public override string GetSubtreeString()
    {
        return "(NOT " + Children[0].GetSubtreeString() + ")";
    }

    public byte Evaluate(List<byte> parameters)
    {
        byte mask = 0b00000001;
        byte val1 = (byte)(parameters[0] & mask);
        return (byte)(~val1);
    }
}

//sealed public class BOOLEQUALS_node : AST_node, IBoolReturn
//{
//    public BEQUALS_node()
//    {
//    }
//
//    public BEQUALS_node(int treeDepth) : base(treeDepth)
//    {
//    }
//
//    protected override List<ValueType> getChildrenTypes()
//    {
//        return new List<ValueType>() { ValueType.BOOL, ValueType.BOOL };
//    }
//
//    public override string GetSubtreeString()
//    {
//        string LStatement = Children[0].GetSubtreeString();
//        string RStatement = Children[1].GetSubtreeString();
//
//        return "(" + LStatement + " == " + RStatement + ")";
//    }
//
//    public byte Evaluate(List<byte> parameters)
//    {
//        byte mask = 0b00000001;
//        byte val1 = (byte)(parameters[0] & mask);
//        byte val2 = (byte)(parameters[1] & mask);
//        return val1 == val2 ? (byte)1 : (byte)0;
//    }
//}

sealed public class BYTEEQUALS_node : AST_node, IBoolReturn
{
    public BYTEEQUALS_node()
    {
    }

    public BYTEEQUALS_node(int treeDepth) : base(treeDepth)
    {
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BYTE, ValueType.BYTE };
    }

    public override string GetSubtreeString()
    {
        string LStatement = Children[0].GetSubtreeString();
        string RStatement = Children[1].GetSubtreeString();

        return "(" + LStatement + " == " + RStatement + ")";
    }

    public byte Evaluate(List<byte> parameters)
    {
        byte val1 = (byte)(parameters[0]);
        byte val2 = (byte)(parameters[1]);
        return val1 == val2 ? (byte)1 : (byte)0;
    }
}

sealed public class GT_node : AST_node, IBoolReturn
{
    public GT_node()
    {
    }

    public GT_node(int treeDepth) : base(treeDepth)
    {
    }

    protected override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BYTE, ValueType.BYTE };
    }

    public override string GetSubtreeString()
    {
        string LStatement = Children[0].GetSubtreeString();
        string RStatement = Children[1].GetSubtreeString();

        return "(" + LStatement + " > " + RStatement + ")";
    }

    public byte Evaluate(List<byte> parameters)
    {
        return parameters[0] > parameters[1] ? (byte)1 : (byte)0;
    }
}

public class AST : MonoBehaviour
{
    //type pools
    public List<Type> node_pool;

    private SequenceNode root;

    void Start()
    {
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

        root = new SequenceNode(4); //a squence node is ALWAYS the root
        root.AddStatement(new IfElseNode(3));
        root.AddStatement(new IfElseNode(3));
        Debug.Log(root.GetSubtreeString());
        //collect all the non-abstract classes that derive from AST node in the program
        //node_pool = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AST_node))).ToList();
        //tnode_pool = node_pool.Where(t => !t.IsAbstract && typeof(Terminal_node).IsAssignableFrom(t) && t != typeof(Terminal_node)).ToList();
        //inode_pool = node_pool.Where(t => !t.IsAbstract && typeof(Interior_node).IsAssignableFrom(t) && t != typeof(Interior_node)).ToList();
        //Debug.Log("Found " + node_pool.Count + " Node types"); 

    }
    
    //DEPTH is the longest chain of children under the root (distance from root node)
    //Returns the depth of the subtree with 'root' at the root (just a root will have depth 0)
    //This is a recursive function!
    //int subtreeDepth(AST_node root) {
    //    if(root == null) return 0; //just in case
    //    if(root is Terminal_node)
    //    {
    //        return 0; //terminal nodes have a depth of 0 (no children)
    //    } 
    //    else if (root is Interior_node iNode)
    //    {
    //        int maxDepth = 0;
    //        foreach(AST_node node in iNode.Children)
    //        {
    //            maxDepth = Math.Max(maxDepth, subtreeDepth(node));
    //        }
    //        return maxDepth + 1;
    //    }
    //    else
    //    {
    //        //FALL THROUGH CASE
    //        return 0;
    //    }
    //}

}



public class ASTEvaluator
{

    private struct Node_frame
    {
        public List<byte> childrenValues; //the value that each child has returned
        public AST_node node; //the node that this frame is assosiated with
        public int currentChild; //the index of the child that needs to be evaluated next
    }
    //store results of calls in a stack. This is used to evaluate nodes with multiple childnode values needed
    private Stack<Node_frame> FrameStack = new Stack<Node_frame>();

    public ASTEvaluator(AST_node root)
    {
        Node_frame frame = new Node_frame();
        frame.childrenValues = new List<byte>();
        frame.node = root;
        frame.currentChild = 0;
        FrameStack.Push(frame);
    }

    //iterate through the tree evaluating as much as possible until a action_node is reached and the node is returned
    //null is returned if the first node is reached without any action nodes to prevent infinite loop
    public Action_node getNextAction()
    {
        //if(FrameStack.Count == 0) return null; //check to make sure there is at least one stack frame. If there isn't something is wrong
        ////the top frame of the stack is the current node
        //Node_frame currentFrame = FrameStack.Peek();
        //if (currentFrame.node is SequenceNode sNode)
        //{
        //    if(currentFrame.currentChild <= sNode.Children.Count - 1)
        //    {
        //        Node_frame childFrame = new Node_frame();
        //        childFrame.node = sNode.Children[currentFrame.currentChild];
        //        childFrame.currentChild = 0;
        //        childFrame.childrenValues = new List<byte>();
        //        FrameStack.Push(childFrame);
        //        currentFrame.currentChild++;
        //    }
        //    else
        //    {
        //        //if there are no more children to iterate through then remove this frame to go up to the parent
        //        FrameStack.Pop();
        //    }
        //}
        ////else if (currentFrame.node is IfElseNode)
        //{
        //    IfElseNode node = (IfElseNode)currentFrame.node;
        //    if(currentFrame.currentChild == 0)
        //    {
        //        Node_frame childFrame = new Node_frame();
        //        childFrame.node = node.Children[0];
        //        childFrame.currentChild = 0;
        //        childFrame.childrenValues = new List<byte>();
        //        FrameStack.Push(childFrame);
        //        currentFrame.currentChild++;
        //    } 
        //    else if(currentFrame.currentChild == 1) 
        //    {
        //        Node_frame childFrame = new Node_frame();
        //        //branch
        //        if ((currentFrame.childrenValues[0] & 0b00000001) == 0b00000001)
        //        {
        //            childFrame.node = node.Children[1];
        //        }
        //        else
        //        {
        //            childFrame.node = node.Children[2];
        //        }
        //        childFrame.currentChild = 0;
        //        childFrame.childrenValues = new List<byte>();
        //        FrameStack.Push(childFrame);
        //        currentFrame.currentChild++;
        //    }
        //    else
        //    {
        //        FrameStack.Pop();
        //    }
        //}
        ////else if (currentFrame.node is Function_node fNode)
        //{
        //    if (currentFrame.currentChild <= fNode.Children.Count - 1)
        //    {
        //        Node_frame childFrame = new Node_frame();
        //        childFrame.node = fNode.Children[currentFrame.currentChild];
        //        childFrame.currentChild = 0;
        //        childFrame.childrenValues = new List<byte>();
        //        FrameStack.Push(childFrame);
        //        currentFrame.currentChild++;
        //    } 
        //    else
        //    {
        //        //evaluate, set ehe next node and pop itself
        //        byte result = fNode.Evaluate(currentFrame.childrenValues);
        //        FrameStack.Pop();
        //        FrameStack.Peek().childrenValues.Add(result);
        //    }
        //}
        //else if (currentFrame.node is Terminal_node tNode)
        //{
        //    //evaluate, set ehe next node and pop itself
        //    byte result = tNode.GetValue();
        //    FrameStack.Pop();
        //    FrameStack.Peek().childrenValues.Add(result);
        //    
        //}
        return null;
    }
}