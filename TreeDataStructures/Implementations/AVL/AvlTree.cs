using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var current = newNode.Parent;
        while (current != null)
        {
            Rebalance(current);
            current = current.Parent;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var current = parent ?? child;
        while (current != null)
        {
            Rebalance(current);
            current = current.Parent;
        }
    }

    protected private int GetHieght(AvlNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return 0;
        }

        return node.Height;
    }

    protected private void RecalculateHeight(AvlNode<TKey, TValue> node)
    {
        node.Height = Math.Max(GetHieght(node.Left), GetHieght(node.Right)) + 1;
    }
    protected private int BalanceFactor(AvlNode<TKey, TValue> node)
    {
        return GetHieght(node.Left) - GetHieght(node.Right);
    }

    protected override void RotateLeft(AvlNode<TKey, TValue> x)
    {
        base.RotateLeft(x);
        RecalculateHeight(x);
        RecalculateHeight(x.Parent!);
    }

    protected override void RotateRight(AvlNode<TKey, TValue> y)
    {
        base.RotateRight(y);
        RecalculateHeight(y);
        RecalculateHeight(y.Parent!);
    }

    protected void Rebalance(AvlNode<TKey, TValue> node)
    {
        if (BalanceFactor(node) == -2)
        {
            if (BalanceFactor(node.Right!) == 1)
            {
                RotateRight(node.Right!);
            }

            RotateLeft(node);
        } else if (BalanceFactor(node) == 2)
        {
            if (BalanceFactor(node.Left!) == -1)
            {
                RotateLeft(node.Left!);
            }

            RotateRight(node);
        }
    }
}