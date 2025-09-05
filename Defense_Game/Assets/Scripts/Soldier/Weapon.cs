using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Data")] public WeaponData weaponData;

    [HideInInspector] public int leftAmmo;
    [HideInInspector] public int leftMagazine;

    private IWeaponState currentState;
    private Soldier user;
    private void Start()
    {
        ChangeState(SupplyState.Instance); // 시작 시 보급
    }

    public void Initialize(Soldier user)
    {
        this.user = user;
    }
    public void ChangeState(IWeaponState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void TryFire()
    {
        if (currentState == IdleState.Instance && leftAmmo > 0)
            Fire();
        else if (currentState == IdleState.Instance && leftAmmo <= 0 && leftMagazine > 0)
            ChangeState(ReloadState.Instance);
    }

    public void Fire()
    {
        user.Fire();
    }
    public void TryReload()
    {
        if (currentState == IdleState.Instance && leftMagazine > 0)
            ChangeState(ReloadState.Instance);
    }

    public void TrySupply()
    {
        if (currentState == IdleState.Instance)
            ChangeState(SupplyState.Instance);
    }
}