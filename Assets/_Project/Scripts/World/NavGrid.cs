using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 벽 기반 그리드 길찾기(8방향 A*). 벽은 정적 지형이므로 씬 시작 시 한 번 굽는다(NavMesh 미사용).
    /// 칸이 막혔는지는 WallQuery(Wall 레이어)로 판정하고, 경로는 벽에 걸리지 않는 구간을 직선으로 잇도록 다듬는다.
    /// </summary>
    public class NavGrid : MonoBehaviour
    {
        [SerializeField] Vector2 center = Vector2.zero;
        [SerializeField] Vector2 size = new Vector2(32f, 20f);
        [Tooltip("칸 크기(월드 유닛)")]
        [SerializeField] float cellSize = 0.5f;
        [Tooltip("이동체 반지름. 벽과 이 거리 안이면 칸을 막힌 것으로 본다")]
        [SerializeField] float agentRadius = 0.4f;
        [SerializeField] bool drawBlockedGizmo;

        public static NavGrid Instance { get; private set; }

        bool[] _blocked;
        int _w, _h;
        Vector2 _origin;

        // A* 작업 버퍼(재사용)
        float[] _g;
        int[] _parent;
        bool[] _closed;
        readonly List<(float f, int idx)> _heap = new List<(float, int)>();

        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>벽 콜라이더가 모두 생성된 뒤(Start 이후 첫 요청 시점)에 굽는다.</summary>
        void EnsureBaked()
        {
            if (_blocked != null) return;
            _w = Mathf.CeilToInt(size.x / cellSize);
            _h = Mathf.CeilToInt(size.y / cellSize);
            _origin = center - size * 0.5f;
            _blocked = new bool[_w * _h];
            _g = new float[_w * _h];
            _parent = new int[_w * _h];
            _closed = new bool[_w * _h];

            Physics2D.SyncTransforms();
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    _blocked[y * _w + x] = WallQuery.Overlaps(CellCenter(x, y), agentRadius);
        }

        Vector2 CellCenter(int x, int y) => _origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);

        bool TryCell(Vector2 p, out int x, out int y)
        {
            x = Mathf.FloorToInt((p.x - _origin.x) / cellSize);
            y = Mathf.FloorToInt((p.y - _origin.y) / cellSize);
            return x >= 0 && y >= 0 && x < _w && y < _h;
        }

        bool Free(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h && !_blocked[y * _w + x];

        /// <summary>p가 속한 칸이 막혀 있으면 가까운 빈 칸을 찾는다(벽에 바짝 붙은 이동체용).</summary>
        bool NearestFree(Vector2 p, out int fx, out int fy)
        {
            TryCell(p, out int cx, out int cy);
            cx = Mathf.Clamp(cx, 0, _w - 1);
            cy = Mathf.Clamp(cy, 0, _h - 1);
            fx = cx; fy = cy;
            if (Free(cx, cy)) return true;
            for (int r = 1; r <= 6; r++)
            {
                float best = float.MaxValue;
                bool found = false;
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r || !Free(cx + dx, cy + dy)) continue;
                        float d = ((Vector2)CellCenter(cx + dx, cy + dy) - p).sqrMagnitude;
                        if (d < best) { best = d; fx = cx + dx; fy = cy + dy; found = true; }
                    }
                }
                if (found) return true;
            }
            return false;
        }

        /// <summary>
        /// from에서 to까지의 경로(경유점, 시작점 제외)를 path에 채운다. 길이 없으면 false.
        /// 마지막 경유점은 to 자체이거나(도달 가능) 가장 가까운 빈 칸의 중심이다.
        /// </summary>
        public bool FindPath(Vector2 from, Vector2 to, List<Vector2> path)
        {
            path.Clear();
            EnsureBaked();
            if (!NearestFree(from, out int sx, out int sy) || !NearestFree(to, out int gx, out int gy)) return false;

            int start = sy * _w + sx, goal = gy * _w + gx;
            System.Array.Fill(_g, float.MaxValue);
            System.Array.Fill(_closed, false);
            _heap.Clear();
            _g[start] = 0f;
            _parent[start] = -1;
            Push(Heuristic(sx, sy, gx, gy), start);

            bool reached = false;
            while (_heap.Count > 0)
            {
                int cur = Pop();
                if (_closed[cur]) continue;
                _closed[cur] = true;
                if (cur == goal) { reached = true; break; }

                int cx = cur % _w, cy = cur / _w;
                foreach (var d in Dirs)
                {
                    int nx = cx + d.x, ny = cy + d.y;
                    if (!Free(nx, ny)) continue;
                    // 대각선은 옆 두 칸이 모두 비어야 통과(모서리 끼임 방지)
                    if (d.x != 0 && d.y != 0 && (!Free(cx + d.x, cy) || !Free(cx, cy + d.y))) continue;
                    int ni = ny * _w + nx;
                    if (_closed[ni]) continue;
                    float ng = _g[cur] + (d.x != 0 && d.y != 0 ? 1.4142f : 1f);
                    if (ng >= _g[ni]) continue;
                    _g[ni] = ng;
                    _parent[ni] = cur;
                    Push(ng + Heuristic(nx, ny, gx, gy), ni);
                }
            }
            if (!reached) return false;

            var cells = new List<Vector2>();
            for (int i = goal; i != start; i = _parent[i]) cells.Add(CellCenter(i % _w, i / _w));
            cells.Reverse();
            // 도착 지점이 빈 칸 안이면 마지막 점을 실제 목표로 바꾼다
            if (TryCell(to, out int tx, out int ty) && tx == gx && ty == gy && cells.Count > 0) cells[cells.Count - 1] = to;
            Smooth(from, cells, path);
            return true;
        }

        /// <summary>보이는 가장 먼 점으로 건너뛰어 불필요한 꺾임을 없앤다(벽에 걸리지 않는 구간만).</summary>
        void Smooth(Vector2 from, List<Vector2> cells, List<Vector2> result)
        {
            Vector2 anchor = from;
            int i = 0;
            while (i < cells.Count)
            {
                int far = i;
                for (int j = cells.Count - 1; j > i; j--)
                {
                    if (!WallQuery.IsBlockedCircle(anchor, cells[j], agentRadius)) { far = j; break; }
                }
                result.Add(cells[far]);
                anchor = cells[far];
                i = far + 1;
            }
        }

        static float Heuristic(int x, int y, int gx, int gy)
        {
            int dx = Mathf.Abs(x - gx), dy = Mathf.Abs(y - gy);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy); // 옥타일 거리
        }

        // 최소 힙(지연 삭제 방식)
        void Push(float f, int idx)
        {
            _heap.Add((f, idx));
            int i = _heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_heap[p].f <= _heap[i].f) break;
                (_heap[p], _heap[i]) = (_heap[i], _heap[p]);
                i = p;
            }
        }

        int Pop()
        {
            int top = _heap[0].idx;
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < _heap.Count && _heap[l].f < _heap[m].f) m = l;
                if (r < _heap.Count && _heap[r].f < _heap[m].f) m = r;
                if (m == i) break;
                (_heap[m], _heap[i]) = (_heap[i], _heap[m]);
                i = m;
            }
            return top;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);
            if (!drawBlockedGizmo || _blocked == null) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    if (_blocked[y * _w + x]) Gizmos.DrawCube(CellCenter(x, y), Vector3.one * cellSize * 0.9f);
        }
    }
}
