using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();


    public virtual void Add(TKey key, TValue value)
    {
        TNode NewNode = CreateNode(key, value);
        if (Root == null)
        {
            Root = NewNode;
            Count++;
            OnNodeAdded(NewNode);
            return;
        }

        TNode? current = Root;
        TNode parent = null!;

        while (current != null)
        {
            parent = current;
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp < 0)
            {
                current = current.Left;
            }
            else if (cmp > 0)
            {
                current = current.Right;
            }
            else
            {
                //тут пока хз
                current.Value = value;
                return;
            }
        }

        NewNode.Parent = parent;

        if (Comparer.Compare(key, parent.Key) < 0)
        {
            parent.Left = NewNode;
        }
        else
        {
            parent.Right = NewNode;

        }

        Count++;
        OnNodeAdded(NewNode);
    }


    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }


    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
        }
        else
        {
            TNode maxLeft = node.Left;
            while (maxLeft.Right != null)
            {
                maxLeft = maxLeft.Right;
            }

            if (maxLeft.Parent != node)
            {
                Transplant(maxLeft, maxLeft.Left);
                maxLeft.Left = node.Left;
                maxLeft.Left.Parent = maxLeft;
            }

            Transplant(node, maxLeft);
            maxLeft.Right = node.Right;
            maxLeft.Right.Parent = maxLeft;
            OnNodeRemoved(maxLeft.Parent, maxLeft);

        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set
        {
            TNode? node = FindNode(key);

            if (node != null)
            {
                node.Value = value;
            }
            else
            {
                Add(key, value);
            }
        }
    }


    #region Hooks

    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }

    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }

    #endregion


    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);


    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        var y = x.Right;
        if (y == null)
        {
            return;
        }

        if (y.Left != null)
        {
            y.Left.Parent = x;
        }

        x.Right = y.Left;
        Transplant(x, y);
        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        var x = y.Left;
        if (x == null)
        {
            return;
        }

        if (x.Right != null)
        {
            x.Right.Parent = y;
        }

        y.Left = x.Right;
        Transplant(y, x);
        x.Right = y;
        y.Parent = x;
    }

    protected void RotateBigLeft(TNode x)
    {
        if (x.Right != null)
        {
            RotateRight(x.Right);
        }

        RotateLeft(x);
    }

    protected void RotateBigRight(TNode y)
    {
        if (y.Left != null)
        {
            RotateLeft(y.Left);
        }

        RotateRight(y);
    }

    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x);
        if (x.Parent != null)
        {
            RotateLeft(x.Parent);
        }
    }

    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y);
        if (y.Parent != null)
        {
            RotateRight(y.Parent);
        }
    }

    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);

    /*
    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node)
    {
        if (node == null) { yield break; }
        throw new NotImplementedException();
    }

    */

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        // probably add something here
        private readonly Stack<TNode> _stack;
        private TNode? _root;
        private TNode? _current;
        private TNode? _lastVisited;
        private TreeEntry<TKey, TValue> _currentEntry;
        private readonly TraversalStrategy _strategy; // or make it template parameter?

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _current = root;
            _stack = new Stack<TNode>();
            _currentEntry = default;
            _lastVisited = null;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current =>  _currentEntry;
        object IEnumerator.Current => this.Current;

        private int GetHieght(TNode? node)
        {
            if (node == null)
            {
                return -1;
            }

            int left = GetHieght(node.Left);
            int right = GetHieght(node.Right);
            return Math.Max(left, right) + 1;
        }
        public bool MoveNext()
        {
            if (_strategy == TraversalStrategy.InOrder)
            {
                while (_current != null)
                {
                    _stack.Push(_current);
                    _current = _current.Left;
                }

                if (_stack.Count == 0)
                {
                    return false;
                }

                var x = _stack.Pop();
                _currentEntry = new TreeEntry<TKey, TValue>(x.Key, x.Value, GetHieght(x));
                _current = x.Right;
                return true;
            }

            else if (_strategy == TraversalStrategy.InOrderReverse)
            {
                while (_current != null)
                {
                    _stack.Push(_current);
                    _current = _current.Right;
                }

                if (_stack.Count == 0)
                {
                    return false;
                }

                var x = _stack.Pop();
                _currentEntry = new TreeEntry<TKey, TValue>(x.Key, x.Value, GetHieght(x));
                _current = x.Left;
                return true;
            }

            else if (_strategy == TraversalStrategy.PreOrder)
            {
                if (_current == null)
                {
                    if (_stack.Count == 0)
                    {
                        return false;
                    }

                    _current = _stack.Pop();
                }

                _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetHieght(_current));
                if (_current.Right != null)
                {
                    _stack.Push(_current.Right);
                }

                _current = _current.Left;
                return true;
            }

            else if (_strategy == TraversalStrategy.PreOrderReverse)
            {
                while (_current != null || _stack.Count > 0)
                {
                    if (_current != null)
                    {
                        _stack.Push(_current);
                        _current = _current.Right;
                    }
                    else
                    {
                        TNode pn = _stack.Peek();
                        if (pn.Right != null && _lastVisited != pn.Right)
                        {
                            _current = pn.Right;
                        }
                        else
                        {
                            _currentEntry = new TreeEntry<TKey, TValue>(pn.Key, pn.Value, GetHieght(pn));
                            _current = _stack.Pop();
                            return true;
                        }
                    }
                }

                return false;
            }

            else if (_strategy == TraversalStrategy.PostOrder)
            {
                while (_current != null || _stack.Count > 0)
                {
                    if (_current != null)
                    {
                        _stack.Push(_current);
                        _current = _current.Left;
                    }
                    else
                    {
                        TNode pn = _stack.Peek();
                        if (pn.Right != null && _lastVisited != pn.Right)
                        {
                            _current = pn.Right;
                        }
                        else
                        {
                            _currentEntry = new TreeEntry<TKey, TValue>(pn.Key, pn.Value, GetHieght(pn));
                            _lastVisited = _stack.Pop();
                            return true;
                        }
                    }
                }

                return false;
            }

            else if (_strategy == TraversalStrategy.PostOrderReverse)
            {
                if (_current == null)
                {
                    if (_stack.Count == 0)
                    {
                        return false;
                    }

                    _current = _stack.Pop();
                }

                _currentEntry = new TreeEntry<TKey, TValue>(_current.Key, _current.Value, GetHieght(_current));
                if (_current.Left != null)
                {
                    _stack.Push(_current.Left);
                }

                _current = _current.Right;
                return true;
            }
            throw new NotImplementedException("Strategy not implemented");
        }

        public void Reset()
        {
            _stack.Clear();
            _current = _root;
            _lastVisited = null;
            _currentEntry = default;
        }


        public void Dispose()
        {
            // TODO release managed resources here
            if (_stack != null)
            {
                _stack.Clear();
            }

            _root = null;
            _current = null;
            _lastVisited = null;
        }
    }


    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach(var x in this)
        {
            array[arrayIndex++] = x;
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}