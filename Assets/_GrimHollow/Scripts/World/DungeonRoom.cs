using UnityEngine;

// 던전의 방 하나: 이름, 종류, 영역. 미니맵과 몬스터 배치가 이 정보를 씀
public class DungeonRoom : MonoBehaviour
{
    public enum Kind { Start, Normal, Treasure, Boss }

    public string displayName;
    public Kind kind;
    public Vector2 size;            // 가로(x), 세로(z) 미터
    public bool visited;            // 한 번이라도 들어가 봤는지 (미니맵에 드러남)
    [System.NonSerialized] public bool revealedOnMap;   // 미니맵이 이 방을 이미 드러냈는지

    public Bounds Bounds => new Bounds(transform.position, new Vector3(size.x, 4f, size.y));

    public bool Contains(Vector3 p)
    {
        Bounds b = Bounds;
        return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = kind == Kind.Boss ? Color.red : kind == Kind.Treasure ? Color.yellow : Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.2f, size.y));
    }
}
