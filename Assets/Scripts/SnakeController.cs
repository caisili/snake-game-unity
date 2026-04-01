using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    public static SnakeController I { get; private set; }

    [Header("Prefabs")]
    public GameObject segmentPrefab;

    [Header("Start")]
    public Vector2Int startPos = new Vector2Int(5, 10);
    public int startLength = 4;

    [Header("Visual")]
    public Color headColor = new Color(1f, 0.9f, 0.2f, 1f); // 黄一点
    public Color bodyColor = new Color(0.2f, 1f, 0.2f, 1f); // 绿一点

    private readonly List<Vector2Int> _body = new List<Vector2Int>(); // [0]=head
    private readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();

    private Vector2Int _dir = Vector2Int.right;
    private Vector2Int _nextDir = Vector2Int.right;

    private float _timer;

    public IReadOnlyList<Vector2Int> Body => _body;
    public Vector2Int HeadPos => _body.Count > 0 ? _body[0] : startPos;

    public bool Occupies(Vector2Int p) => _occupied.Contains(p);

    private readonly List<Transform> _segmentViews = new List<Transform>();

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        InitSnake();
    }

    private void InitSnake()
    {
        _timer = 0f;
        _dir = Vector2Int.right;
        _nextDir = Vector2Int.right;

        _body.Clear();
        _occupied.Clear();

        // 清旧视图
        foreach (var t in _segmentViews)
            if (t != null) Destroy(t.gameObject);
        _segmentViews.Clear();

        // ✅ 关键：保证起点和整条蛇都在网格内（避免出生就出界）
        // 蛇是向左铺开，所以 startPos.x 至少要 >= startLength-1
        int safeX = Mathf.Clamp(startPos.x, startLength - 1, GameManager.I.width - 1);
        int safeY = Mathf.Clamp(startPos.y, 0, GameManager.I.height - 1);
        Vector2Int safeStart = new Vector2Int(safeX, safeY);

        // 生成蛇身：向左铺开
        for (int i = 0; i < startLength; i++)
        {
            var p = new Vector2Int(safeStart.x - i, safeStart.y);
            _body.Add(p);
            _occupied.Add(p);

            var seg = Instantiate(segmentPrefab, GameManager.I.GridToWorld(p), Quaternion.identity);

            // ✅ 可选：让段都挂到 Snake 物体下面，层级更干净
            seg.transform.SetParent(this.transform, true);

            seg.name = i == 0 ? "SnakeHead" : $"SnakeSeg_{i}";
            ApplySegmentVisual(seg, i == 0);
            _segmentViews.Add(seg.transform);
        }
    }

    private void ApplySegmentVisual(GameObject segObj, bool isHead)
    {
        var sr = segObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = isHead ? headColor : bodyColor;
            // 如果你有背景挡住，可以把蛇画在更上层：
            // sr.sortingOrder = 10;
        }
    }

    private void Update()
    {
        if (!GameManager.I.HasStarted) return;
        if (GameManager.I.IsPaused || GameManager.I.IsGameOver) return;

        // 输入：避免反向
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) TrySetNextDir(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) TrySetNextDir(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) TrySetNextDir(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TrySetNextDir(Vector2Int.right);

        _timer += Time.deltaTime;
        if (_timer >= GameManager.I.CurrentStepTime)
        {
            _timer = 0f;
            Step();
        }
    }

    private void TrySetNextDir(Vector2Int d)
    {
        // 禁止直接反向
        if (d + _dir == Vector2Int.zero) return;
        _nextDir = d;
    }

    private void Step()
    {
        _dir = _nextDir;
        Vector2Int newHead = HeadPos + _dir;

        // 撞墙（出界）
        if (!GameManager.I.InBounds(newHead))
        {
            GameManager.I.GameOver();
            return;
        }

        // 撞敌人
        if (EnemyController.I != null && EnemyController.I.EnemyPos == newHead)
        {
            GameManager.I.GameOver();
            return;
        }

        // 计算是否吃到食物
        bool eat = (FoodSpawner.I != null && newHead == FoodSpawner.I.FoodPos);

        // 如果不吃，尾巴会移动走，所以“撞自己”时可以允许踩到当前尾巴格（仅当不吃）
        Vector2Int tail = _body[_body.Count - 1];
        bool hitsSelf = _occupied.Contains(newHead) && !(!eat && newHead == tail);

        if (hitsSelf)
        {
            GameManager.I.GameOver();
            return;
        }

        // 头插入
        _body.Insert(0, newHead);
        _occupied.Add(newHead);

        if (eat)
        {
            GameManager.I.AddScore(1);
            AudioManager.I?.PlayEat();
            FoodSpawner.I.SpawnFood();
            AddSegmentViewAtEnd();
        }
        else
        {
            // 正常移动：删尾
            _body.RemoveAt(_body.Count - 1);
            _occupied.Remove(tail);
        }

        RefreshViews();
    }

    private void AddSegmentViewAtEnd()
    {
        var seg = Instantiate(segmentPrefab, GameManager.I.GridToWorld(_body[_body.Count - 1]), Quaternion.identity);
        seg.transform.SetParent(this.transform, true);
        seg.name = $"SnakeSeg_{_segmentViews.Count}";

        ApplySegmentVisual(seg, false);
        _segmentViews.Add(seg.transform);
    }

    private void RefreshViews()
    {
        // 确保视图数量与身体一致
        while (_segmentViews.Count < _body.Count) AddSegmentViewAtEnd();

        for (int i = 0; i < _body.Count; i++)
        {
            _segmentViews[i].position = GameManager.I.GridToWorld(_body[i]);
        }

        // ✅ 确保第0段永远是“头”的颜色（以防 prefab 颜色被改）
        if (_segmentViews.Count > 0)
        {
            var headSR = _segmentViews[0].GetComponent<SpriteRenderer>();
            if (headSR != null) headSR.color = headColor;
        }
    }
}