using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Collections;
using UnityEditor.Experimental.GraphView;

/*
 * Abstract classes for general node implementaiton interfaces
 */

public enum ValueType
{
    NONE =  0b00000001,
    BOOL =  0b00000010,
    HBYTE = 0b00000100,
    BYTE =  0b00001000
}


//return type interfaces. 
public interface IReturnable 
{ 
    public byte Evaluate(List<byte> parameters);
}
public interface IBoolReturn : IReturnable { }
public interface IByteReturn : IReturnable { }
public interface IHByteReturn : IReturnable { }

//this interface is used if the node is an action to be executed
public interface IAction { }


// Base class for AST nodes
public abstract class AST_node
{
    public AST_node parent;
    //prevent this class being made outside this file
    //the constructor will automatically create children to ensure valid state
    internal AST_node() {}

    //return a string that describes the subtree starting at this node
    public abstract string GetSubtreeString();

    //helper methods
    protected string AddScopeLine(string input)
    {
        string[] lines = input.Split(Environment.NewLine);
        string output = "";
        for(int i = 0; i < lines.Length; i++)
            output += "| " + lines[i] + Environment.NewLine;
        return output.Trim(Environment.NewLine.ToCharArray());
    }
}

//internal nodes have children
public abstract class Internal_node : AST_node
{
    public Internal_node()
    {
        generateChildren();
    }

    public List<AST_node> Children = new List<AST_node>();

    void generateChildren()
    {
        List<ValueType> childrenReturnTypes = getChildrenTypes();
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (ValueType type in childrenReturnTypes)
        {
            List<Type> implementingTypes = new List<Type>();

            if ((type & ValueType.NONE) > 0)
                implementingTypes.AddRange(types.Where(t => !typeof(IReturnable).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Leaf_node))));
            if ((type & ValueType.BOOL) > 0)
                implementingTypes.AddRange(types.Where(t => typeof(IBoolReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Leaf_node))));
            if ((type & ValueType.BYTE) > 0)
                implementingTypes.AddRange(types.Where(t => typeof(IByteReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Leaf_node))));
            if ((type & ValueType.HBYTE) > 0)
                implementingTypes.AddRange(types.Where(t => typeof(IHByteReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(Leaf_node))));

            //check to ensure we found a node
            if (implementingTypes.Count() == 0)
                throw new Exception("CRITICAL ERROR: Tried to find AST_node with " + type + " return but none were found");
            //}
            Type nodeType = implementingTypes.ElementAt(UnityEngine.Random.Range(0, implementingTypes.Count()));
            //if (typeof(Internal_node).IsAssignableFrom(node))
            AST_node node = (AST_node)Activator.CreateInstance(nodeType);
            Children.Add(node);
            node.parent = this;

            //else
                //Children.Add((AST_node)Activator.CreateInstance(node, remainingDepth - 1));
        }
    }

    //this function is called during construction to initialize default children. It can return an empty list
    public abstract List<ValueType> getChildrenTypes();
}

public abstract class Leaf_node : AST_node
{
}


/*
 * Constant nodes
 */
sealed class Constant_bool : Leaf_node, IBoolReturn
{
    private readonly byte Value;
    public Constant_bool(byte value) { this.Value = value; }

    public Constant_bool()
    {
        this.Value = (byte)UnityEngine.Random.Range(0, 2);
    }

    public byte Evaluate(List<byte> parameters)
    {
        return Value;
    }

    public override string GetSubtreeString()
    {
        return Value == 0 ? "FALSE" : "TRUE";
    }
}

sealed class Constant_byte : Leaf_node, IByteReturn
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

    public override string GetSubtreeString()
    {
        return Value.ToString();
    }
}

sealed class Constant_hbyte : Leaf_node, IHByteReturn
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

    public override string GetSubtreeString()
    {
        return Value.ToString();
    }
}

sealed class NOOP : Leaf_node, IAction
{
    public override string GetSubtreeString()
    {
        return "NOOP";
    }
}

