using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
        {
            return (null, null);
        }

        int cmp = Comparer.Compare(root.Key, key);
        if (cmp > 0)
        {
            (TreapNode<TKey, TValue>?SplitLeft, TreapNode<TKey, TValue>?SplitRight) = Split(root.Left, key);
            root.Left = SplitRight;
            if (root.Left != null)
            {
                root.Left.Parent = root;
            }

            if (SplitLeft != null)
            {
                SplitLeft.Parent = null;
            }

            return (SplitLeft, root);
        }
        else
        {
            (TreapNode<TKey, TValue>?SplitLeft, TreapNode<TKey, TValue>?SplitRight) = Split(root.Right, key);
            root.Right = SplitLeft;
            if (root.Right != null)
            {
                root.Right.Parent = root;
            }

            if (SplitRight != null)
            {
                SplitRight.Parent = null;
            }

            return (root, SplitRight);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null)
        {
            return right;
        }

        else if (right == null)
        {
            return left;
        }

        if (left.Priority < right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null)
            {
                left.Right.Parent = left;
            }

            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null)
            {
                right.Left.Parent = right;
            }

            return right;
        }
    }

    public override void Add(TKey key, TValue value)
    {
        var NewNode = CreateNode(key, value);
        (TreapNode<TKey, TValue>?SplitLeft, TreapNode<TKey, TValue>?SplitRight) = Split(this.Root, key);
        var result = Merge(SplitLeft, Merge(NewNode, SplitRight));
        this.Root = result;
        if (this.Root != null)
        {
            this.Root.Parent = null;
        }

        Count++;
    }

    public override bool Remove(TKey key)
    {
        TreapNode<TKey, TValue>? RemoveNode = FindNode(key);
        if (RemoveNode == null)
        {
            return false;
        }

        TreapNode<TKey, TValue>? Children = Merge(RemoveNode.Left, RemoveNode.Right);
        Transplant(RemoveNode, Children);
        Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }
    
}