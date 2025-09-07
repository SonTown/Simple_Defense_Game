using UnityEngine;
using Cysharp.Threading.Tasks;
[CreateAssetMenu(fileName = "WeaponData", menuName = "RTS/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Stats")]
    public int Damage = 10;
    public int MaxAmmo = 30;
    public int MaxMagazine = 5;
    [Header("Type")] 
    public bool isExplosive = false;
    [Header("Cooldowns")]
    public float AttackRange = 30f;
    public float AttackDelay = 0.1f;
    public float ReloadDelay = 1.2f;
    public float SupplyDelay = 2f;
}
public interface IWeaponState
{
    void Enter(Weapon weapon);
    void Exit(Weapon weapon);
}

public class IdleState : IWeaponState
{
    public static readonly IdleState Instance = new IdleState();
    private IdleState() {}

    public void Enter(Weapon weapon)
    {
        Debug.Log($"[{weapon.name}] Idle 상태 진입");
    }

    public void Exit(Weapon weapon) {}
}

public class FireState : IWeaponState
{
    public static readonly FireState Instance = new FireState();
    private FireState() {}

    public async void Enter(Weapon weapon)
    {
        weapon.leftAmmo--;
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(weapon.weaponData.AttackDelay));

        weapon.ChangeState(IdleState.Instance);
    }

    public void Exit(Weapon weapon) {}
}

public class ReloadState : IWeaponState
{
    public static readonly ReloadState Instance = new ReloadState();
    private ReloadState() {}

    public async void Enter(Weapon weapon)
    {
        Debug.Log($"[{weapon.name}] 장전 시작");
        weapon.leftMagazine--;

        await UniTask.Delay(System.TimeSpan.FromSeconds(weapon.weaponData.ReloadDelay));

        weapon.leftAmmo = weapon.weaponData.MaxAmmo;
        Debug.Log($"[{weapon.name}] 장전 완료");

        weapon.ChangeState(IdleState.Instance);
    }

    public void Exit(Weapon weapon) {}
}

public class SupplyState : IWeaponState
{
    public static readonly SupplyState Instance = new SupplyState();
    private SupplyState() {}

    public async void Enter(Weapon weapon)
    {
        Debug.Log($"[{weapon.name}] 보급 시작");

        await UniTask.Delay(System.TimeSpan.FromSeconds(2f)); // SupplyDelay를 WeaponData에 넣어도 됨

        weapon.leftMagazine = weapon.weaponData.MaxMagazine;
        weapon.leftAmmo = weapon.weaponData.MaxAmmo;

        Debug.Log($"[{weapon.name}] 보급 완료");

        weapon.ChangeState(IdleState.Instance);
    }

    public void Exit(Weapon weapon) {}
}
