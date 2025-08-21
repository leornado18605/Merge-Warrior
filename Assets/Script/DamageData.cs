using UnityEngine;

[System.Serializable]
public struct DamageData
{
    public int val;
    public Team from;
    public Vector3 pos;

    public DamageData(
        int v,
        Team f,
        Vector3 p)
    {
        val = v;
        from = f;
        pos = p;
    }
}
