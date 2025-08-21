using System;
using UnityEngine;

[Serializable]
public struct StatRow
{
    public int hp;
    public int dmg;
    public float armor;
    public float atkGap;
    public float range;
    public float move;
}

public class UnitCore : MonoBehaviour
{
    [Header("Link")]
    [SerializeField] private Unit unit;

    [Header("Base")]
    public string typeName;
    public int level = 1;
    public Team team = Team.Player;

    [Header("Stat (manual)")]
    public int hpMax = 100;
    public int hp = 100;
    public int dmg = 10;
    public float atkGap = 0.6f;
    public float range = 3f;
    public float move = 6f;
    public float armor = 0f;

    [Header("Level Table")]
    public bool useTable = true;
    public StatRow[] table;

    public event Action<UnitCore> onDead;
    public event Action<UnitCore, int> onHit;
    [SerializeField] private Animator animator;
    [SerializeField] private string dieTrigger = "Die";
    public void Init(
        string t,
        int lv,
        Team tm,
        int h,
        int d)
    {
        typeName = t;
        level = lv;
        team = tm;

        if (useTable)
            Apply();
        else
            SetManual(h, d);
    }

    public void Apply()
    {
        int lv = unit ? unit.level : level;
        var s = Pick(lv);
        SetFrom(s);
    }

    public void Heal(int v)
    {
        if (v <= 0)
            return;

        hp = Mathf.Min(hp + v, hpMax);
    }

    public void Hit(DamageData d)
    {
        if (hp <= 0)
            return;

        if (d.val <= 0)
            return;

        if (d.from == team)
            return;

        int raw = d.val;
        int cut = Mathf.RoundToInt(Mathf.Max(0f, raw - armor));
        int real = Mathf.Max(1, cut);

        hp -= real;
        onHit?.Invoke(this, real);

        if (hp <= 0)
            Die();
    }

    public bool Alive()
    {
        return hp > 0;
    }

    public void SetHpMax(int v)
    {
        hpMax = Mathf.Max(1, v);
        hp = Mathf.Min(hp, hpMax);
    }

    public void SetDmg(int v)
    {
        dmg = Mathf.Max(1, v);
    }

    public void SetLevel(int lv)
    {
        level = Mathf.Max(1, lv);
        if (useTable)
            Apply();
    }

    private void Die()
    {
        Debug.Log($"[UnitCore.Die] {name} hp->0");
        hp = 0;
        onDead?.Invoke(this);
        
    }

    private void SetManual(int h, int d)
    {
        hpMax = Mathf.Max(1, h);
        hp = hpMax;
        dmg = Mathf.Max(1, d);
    }

    private StatRow Pick(int lv)
    {
        if (table == null || table.Length == 0)
            return MakeDefault();

        int idx = Mathf.Clamp(lv - 1, 0, table.Length - 1);
        return table[idx];
    }

    private void SetFrom(StatRow s)
    {
        hpMax = Mathf.Max(1, s.hp);
        hp = hpMax;
        dmg = Mathf.Max(1, s.dmg);
        armor = Mathf.Max(0f, s.armor);
        atkGap = Mathf.Max(0.05f, s.atkGap);
        range = Mathf.Max(0.1f, s.range);
        move = Mathf.Max(0.1f, s.move);
    }

    private StatRow MakeDefault()
    {
        var s = new StatRow();
        s.hp = hpMax;
        s.dmg = dmg;
        s.armor = armor;
        s.atkGap = atkGap;
        s.range = range;
        s.move = move;
        return s;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (useTable)
            Apply();
    }
#endif
}
