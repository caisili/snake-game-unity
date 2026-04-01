using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public static EnemyController I { get; private set; }

    [Header("Start")]
    public Vector2Int startPos = new Vector2Int(24, 10);

    [Header("AI")]
    public int recomputePathEverySteps = 1; // 每走几步重算一次（分数高也可加快重算）
    public bool avoidSnakeHeadBias = true;  // 可选：稍微躲开蛇头附近（更像抢食物）

    public Vector2Int EnemyPos { get; private set; }

    private float _timer;
    private int _stepCounter;

    private List<Vector2Int> _currentPath = new List<Vector2Int>(); // 包含从下一格到目标
    private int _pathIndex = 0;

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        EnemyPos = startPos;
        transform.position = GameManager.I.GridToWorld(EnemyPos);
    }

    private void Update()
    {
        if (!GameManager.I.HasStarted) return;
        if (GameManager.I.IsPaused || GameManager.I.IsGameOver) return;

        _timer += Time.deltaTime;
        if (_timer >= GameManager.I.CurrentStepTime)
        {
            _timer = 0f;
            Step();
        }
    }

    private void Step()
    {
        _stepCounter++;

        Vector2Int target = FoodSpawner.I != null ? FoodSpawner.I.FoodPos : EnemyPos;

        // 需要重算路径？
        if (_currentPath.Count == 0 || _pathIndex >= _currentPath.Count || (_stepCounter % Mathf.Max(1, recomputePathEverySteps) == 0))
        {
            RecomputePath(target);
        }

        Vector2Int next = EnemyPos;

        if (_currentPath.Count > 0 && _pathIndex < _currentPath.Count)
        {
            next = _currentPath[_pathIndex];
            _pathIndex++;
        }
        else
        {
            // 兜底：如果找不到路径，就随机走一步合法格
            next = RandomLegalNeighbor();
        }

        // 撞墙（不应该发生，保险）
        if (!GameManager.I.InBounds(next)) return;

        // 敌人走到蛇身上：不允许（A* 已避开），兜底
        if (SnakeController.I != null && SnakeController.I.Occupies(next)) return;

        EnemyPos = next;
        transform.position = GameManager.I.GridToWorld(EnemyPos);

        // 敌人吃到食物：分数不加，但刷新食物（你也可以让敌人吃到扣你分）
        if (FoodSpawner.I != null && EnemyPos == FoodSpawner.I.FoodPos)
        {
            FoodSpawner.I.SpawnFood();
        }

        // 敌人撞到蛇头：你失败（更刺激）
        if (SnakeController.I != null && EnemyPos == SnakeController.I.HeadPos)
        {
            GameManager.I.GameOver();
        }
    }

    private void RecomputePath(Vector2Int target)
    {
        _currentPath = AStar(EnemyPos, target);
        _pathIndex = 0;
    }

    private Vector2Int RandomLegalNeighbor()
    {
        var dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int i = 0; i < 10; i++)
        {
            var d = dirs[Random.Range(0, dirs.Length)];
            var p = EnemyPos + d;
            if (!GameManager.I.InBounds(p)) continue;
            if (SnakeController.I != null && SnakeController.I.Occupies(p)) continue;
            return p;
        }
        return EnemyPos;
    }

    // ---------------- A* ----------------

    private class Node
    {
        public Vector2Int p;
        public int g; // cost from start
        public int f; // g + h
        public Node parent;
        public Node(Vector2Int p, int g, int f, Node parent) { this.p = p; this.g = g; this.f = f; this.parent = parent; }
    }

    private List<Vector2Int> AStar(Vector2Int start, Vector2Int goal)
    {
        // 快速退出
        if (start == goal) return new List<Vector2Int>();

        var open = new List<Node>();
        var bestG = new Dictionary<Vector2Int, int>();
        var closed = new HashSet<Vector2Int>();

        int h0 = Heuristic(start, goal);
        open.Add(new Node(start, 0, h0, null));
        bestG[start] = 0;

        while (open.Count > 0)
        {
            // 找 f 最小的节点（网格不大，用线性扫描足够）
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].f < open[bestIndex].f) bestIndex = i;
            }

            Node cur = open[bestIndex];
            open.RemoveAt(bestIndex);

            if (cur.p == goal)
            {
                return ReconstructPath(cur, start);
            }

            closed.Add(cur.p);

            foreach (var nb in Neighbors(cur.p))
            {
                if (closed.Contains(nb)) continue;
                if (!IsWalkable(nb, goal)) continue;

                int tentativeG = cur.g + 1;
                if (bestG.TryGetValue(nb, out int knownG) && tentativeG >= knownG) continue;

                bestG[nb] = tentativeG;
                int f = tentativeG + Heuristic(nb, goal);

                // 轻微偏好：让敌人更像“抢食物”而不是贴脸（可选）
                if (avoidSnakeHeadBias && SnakeController.I != null)
                {
                    int distToHead = Mathf.Abs(nb.x - SnakeController.I.HeadPos.x) + Mathf.Abs(nb.y - SnakeController.I.HeadPos.y);
                    if (distToHead <= 2) f += 2; // 距离蛇头太近就加点代价
                }

                open.Add(new Node(nb, tentativeG, f, cur));
            }
        }

        // 找不到路
        return new List<Vector2Int>();
    }

    private IEnumerable<Vector2Int> Neighbors(Vector2Int p)
    {
        yield return p + Vector2Int.up;
        yield return p + Vector2Int.down;
        yield return p + Vector2Int.left;
        yield return p + Vector2Int.right;
    }

    private bool IsWalkable(Vector2Int p, Vector2Int goal)
    {
        if (!GameManager.I.InBounds(p)) return false;

        // 允许走到 goal（食物）上
        if (p == goal) return true;

        // 避开蛇身
        if (SnakeController.I != null && SnakeController.I.Occupies(p)) return false;

        // 避开自己当前位置以外的敌人（这里只有一个敌人所以不用）
        return true;
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        // 曼哈顿距离（网格四方向移动）
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> ReconstructPath(Node goalNode, Vector2Int start)
    {
        var rev = new List<Vector2Int>();
        Node cur = goalNode;

        // 注意：我们需要“下一步到目标”的路径，不包含 start
        while (cur != null && cur.p != start)
        {
            rev.Add(cur.p);
            cur = cur.parent;
        }

        rev.Reverse();
        return rev;
    }
}