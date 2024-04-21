using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;

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

//specified a class contains a value like a number or boolean. This is useful for mutations where we might want to change the number but leave it a constant
public interface IValue : IReturnable { public void SetValue(byte value); }

// Base class for AST nodes
public abstract class AST_node
{
    public bool mutatable = true;//if you want a node to not be mutated set this to false in the obejct
    public AST_node parent;
    //prevent this class being made outside this file
    //the constructor will automatically create children to ensure valid state
    internal AST_node() { parent = null;  }

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
    public Internal_node() {}

    public List<AST_node> Children = new List<AST_node>();

    public void GenerateTerminalChildren(List<Type> nodeTypes)
    {
        List<ValueType> childrenReturnTypes = getChildrenTypes();
        Type[] types = nodeTypes.ToArray();
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

            Type nodeType = implementingTypes.ElementAt(UnityEngine.Random.Range(0, implementingTypes.Count()));
            AST_node node = (AST_node)Activator.CreateInstance(nodeType);
            Children.Add(node);
            node.parent = this;
        }
    }

    //this function is called during construction to initialize default children. It can return an empty list
    public abstract List<ValueType> getChildrenTypes();
}

public abstract class Leaf_node : AST_node
{
}

/*
 * Implementations of nodes
 */

/*
 * Constant nodes
 */
sealed class Constant_bool : Leaf_node, IBoolReturn, IValue
{
    private byte Value;
    public Constant_bool(byte value) { this.Value = value; }

    public Constant_bool()
    {
        SetValue((byte)UnityEngine.Random.Range(0, 2));
    }

