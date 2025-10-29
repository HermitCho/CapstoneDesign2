using UnityEngine;

public class DummyManager : MonoBehaviour
{
    public GameObject wall; // 허물어질 벽
    public int dummyCount = 5; // 총 더미 개수

    void OnEnable()
    {
        DummyHealth.onDeath += OnDummyDeath;
    }

    void OnDisable()
    {
        DummyHealth.onDeath -= OnDummyDeath;
    }

    void OnDummyDeath(DummyHealth dummy)
    {
        dummyCount--;

        if (dummyCount <= 0)
        {
            Destroy(wall); // 벽 제거
            Debug.Log("모든 더미가 죽어서 벽이 허물어졌습니다!");
        }
    }
}
