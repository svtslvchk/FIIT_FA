using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Implementations.AVL;
using TreeDataStructures.Implementations.RedBlackTree;
using TreeDataStructures.Implementations.Splay;
using TreeDataStructures.Implementations.Treap;
using TreeDataStructures.Interfaces;

Console.WriteLine("=== ТЕСТ ВСЕХ ДЕРЕВЬЕВ ===");

var trees = new Dictionary<string, ITree<int, string>>
{
    ["BST"] = new BinarySearchTree<int, string>(),
    ["AVL"] = new AvlTree<int, string>(),
    ["RedBlack"] = new RedBlackTree<int, string>(),
    ["Splay"] = new SplayTree<int, string>(),
    ["Treap"] = new Treap<int, string>()
};

var random = new Random(123);

foreach (var (name, tree) in trees)
{
    Console.WriteLine($"\n=== ТЕСТИРУЕМ {name} ===");
    
    var inserted = new HashSet<int>();
    
    // Вставляем 500 элементов
    for (int i = 0; i < 500; i++)
    {
        int val = random.Next(-1000, 1000);
        if (inserted.Add(val))
        {
            tree.Add(val, "v");
        }
    }
    Console.WriteLine($"Вставлено: {tree.Count}");
    
    // Проверяем InOrder
    try
    {
        var keys = tree.InOrder().Select(x => x.Key).ToList();
        Console.WriteLine($"InOrder OK: {keys.Count} элементов");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"InOrder ОШИБКА: {ex.Message}");
    }
    
    // Удаляем половину
    var toRemove = inserted.Take(250).ToList();
    int removed = 0;
    foreach (int key in toRemove)
    {
        if (tree.Remove(key))
        {
            removed++;
            inserted.Remove(key);
        }
    }
    Console.WriteLine($"Удалено: {removed}, ожидалось: {250}");
    
    // Проверяем финальный InOrder
    try
    {
        var finalKeys = tree.InOrder().Select(x => x.Key).ToList();
        Console.WriteLine($"Финальный InOrder OK: {finalKeys.Count} элементов");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Финальный InOrder ОШИБКА: {ex.Message}");
    }
}