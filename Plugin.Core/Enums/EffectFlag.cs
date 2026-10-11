using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Enums
{
    [Flags]
    public enum EffectFlag
    {
        Ammo40 = 1,
        Ammo10 = 2,
        GetDroppedWeapon = 4,
        QuickChangeWeapon = 16, // 0x00000010
        QuickChangeReload = 64, // 0x00000040
        Invincible = Ammo40, // 0x00000001
        FullMetalJack = GetDroppedWeapon, // 0x00000004
        HollowPoint = QuickChangeWeapon, // 0x00000010
        HollowPointPlus = QuickChangeReload, // 0x00000040
        C4SpeedKit = 128, // 0x00000080
        ExtraGrenade = Invincible, // 0x00000001
        ExtraThrowGrenade = Ammo10, // 0x00000002
        JackHollowPoint = FullMetalJack, // 0x00000004
        HP5 = 8,
        HP10 = HollowPoint, // 0x00000010
        Defense5 = 32, // 0x00000020
        Defense10 = HollowPointPlus, // 0x00000040
        Defense20 = C4SpeedKit, // 0x00000080
        Defense90 = ExtraGrenade, // 0x00000001
        Respawn20 = ExtraThrowGrenade, // 0x00000002
        Respawn30 = HP5, // 0x00000008
        Respawn50 = Defense5, // 0x00000020
        Respawn100 = Defense20, // 0x00000080
    }
}
