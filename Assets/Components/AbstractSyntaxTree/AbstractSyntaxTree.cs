using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

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
    //EVERY node will return a value of some kind
    public abstract byte GetValue();
    public List<Function_node> children = new List<Function_node>();
}

//terminal will have no children
public abstract class Terminal_node : Function_node { }

//If something is a action it affects the agent and will cause the iterator to yeild return
public abstract class Action_node : Function_node { }

/*
 * More specific nodes that don't fall into functions or terminal nodes neatly
 */
//Sequence node is the foundation of the code as it allows multiple statements of any type to be placed in succession
public class SequenceNode : ASTNode
{
    public List<ASTNode> children { get; set; } = new List<ASTNode>();
    //adds a new statement to the sequence
    public void AddStatement(ASTNode statement)
    {
        children.Add(statement);
    }

    public override string GetSubtreeString()
    {
        string subtreeString = "";
        foreach (ASTNode statement in children)
        {
            subtreeString += statement.GetSubtreeString() + Environment.NewLine;
        }
        return subtreeString;
    }
}

//if else nodes will evaluate a condition then branch accordingly. 
public class IfElseNode : ASTNode
{
    public List<ASTNode> children { get; set; } = new List<ASTNode>();
    public IfElseNode(Terminal_node condition, SequenceNode ifBody, SequenceNode elseBody)
    {
        children.Add(condition);
        children.Add(ifBody);
        children.Add(elseBody);
    }

    public override string GetSubtreeString()
    {
        string conditionBody = children[0].GetSubtreeString();

        string ifBody = "";
        foreach (string line in children[1].GetSubtreeString().Split(Environment.NewLine))
        {
            ifBody += "| " + line + Environment.NewLine;
        }

        string elseBody = "";
        foreach (string line in children[2].GetSubtreeString().Split(Environment.NewLine))
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
    Value_node(byte _value)
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
        foreach (string line in children[1].GetSubtreeString().Split(Environment.NewLine))
        {
            LStatement += "| " + line + Environment.NewLine;
        }

        string RStatement = "";
        foreach (string line in children[2].GetSubtreeString().Split(Environment.NewLine))
        {
            RStatement += "| " + line + Environment.NewLine;
        }

        return LStatement + " && " + RStatement;
    }

    public override byte GetValue()
    {
        byte mask = 0b00000001;
        byte val1 = (byte)(children[0].GetValue() & mask);
        byte val2 = (byte)(children[1].GetValue() & mask);
        return (byte)(val1 & val2);
    }
}


/*
public class ASTIterator
{
    public IEnumerable<ASTNode> DepthFirstTraversal(ASTNode node)
    {
        yield return node;

        if (node is SubA1 subA1Node)
        {
            foreach (var child in DepthFirstTraversal(subA1Node))
            {
                yield return child;
            }
        }
        else if (node is SubA2 subA2Node)
        {
            foreach (var child in DepthFirstTraversal(subA2Node))
            {
                yield return child;
            }
        }
        // Add more else-if blocks for other subclasses of A as needed
    }
}
*/

class AST
{
    ASTNode root;
    AST()
    {
        root = new SequenceNode(); //a squence node is ALWAYS the root
    }
   
}


/*
Yes, you can certainly store the state of the iterator between calls by making it a class variable. This way, the iterator's state will persist between multiple calls to the function, allowing you to resume the iteration from where it left off.

Here's an example demonstrating how you might implement this:

```csharp
using System;
using System.Collections.Generic;

public class A
{
    public string Value { get; set; }
}

public class SubA1 : A
{
    public List<A> Children { get; set; } = new List<A>();
}

public class SubA2 : A
{
    // Subclass-specific properties or methods can be added as needed
}

public class DepthFirstIterator
{
    private IEnumerator<A> enumerator;

    public IEnumerable<A> DepthFirstTraversal(A node)
    {
        yield return node;

        if (node is SubA1 subA1Node)
        {
            foreach (var child in subA1Node.Children)
            {
                foreach (var result in DepthFirstTraversal(child))
                {
                    yield return result;
                }
            }
        }
    }

    public void StartTraversal(A rootNode)
    {
        enumerator = DepthFirstTraversal(rootNode).GetEnumerator();
    }

    public A GetNextNode()
    {
        if (enumerator.MoveNext())
        {
            return enumerator.Current;
        }
        else
        {
            return null; // Indicates traversal is complete
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Constructing a simple tree for demonstration
        var root = new SubA1 { Value = "Root" };
        var child1 = new SubA1 { Value = "Child 1" };
        var child2 = new SubA2 { Value = "Child 2" };
        var subChild1 = new SubA2 { Value = "Sub Child 1" };
        var subChild2 = new SubA1 { Value = "Sub Child 2" };

        root.Children.Add(child1);
        root.Children.Add(child2);
        child1.Children.Add(subChild1);
        child1.Children.Add(subChild2);

        // Start the traversal
        var iterator = new DepthFirstIterator();
        iterator.StartTraversal(root);

        // Continue traversal until a certain condition is met
        A node;
        while ((node = iterator.GetNextNode()) != null)
        {
            Console.WriteLine(node.Value);
            if (node is SubA2)
            {
                // Stop traversal if SubA2 node is encountered
                break;
            }
        }

        // Resume traversal from where it left off
        while ((node = iterator.GetNextNode()) != null)
        {
            Console.WriteLine(node.Value);
        }
    }
}
```

In this example, the `DepthFirstIterator` class encapsulates the logic for depth-first traversal. The `StartTraversal` method initializes the traversal by setting up the enumerator, and the `GetNextNode` method retrieves the next node in the traversal sequence. The traversal can be paused and resumed between calls to the iterator.
*/


/*
 * Example storing a variable in the iterator
using System;
using System.Collections.Generic;

public class A
{
    public string Value { get; set; }
}

public class SubA1 : A
{
    public List<A> Children { get; set; } = new List<A>();
}

public class SubA2 : A
{
    // Subclass-specific properties or methods can be added as needed
}

public class DepthFirstIterator
{
    private List<A> results = new List<A>();

    public IEnumerable<A> DepthFirstTraversal(A node)
    {
        if (node is SubA1 subA1Node)
        {
            foreach (var child in subA1Node.Children)
            {
                foreach (var result in DepthFirstTraversal(child))
                {
                    yield return result;
                }
            }

            // Store the result of evaluating the current node
            results.Add(node);
        }
        else
        {
            // Process other types of nodes as needed
            // For simplicity, we directly yield the node
            yield return node;
        }
    }

    public List<A> GetResults()
    {
        return results;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Constructing a simple tree for demonstration
        var root = new SubA1 { Value = "Root" };
        var child1 = new SubA1 { Value = "Child 1" };
        var child2 = new SubA2 { Value = "Child 2" };
        var subChild1 = new SubA2 { Value = "Sub Child 1" };
        var subChild2 = new SubA1 { Value = "Sub Child 2" };

        root.Children.Add(child1);
        root.Children.Add(child2);
        child1.Children.Add(subChild1);
        child1.Children.Add(subChild2);

        // Iterating over the tree using Depth-First Traversal
        var iterator = new DepthFirstIterator();
        foreach (var node in iterator.DepthFirstTraversal(root))
        {
            Console.WriteLine(node.Value);
        }

        // Accessing the stored results after traversal
        Console.WriteLine("\nResults:");
        foreach (var result in iterator.GetResults())
        {
            Console.WriteLine(result.Value);
        }
    }
}

 * */