using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public Transform cameraTrans;
    public Transform weaponTrans;
    public WeaponEnum.PlayerWeapon weapon;

    void Awake()
    {
        GetWeapon();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GetWeapon()
    {
        WeaponInfo[] weapons = weaponTrans.GetComponentsInChildren<WeaponInfo>();
        if (weapons.Length > 1)
        {
            Debug.LogError($"Hold {weapons.Length} weapons!!");
            return;
        }

        if (weapons.Length == 1)
        {
            weapon = weapons[0].weapon;
            return;
        }

        Debug.Log("No weapon detected");
    }
}