/*
 * Flow nodes
*/
//Sequence node is the foundation of the code as it allows multiple statements of any type to be placed in succession
sealed public class SequenceNode : Internal_node
{
    public SequenceNode() {}

    public override List<ValueType> getChildrenTypes()
    {
        List<ValueType> ret = new List<ValueType>();
        for (int i = 0; i < Children.Count; i++)
            ret.Add(ValueType.NONE);
        return ret;
    }

    public override string GetSubtreeString()
    {
        string subtreeString = "";
        foreach (AST_node statement in Children)
        {
            subtreeString += statement.GetSubtreeString() + Environment.NewLine;
        }
        return "SEQ" + Environment.NewLine + AddScopeLine(subtreeString.Trim(Environment.NewLine.ToCharArray()));
    }
}

//if else nodes will evaluate a condition then branch accordingly. 
sealed public class IfElseNode : Internal_node
{
    public IfElseNode()
    {
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE, ValueType.NONE, ValueType.NONE };
    }

    public override string GetSubtreeString()
    {
        string conditionBody = Children[0].GetSubtreeString();
        string ifBody = Children[1].GetSubtreeString();
        string elseBody = Children[2].GetSubtreeString();
        return "IF ( " + conditionBody + " )" + Environment.NewLine + AddScopeLine(ifBody) + Environment.NewLine + "Else" + Environment.NewLine + AddScopeLine(elseBody);
    }
}

/*
 * Logical nodes
 */
