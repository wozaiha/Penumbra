using System.Runtime.InteropServices;

namespace Penumbra.Game
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public readonly struct ActorEquip
    {
        public readonly SetId   Set;
        public readonly byte    Variant;
        public readonly StainId Stain;

        public override string ToString()
            => $"{Set},{Variant},{Stain}";
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public readonly struct ActorWeapon
    {
        public readonly SetId      Set;
        public readonly WeaponType Type;
        public readonly ushort     Variant;
        public readonly StainId    Stain;

        public override string ToString()
            => $"{Set},{Type},{Variant},{Stain}";
    }
}