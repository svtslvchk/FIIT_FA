using TreeDataStructures.Core;
using TreeDataStructures.Implementations.AVL;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{

    private RbNode<TKey, TValue>? GetGrandparent(RbNode<TKey, TValue>? node)
    {
        if (node == null || node.Parent == null)
        {
            return null;
        }

        return node.Parent.Parent;
    }

    private RbNode<TKey, TValue>? GetUncle(RbNode<TKey, TValue> node)
    {
        var g = GetGrandparent(node);
        if (g == null)
        {
            return null;
        }

        var u = (node.Parent == g.Right) ? g.Left : g.Right;
        return u;
    }

    private RbNode<TKey, TValue>? GetBro(RbNode<TKey, TValue> node)
    {
        if (node.Parent == null)
        {
            return null;
        }
        return (node == node.Parent.Left) ? node.Parent.Right : node.Parent.Left;
    }

    private bool IsRed(RbNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return false;
        }

        return node.Color == RbColor.Red;
    }

    private bool IsBlack(RbNode<TKey, TValue>? node)
    {
        return !IsRed(node);
    }
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        var p = newNode.Parent;
        var g = GetGrandparent(newNode);
        var u = GetUncle(newNode);
        while (newNode != Root && IsRed(p))
        {
            if (u == g!.Right && g != null)
            {
                // 1 случай: дядя - красный
                if (IsRed(u))
                {
                    u!.Color = RbColor.Black;
                    p!.Color = RbColor.Black;
                    g.Color = RbColor.Red;
                    newNode = g;
                }

                else
                {
                    // 2.1 newnode и p по разные стороны от своих родителей
                    if (newNode == p!.Right)
                    {
                        newNode = p;
                        RotateLeft(newNode); // свели к 2.2
                    }

                    // 2.2 по одну сторону от родителей
                    g!.Color = RbColor.Red;
                    p!.Color = RbColor.Black;
                    RotateRight(g);
                }
            }

            else if (g != null && u == g.Left)
            {
                if (IsRed(u))
                {
                    u!.Color = RbColor.Black;
                    p!.Color = RbColor.Black;
                    g!.Color = RbColor.Red;
                    newNode = g;
                }
                else
                {
                    if (newNode == p!.Left)
                    {
                        newNode = p;
                        RotateRight(newNode);
                    }

                    g!.Color = RbColor.Red;
                    p!.Color = RbColor.Black;
                    RotateLeft(g);
                }
            }
        }

        if (Root != null)
        {
            Root.Color = RbColor.Black;
        }
    }
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        throw new NotImplementedException();
    }
}