    public void SetValue(byte value)
    {
        this.Value = value;
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

sealed class Constant_byte : Leaf_node, IByteReturn, IValue
{
    private byte Value;
    public Constant_byte(byte value) { this.Value = value; }

    public Constant_byte()
    {
        SetValue((byte)UnityEngine.Random.Range(0, 256));
    }

    public void SetValue(byte value)
    {
        this.Value = value;
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

sealed class Constant_hbyte : Leaf_node, IHByteReturn, IValue
{
    private byte Value;
    public Constant_hbyte(byte value) { this.Value = value; }

    public Constant_hbyte()
    {
        SetValue((byte)UnityEngine.Random.Range(0, 16));
    }

    public void SetValue(byte value)
    {
        this.Value = value;
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

public class AST
{
    public List<AST_node> nodes = new List<AST_node>();
    List<Type> nodeTypes = new List<Type>();

    public readonly SequenceNode root;

    //technically can be done with a tuple but I want the variable names so the logic is readable
    struct NodeMirror
    {
        public AST_node original;
        public AST_node copy;
        public NodeMirror(AST_node original, AST_node copy)
        {
            this.original = original;
            this.copy = copy;
        }
    }

    public List<string> MutationHistory;

    private void RegisterNodes()
    {
        //these nodes are always part of program structure
        RegisterUserNodeType(typeof(Constant_bool));
        RegisterUserNodeType(typeof(Constant_byte));
        RegisterUserNodeType(typeof(Constant_hbyte));
        RegisterUserNodeType(typeof(NOOP));
        RegisterUserNodeType(typeof(SequenceNode));
        RegisterUserNodeType(typeof(IfElseNode));
        RegisterUserNodeType(typeof(AND_node));
        RegisterUserNodeType(typeof(OR_node));
        RegisterUserNodeType(typeof(NOT_node));
        RegisterUserNodeType(typeof(GT_node));
    }

    public AST(AST other)
    {
        MutationHistory = new List<string>(other.MutationHistory);
        foreach (Type nodeT in other.nodeTypes)
            RegisterUserNodeType(nodeT);

        //we need to copy the tree structure while making new nodes and setting child/parent relations to be correct...
        Stack<NodeMirror> toProcess = new Stack<NodeMirror>();
        root = new SequenceNode();
        root.mutatable = other.root.mutatable;
        toProcess.Push(new NodeMirror(other.root, root));
        nodes.Add(root);
        
        //this loop iterates over every node and copys them
        while(toProcess.Count > 0)
        {
            //processing will contain created and initialized instances of the nodes in it
            //we need to create and initialize children of the node and add them to the stack 
            NodeMirror processing = toProcess.Pop();

            if(processing.original is Internal_node iNode)
            {
                //create copys of all the children
                foreach(AST_node origChild in iNode.Children)
                {
                    AST_node copyChild = (AST_node)Activator.CreateInstance(origChild.GetType());
                    copyChild.parent = processing.copy;
                    copyChild.mutatable = origChild.mutatable;
                    ((Internal_node)processing.copy).Children.Add(copyChild);
                    toProcess.Push(new NodeMirror(origChild, copyChild));
                    nodes.Add(copyChild);
                }
            }
            if(processing.original is IValue vNode)
            {
                ((IValue)processing.copy).SetValue(vNode.Evaluate(new List<byte>()));
            }
        }
    }

    public AST()
    {
        RegisterNodes();

        //create barebones structure
        root = new SequenceNode(); //a squence node is ALWAYS the root
        root.Children.Add(new NOOP()); //the noop prevents infinite loops
        root.Children.Add(new SequenceNode());
        root.Children[0].mutatable = false;
        root.mutatable = false;

        //TESTING
        //Internal_node pheromoneNode = new SensePheromoneHere();
        //root.Children.Add(pheromoneNode);
        //Constant_hbyte hnode = new Constant_hbyte();
        //pheromoneNode.Children.Add(hnode);
        //hnode.parent = pheromoneNode;
        
        foreach (AST_node child in root.Children)
            child.parent = root;

        nodes.Add(root);
        nodes.AddRange(root.Children);
        MutationHistory = new List<string>();
    }

    public void RegisterUserNodeType(Type type) 
    {
        if(type.IsSubclassOf(typeof(AST_node))) 
            nodeTypes.Add(type);
    }

    public bool RandomMutation()
    {
        List<int> choices = new List<int>(){ 1,2,3,4 }; //TODO, add in other mutations
        while(choices.Count > 0)
        {
            int choice = choices[UnityEngine.Random.Range(0, choices.Count)];
            bool result = false;
            switch (choice)
            {
                case 0: result = HoistMutate(); break;
                case 1: result = ShrinkMutate(); break;
                case 2: result = ExpandMutate(); break;
                case 3: result = PointMutate(); break;
            }
            if (result)
                return true;
            choices.Remove(choice);
        }
        return false;
    }

    //invalidates evaluators!
    public bool HoistMutate()
    {
        //Choose an internal node and replace it with one of the children
        List<Internal_node> candidates = new List<Internal_node>(nodes.OfType<Internal_node>().ToList());
        if (candidates.Count == 0)
            return false;
        while (candidates.Count > 0)
        {
            Internal_node candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (!candidate.mutatable)
            {
                candidates.Remove(candidate);
                continue;
            }

            Internal_node parent = (Internal_node)candidate.parent;
            int indexInParent = parent.Children.IndexOf(candidate);
            ValueType expectedReturnType = parent.getChildrenTypes()[indexInParent];



            //choose which of the children will be hoisted. Must be compatable return type with the candidates parent
            List<AST_node> children = new List<AST_node>(candidate.Children);
            for (int i = children.Count - 1; i >= 0; i--)
            {
                AST_node child = children[i];
                ValueType childReturnType = NodeReturnValue(child);
                if ((childReturnType & expectedReturnType) == 0)
                    children.RemoveAt(i);
            }
            if (children.Count == 0)
            {
                candidates.Remove(candidate);
                continue;
            }
            AST_node hoistChild = children[UnityEngine.Random.Range(0, children.Count)];

            //delete subtrees of other candidate children
            for (int i = candidate.Children.Count - 1; i >= 0; i--)
            {
                if (candidate.Children[i] != hoistChild)
                {
                    if (candidate.Children[i] is Internal_node ichild)
                        DeleteSubtree(ichild);
                    else if (candidate.Children[i] is Leaf_node lchild)
                        lchild.parent = null;
                }
                candidate.Children[i] = null;
                candidate.parent = null;
            }

            nodes.Remove(candidate);

            parent.Children[indexInParent] = hoistChild;
            hoistChild.parent = parent;

            //log the mutation to the AST history
            string oldNode = candidate.GetSubtreeString();
            string newNode = hoistChild.GetSubtreeString();
            MutationHistory.Add("HOIST: " + Environment.NewLine + oldNode + Environment.NewLine + "->" + Environment.NewLine + newNode);

            return true;
        }
        return false;
    }

    //invalidates evaluators!
    public bool ShrinkMutate()
    {
        //choose a random node and replace it with a leaf node
        List<Internal_node> candidates = new List<Internal_node>(nodes.OfType<Internal_node>().ToList());
        if (candidates.Count == 0)
            return false;

        while (candidates.Count > 0)
        {

            Internal_node candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            if (!candidate.mutatable)
            {
                candidates.Remove(candidate);
                continue;
            }

            Internal_node parent = (Internal_node)candidate.parent;
            int indexInParent = parent.Children.IndexOf(candidate);

            string oldNode = candidate.GetSubtreeString();
            DeleteSubtree(candidate);

            //get possible replacements for the node
            ValueType expectedReturnType = parent.getChildrenTypes()[indexInParent];
            List<Type> replacements = NodesWithReturnType(expectedReturnType);
            replacements.RemoveAll(item => item.IsSubclassOf(typeof(Internal_node)));
            if (replacements.Count == 0)
            {
                candidates.Remove(candidate);
                continue;
            }
                
            int replacementIndex = UnityEngine.Random.Range(0, replacements.Count);
            Leaf_node replacement = (Leaf_node)Activator.CreateInstance(replacements[replacementIndex]);
            nodes.Add(replacement);
            parent.Children[indexInParent] = replacement;
            replacement.parent = parent;

            //log the mutation to the AST history
            
            string newNode = replacement.GetSubtreeString();
            MutationHistory.Add("SHRINK: " + Environment.NewLine + oldNode + Environment.NewLine + "->" + Environment.NewLine + newNode);
            return true;
        }
        return false;
    }

    //invalidates evaluators!
    public bool PointMutate()
    {
        //try to find a point that can be mutated with a valid return value and children types
        List<AST_node> candidates = new List<AST_node>(nodes.OfType<AST_node>().ToList());
        //remove sequences and ifs since mutations are difficult to define for those
        candidates.RemoveAll(item => item is SequenceNode || item is IfElseNode);

        while (candidates.Count > 0)
        {
            //choose a random candidate node for replacement and store some useful info based on it
            AST_node candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (!candidate.mutatable)
            {
                candidates.Remove(candidate);
                continue;
            }
            Internal_node parent = (Internal_node)candidate.parent;
            int indexInParent = parent.Children.IndexOf(candidate);



            //Debug.Log("Attempting to mutate candidate " + Environment.NewLine + candidate.GetSubtreeString());

            //get all possible replacements that would be compatible with the parents return type expectation
            ValueType expectedReturnType = parent.getChildrenTypes()[indexInParent];
            List<Type> replacements = NodesWithReturnType(expectedReturnType);

            //filter replacements based on internal/leaf
            //We can't filter by child count since there is no way to know the count without an instance of it
            if (candidate is Leaf_node leafNode)
                replacements.RemoveAll(item => item.IsSubclassOf(typeof(Internal_node)));
            else if (candidate is Internal_node inNode)
                replacements.RemoveAll(item => item.IsSubclassOf(typeof(Leaf_node)));

            //Prevents pointless mutations like DIG->DIG but still allows value types to mutate (ex: 14->6)
            if (candidate is not IValue)
                replacements.Remove(candidate.GetType());

            //test the replacements
            while (replacements.Count > 0)
            {
                int replacementIndex = UnityEngine.Random.Range(0, replacements.Count);
                AST_node replacement = (AST_node)Activator.CreateInstance(replacements[replacementIndex]);


                if (replacement is Internal_node inReplace)
                {
                    Internal_node inCandidate = (Internal_node)candidate;
                    //now that it is an instance we can test child counts
                    if (inReplace.Children.Count != inCandidate.Children.Count)
                    {
                        replacements.RemoveAt(replacementIndex);
                        continue;
                    }

                    bool fail = false;
                    //test the child returns to make sure the replacement is compatable with the current children
                    for (int i = 0; i < inCandidate.Children.Count; i++)
                    {
                        ValueType creturn = NodeReturnValue(inCandidate.Children[i]);
                        //if one child has a return incompatable with the replacement children types remove that replacement option
                        if ((creturn & inReplace.getChildrenTypes()[i]) == 0)
                        {
                            replacements.RemoveAt(replacementIndex);
                            fail = true;
                            break;
                        }
                    }
                    if (fail) continue;

                    //perform the replacement
                    nodes.Add(inReplace);
                    nodes.Remove(candidate);
                    //switch out the node in the parent
                    parent.Children[indexInParent] = inReplace;
                    inReplace.parent = parent;
                    for (int i = 0; i < inCandidate.Children.Count; i++)
                    {
                        inCandidate.Children[i].parent = inReplace;
                        inReplace.Children[i].parent = null; //do this since the replacement will have automatic children assigned to it. Setting their parent to null ensures the GC will destroy them
                        inReplace.Children[i] = inCandidate.Children[i];
                    }
                    //Debug.Log("Replaced with " + Environment.NewLine + inReplace.GetSubtreeString());

                    //log the mutation to the AST history
                    string oldNode = candidate.GetSubtreeString();
                    string newNode = replacement.GetSubtreeString();
                    MutationHistory.Add("REPLACE: " + Environment.NewLine + oldNode + Environment.NewLine + "->" + Environment.NewLine + newNode);

                    return true;
                }
                else if (replacement is Leaf_node lReplace)
                {
                    Leaf_node lCandidate = (Leaf_node)candidate;

                    //perform the replacement
                    nodes.Add(lReplace);
                    nodes.Remove(lCandidate);
                    //switch out the node in the parent
                    parent.Children[indexInParent] = lReplace;
                    lReplace.parent = parent;
                    //Debug.Log("Replaced with " + Environment.NewLine + lReplace.GetSubtreeString());
                    return true;
                }
            }

            candidates.Remove(candidate);
        }

        return false;
    }

    //invalidates evaluators!
    public bool ExpandMutate()
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
            if (!candidate.mutatable)
            {
                candidates.Remove(candidate);
                continue;
            }
            Internal_node parent = (Internal_node)candidate.parent;
            
            //use the parent child parameters to determine what nodes are good
            int indexInParent = parent.Children.IndexOf(candidate);
            ValueType retType = parent.getChildrenTypes()[indexInParent];
            List<Type> replacementTypes = NodesWithReturnType(retType); //based on the parents expectation for the children find replacements that can fit the interface

            //filter replacements to only be internal nodes (unless the candidate is a sequence node)
            if (candidate is SequenceNode)
            {
                for (int i = replacementTypes.Count - 1; i >= 0; i--)
                    if (!replacementTypes[i].IsSubclassOf(typeof(Leaf_node)))
                        replacementTypes.RemoveAt(i);
            }
            else
            {
                for (int j = replacementTypes.Count - 1; j >= 0; j--)
                    if (!replacementTypes[j].IsSubclassOf(typeof(Internal_node)))
                        replacementTypes.RemoveAt(j);
            }

            if (replacementTypes.Count > 0)
            {
                int ReplacementIndex = UnityEngine.Random.Range(0, replacementTypes.Count);
                AST_node replacement = (AST_node)Activator.CreateInstance(replacementTypes[ReplacementIndex]);
                if(candidate is not SequenceNode)
                    ((Internal_node)replacement).GenerateTerminalChildren(nodeTypes);
                //switch out the node
                //sequence node is special since it gains a new child instead of being a leaf that needs to be switched
                nodes.Add(replacement);
                if (candidate is not SequenceNode)
                    nodes.AddRange(((Internal_node)replacement).Children);

                //log the mutation to the AST history
                string oldNode = candidate.GetSubtreeString();
                string newNode = replacement.GetSubtreeString();
                MutationHistory.Add("EXPAND: " + Environment.NewLine + oldNode + Environment.NewLine + "->" + Environment.NewLine + newNode);

                if (candidate is SequenceNode seq)
                {
                    seq.Children.Add(replacement);
                    replacement.parent = seq;
                    return true;
                }
                else //regular leaf nodes 
                {
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

    private void DeleteSubtree(Internal_node root)
    {
        Stack<AST_node> toDelete = new Stack<AST_node>();
        toDelete.Push(root);
        //deletion loop
        while (toDelete.Count > 0)
        {
            AST_node node = toDelete.Pop();
            if (node is Internal_node inNode)
                foreach (AST_node child in inNode.Children)
                    toDelete.Push(child);
            node.parent = null;
            nodes.Remove(node);
        }
    }

    private ValueType NodeReturnValue(AST_node node)
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
    private List<Type> NodesWithReturnType(ValueType returnType) {
        //get all nodes with the specified return type(s)
        Type[] types = nodeTypes.ToArray(); // Assembly.GetExecutingAssembly().GetTypes();
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
    private class Node_frame
    {
        public List<byte> childrenValues; //the value that each child has returned
        public AST_node node; //the node that this frame is assosiated with
        public int currentChild; //the index of the child that needs to be evaluated next
        public Node_frame()
        {
            childrenValues = new List<byte>();
            currentChild = 0;
            node = null;
        }
    }
    //store results of calls in a stack. This is used to evaluate nodes with multiple childnode values needed
    private Stack<Node_frame> FrameStack = new Stack<Node_frame>();
    private AST tree; //used for debugging mainly

    public ASTEvaluator(AST tree)
    {
        this.tree = tree;
        Node_frame frame = new Node_frame();
        frame.node = tree.root;
        frame.currentChild = 0;
        //if there are no actionable nodes then the evalutator would never return so we just prevent any evaluation from happening
        //the root must be a sequence node and the first child must be a NOOP instruction to ensure no infinite logic loops can occur
        if (tree.nodes.OfType<IAction>().Count() > 0 && tree.root is SequenceNode sNode && sNode.Children.Count > 0 && sNode.Children[0] is NOOP)
            FrameStack.Push(frame);
        
    }

    //iterate through the tree evaluating as much as possible until a action_node that can be evaulated (all child subtrees have been processed) is reached and the node type is returned
    //null is returned if the first node is reached without any action nodes to prevent infinite loop
    public Tuple<Type, byte[]> getNextAction()
    {
        if(FrameStack.Count == 0) return null; //check to make sure there is at least one stack frame

        while(true)
        {
            Node_frame currentFrame = FrameStack.Peek();

            //Check if the current node has all subtrees processed 
            if (currentFrame.node is Leaf_node || (currentFrame.node is Internal_node && (currentFrame.currentChild >= ((Internal_node)currentFrame.node).Children.Count)))
            {
                //if the subtrees have been processed we can now attempt to evaluate the node or have an action executed
                //since this node is all done remove it from the stack
                if (currentFrame.node.parent != null)
                    FrameStack.Pop();
                else //special case for the top most node since it MUST loop so we reset the child counter
                    currentFrame.currentChild = 0;

                //check if it is an action and return it for processing
                if (currentFrame.node is IAction)
                    return new Tuple<Type, byte[]>(currentFrame.node.GetType(), currentFrame.childrenValues.ToArray());
                

                //if it is a node to be evaluated then evaluate it
                if (currentFrame.node is IReturnable ret)
                {
                    byte val = ret.Evaluate(currentFrame.childrenValues);
                    //we popped the current frame, so the next frame will be its parent
                    Node_frame parent = FrameStack.Peek();
                    parent.childrenValues.Add(val);
                }
            }
            else //if the node can't be evaluated we need to work down the children subtrees
            {
                //IFELSE needs special processing since only two of the 3 children are evaluated
                if (currentFrame.node is IfElseNode ifElseNode)
                {
                    //test if we are currently evaluating the condition branch
                    if (currentFrame.currentChild == 0)
                    {
                        Node_frame childFrame = new Node_frame();
                        childFrame.node = ifElseNode.Children[0];
                        FrameStack.Push(childFrame);
                        currentFrame.currentChild++;
                    }
                    //if the currentChild is 1 that means the logic branch has been evaluated and we can use the result to add the appropiate frame to the stack
                    else if (currentFrame.currentChild == 1)
                    {
                        Node_frame childFrame = new Node_frame();
                        //branch (true is any value > 0) in the spirit of C++
                        if (currentFrame.childrenValues[0] > 0)
                        {
                            childFrame.node = ifElseNode.Children[1];
                        }
                        else
                        {
                            childFrame.node = ifElseNode.Children[2];
                        }
                        FrameStack.Push(childFrame);
                        //setting this to 4 will cause the 'all subtres processed' to catch it above
                        currentFrame.currentChild = 4;
                    }
                }
                else if (currentFrame.node is Internal_node iNode) //if it isn't an IfElse node it must be an internal node since leaf nodes would be caught earler in code
                {
                    Node_frame childFrame = new Node_frame();
                    childFrame.node = iNode.Children[currentFrame.currentChild];
                    FrameStack.Push(childFrame);
                    currentFrame.currentChild++;
                }
                else //just in case XD
                {
                    Debug.LogError("Holy shit something really unexpected happened in the AST evaluator, I should not be printed!");
                }
            }
        }
    }
}