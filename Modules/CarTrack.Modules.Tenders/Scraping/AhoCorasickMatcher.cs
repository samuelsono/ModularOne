namespace CarTrack.Modules.Tenders;

/// <summary>
/// Compact Aho–Corasick multi-pattern matcher for large keyword sets (Any-mode).
/// </summary>
internal sealed class AhoCorasickMatcher
{
    private readonly Node _root = new();

    public AhoCorasickMatcher(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            Insert(pattern.ToLowerInvariant());
        }

        BuildFailureLinks();
    }

    public IReadOnlyList<string> FindAll(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var found = new HashSet<string>(StringComparer.Ordinal);
        var node = _root;
        foreach (var ch in text.ToLowerInvariant())
        {
            while (node != _root && !node.Children.ContainsKey(ch))
            {
                node = node.Fail!;
            }

            if (node.Children.TryGetValue(ch, out var next))
            {
                node = next;
            }

            var output = node;
            while (output is not null)
            {
                foreach (var pattern in output.Outputs)
                {
                    found.Add(pattern);
                }

                output = output.OutputLink;
            }
        }

        return found.ToList();
    }

    private void Insert(string pattern)
    {
        var node = _root;
        foreach (var ch in pattern)
        {
            if (!node.Children.TryGetValue(ch, out var child))
            {
                child = new Node();
                node.Children[ch] = child;
            }

            node = child;
        }

        node.Outputs.Add(pattern);
    }

    private void BuildFailureLinks()
    {
        var queue = new Queue<Node>();
        foreach (var child in _root.Children.Values)
        {
            child.Fail = _root;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var (ch, child) in current.Children)
            {
                queue.Enqueue(child);
                var fail = current.Fail;
                while (fail is not null && !fail.Children.ContainsKey(ch))
                {
                    fail = fail.Fail;
                }

                child.Fail = fail?.Children.GetValueOrDefault(ch) ?? _root;
                child.OutputLink = child.Fail.Outputs.Count > 0 ? child.Fail : child.Fail.OutputLink;
            }
        }
    }

    private sealed class Node
    {
        public Dictionary<char, Node> Children { get; } = new();

        public Node? Fail { get; set; }

        public Node? OutputLink { get; set; }

        public List<string> Outputs { get; } = [];
    }
}
