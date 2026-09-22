using System.Collections.Generic;
using Game.Enemies;
using Game.Player;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 6스테이지 무한 스폰(기획: 무한 스폰되는 가시 창병 떼로 플레이어를 오른쪽으로 몰아넣는 용도, 싸워 이기는 대상이 아님).
    /// 생존 개체 수가 상한보다 적을 때만 왼쪽 스폰 지점에서 보충하고, 스폰 즉시 플레이어를 향해 돌진하게 만든다.
    /// </summary>
    public class FunnelSpawner : MonoBehaviour
    {
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] Transform[] spawnPoints;
        [Tooltip("동시 생존 상한")]
        [SerializeField] int maxAlive = 6;
        [SerializeField] float spawnIntervalSeconds = 1.2f;
        [Tooltip("스폰 직후 플레이어를 향해 경계(사실상 계속 추격하도록 충분히 큰 값)")]
        [SerializeField] float alertSeconds = 999f;
        [Tooltip("씬 진입 후 첫 스폰까지의 대기 시간(초). 플레이어가 상황을 파악하고 움직일 시간을 준다")]
        [SerializeField] float initialDelaySeconds = 2.5f;

        readonly List<EnemyBase> _alive = new List<EnemyBase>();
        float _nextSpawnTime;
        PlayerHealth _player;

        void Start() => _nextSpawnTime = Time.time + initialDelaySeconds;

        void Update()
        {
            _alive.RemoveAll(e => e == null || e.IsDead);

            if (_player == null) _player = FindAnyObjectByType<PlayerHealth>();
            if (_player == null || _player.IsDead) return;
            if (Time.time < _nextSpawnTime) return;
            if (spawnPoints == null || spawnPoints.Length == 0 || enemyPrefab == null) return;
            if (_alive.Count >= maxAlive) return;

            _nextSpawnTime = Time.time + spawnIntervalSeconds;
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var go = Instantiate(enemyPrefab, point.position, point.rotation);
            if (go.TryGetComponent<EnemyBase>(out var enemy)) _alive.Add(enemy);
            if (go.TryGetComponent<EnemyAI>(out var ai)) ai.AlertTo(_player.transform.position, alertSeconds);
        }
    }
}
