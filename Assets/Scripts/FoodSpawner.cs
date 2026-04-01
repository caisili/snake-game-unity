using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public static FoodSpawner I { get; private set; }

    [Header("Prefab")]
    public GameObject foodPrefab;

    [Header("World Mapping (usually leave as 1,0,0)")]
    [SerializeField] private float cellSize = 1f;          // 1格 = 1单位（按你项目需要调）
    [SerializeField] private Vector2 worldOrigin = Vector2.zero; // 地图中心的世界坐标（一般 0,0）

    public Vector2Int FoodPos { get; private set; }

    private GameObject _foodInstance;

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        SpawnFood();
    }

    public void SpawnFood()
    {
        if (foodPrefab == null)
        {
            Debug.LogError("FoodSpawner: foodPrefab is null.");
            return;
        }

        if (GameManager.I == null)
        {
            Debug.LogError("FoodSpawner: GameManager.I is null.");
            return;
        }

        Vector2Int p = GetRandomEmptyCell();
        FoodPos = p;

        Vector3 worldPos = GridToWorldCentered(p);

        if (_foodInstance == null)
            _foodInstance = Instantiate(foodPrefab, worldPos, Quaternion.identity);
        else
            _foodInstance.transform.position = worldPos;
    }

    // ✅ 关键：把 0..width-1 / 0..height-1 的网格坐标“居中映射”到世界坐标
    private Vector3 GridToWorldCentered(Vector2Int p)
    {
        int w = Mathf.Max(1, GameManager.I.width);
        int h = Mathf.Max(1, GameManager.I.height);

        // 把网格中心对齐到 (0,0)
        // 例如 width=20 -> x=0 会变成 -9.5，x=19 会变成 +9.5
        float x = (p.x - (w - 1) * 0.5f) * cellSize + worldOrigin.x;
        float y = (p.y - (h - 1) * 0.5f) * cellSize + worldOrigin.y;

        return new Vector3(x, y, 0f);
    }

    private Vector2Int GetRandomEmptyCell()
    {
        for (int tries = 0; tries < 5000; tries++)
        {
            int x = Random.Range(0, Mathf.Max(1, GameManager.I.width));
            int y = Random.Range(0, Mathf.Max(1, GameManager.I.height));
            var p = new Vector2Int(x, y);

            if (SnakeController.I != null && SnakeController.I.Occupies(p)) continue;
            if (EnemyController.I != null && EnemyController.I.EnemyPos == p) continue;

            return p;
        }

        return Vector2Int.zero;
    }
}