sealed public class AND_node : Internal_node, IBoolReturn
{
    public AND_node()
    {
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { 
            ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE, 
            ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE };
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

sealed public class OR_node : Internal_node, IBoolReturn
{
    public OR_node()
    {
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() {
            ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE, 
            ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE };
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

sealed public class NOT_node : Internal_node, IBoolReturn
{
    public NOT_node()
    {
    }

    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() {
            ValueType.BOOL | ValueType.HBYTE | ValueType.BYTE };
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

//sealed public class BYTEEQUALS_node : AST_node, IBoolReturn
//{
//    public BYTEEQUALS_node()
//    {
//    }
//
//    public BYTEEQUALS_node(int treeDepth) : base(treeDepth)
//    {
//    }
//
//    protected override List<ValueType> getChildrenTypes()
//    {
//        return new List<ValueType>() { ValueType.BYTE, ValueType.BYTE };
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
//        byte val1 = (byte)(parameters[0]);
//        byte val2 = (byte)(parameters[1]);
//        return val1 == val2 ? (byte)1 : (byte)0;
//    }
//}

sealed public class GT_node : Internal_node, IBoolReturn
{
    public GT_node()
    {
    }


    public override List<ValueType> getChildrenTypes()
    {
        return new List<ValueType>() { 
            ValueType.BYTE | ValueType.HBYTE, 
            ValueType.BYTE | ValueType.HBYTE };
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
    List<AST_node> nodes = new List<AST_node>();

    private SequenceNode root;

    void Start()
    {
        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

        root = new SequenceNode(); //a squence node is ALWAYS the root
        root.Children.Add(new SequenceNode());
        root.Children[0].parent = root;
        nodes.AddRange(root.Children); //note, root is excluded from this list since it must be treated special

        for(int i = 0; i < 30; i++)
        {
            Debug.Log(root.GetSubtreeString());
            if (!Expand())
                Debug.Log("Failed to expand");
            //decide if we want to add or mutate
            //if(UnityEngine.Random.Range(0,10) < 6)
            //{
            //    //add to a sequence
            //
            //} else
            //{
            //    //mutate
            //}
        } 

        Debug.Log(root.GetSubtreeString());
    } 

    bool PointMutate()
    {
        //try to find a point that can be mutated with a valid return value and children types
        return false;
    }

    bool Shrink()
    {
        //choose a random node and replace it with a leaf node
        return false;
    }

    //expands leaf nodes into internal nodes
    bool Expand()
    {
        //choose a leaf node (or a sequence) and add/replace it with a more complex node
        List<AST_node> candidates = new List<AST_node>();
        foreach (Leaf_node l in nodes.OfType<Leaf_node>().ToList())
            candidates.Add(l);
        foreach (SequenceNode l in nodes.OfType<SequenceNode>().ToList())
            candidates.Add(l);
        
        while(candidates.Count > 0)
        {
            //find all possible replacements for a random candidate
            AST_node candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Internal_node parent = (Internal_node)candidate.parent;
            
            //use the parent child parameters to determine what nodes are good
            int indexInParent = parent.Children.IndexOf(candidate);
            ValueType retType = parent.getChildrenTypes()[indexInParent];
            List<Type> replacements = NodesWithReturnType(retType);

            //filter replacements to only be internal nodes
            for(int i = replacements.Count-1; i >= 0; i--)
            {
                if (!replacements[i].IsSubclassOf(typeof(Internal_node)))
                    replacements.RemoveAt(i);
            }

            while (replacements.Count > 0)
            {
                int typeIndex = UnityEngine.Random.Range(0, replacements.Count);

                Internal_node replacement = (Internal_node)Activator.CreateInstance(replacements[typeIndex]);
                
                //switch out the node
                //sequence node is special since it gains a new child instead of being a leaf that needs to be switched
                if(candidate is SequenceNode seq)
                {
                    nodes.Add(replacement);
                    nodes.AddRange(replacement.Children);
                    seq.Children.Add(replacement);
                    replacement.parent = seq;
                    return true;
                } else
                {
                    //regular leaf nodes here
                    //make sure it isn't the same type (seq can have the same type since it is nested not replaced)
                    if (replacements[typeIndex] == candidate.GetType())
                    {
                        replacements.RemoveAt(typeIndex);
                        continue;
                    }
                    nodes.Add(replacement);
                    nodes.AddRange(replacement.Children);
                    nodes.Remove(candidate);
                    //switch out the node in the parent
                    parent.Children[parent.Children.IndexOf(candidate)] = replacement;
                    replacement.parent = parent;
                    return true;
                }
            }
            //if the above failed that means the candidate can't be replaced and move on to a different one
            candidates.Remove(candidate);

        }
        return false;
    }

    ValueType NodeReturnValue(AST_node node)
    {
        ValueType val = 0;
        if (typeof(IBoolReturn).IsAssignableFrom(node.GetType()))
            val |= ValueType.BOOL;
        if (typeof(IByteReturn).IsAssignableFrom(node.GetType()))
            val |= ValueType.BYTE;
        if (typeof(IHByteReturn).IsAssignableFrom(node.GetType()))
            val |= ValueType.HBYTE;
        //if no interfaces match then it returns nothing
        if (val == 0)
            val = ValueType.NONE;

        return val;
    }

    //supports finding nodes with different return types by | the opertators (ie: BYTE | HBYYTE)
    List<Type> NodesWithReturnType(ValueType returnType) {
        //get all nodes with the specified return type(s)
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        List<Type> implementingTypes = new List<Type>();

        if ((returnType & ValueType.NONE) > 0)
            implementingTypes.AddRange(types.Where(t => t.IsSubclassOf(typeof(AST_node)) && !typeof(IReturnable).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass));
        if ((returnType & ValueType.BOOL) > 0)
            implementingTypes.AddRange(types.Where(t => t.IsSubclassOf(typeof(AST_node)) && typeof(IBoolReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass));
        if ((returnType & ValueType.BYTE) > 0)
            implementingTypes.AddRange(types.Where(t => t.IsSubclassOf(typeof(AST_node)) && typeof(IByteReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass));
        if ((returnType & ValueType.HBYTE) > 0)
            implementingTypes.AddRange(types.Where(t => t.IsSubclassOf(typeof(AST_node)) && typeof(IHByteReturn).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass));
        return implementingTypes;
    }
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
    //public Action_node getNextAction()
    //{
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
        //return null;
    //}
}