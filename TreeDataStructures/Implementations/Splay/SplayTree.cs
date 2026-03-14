using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        Splay(child ?? parent);
    }

    // доделать 
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        // идем до ключа и кидаем вверх последний посещенный элемент (даже если не нашли)
        BstNode<TKey, TValue>? current = this.Root;
        BstNode<TKey, TValue>? last = null;
        while (current != null)
        {
            last = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0)
            {
                value = current.Value;
                Splay(current);
                return true;
            }

            current = (cmp < 0) ? current.Left : current.Right;
        }

        if (last != null)
        {
            Splay(last);
        }

        value = default;
        return false;
    }

    private void Zig(BstNode<TKey, TValue> x, BstNode<TKey, TValue> p)
    {
        if (p.Left == x)
        {
            RotateRight(p);
        }
        else
        {
            RotateLeft(p);
        }
    }

    private void Splay(BstNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return;
        }

        while (node.Parent != null)
        {
            var p = node.Parent;
            var g = p.Parent;

            if (g == null)
            {
                Zig(node, p);
            }

            // ZigZag
            else if (p == g.Left && node == p.Right)
            {
                RotateBigRight(g);
            }

            else if (p == g.Right && node == p.Left)
            {
                RotateBigLeft(g);
            }

            // ZigZig
            else if (p == g.Left && node == p.Left)
            {
                RotateDoubleRight(g);
            }

            else if (p == g.Right && node == p.Right)
            {
                RotateDoubleLeft(g);
            }
        }
    }

    public override bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }
}
