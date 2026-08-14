using UnityEngine;

namespace Desktopirates
{
    public static class BossModel
    {
        public static int GetHull(BossKind boss) => boss switch
        {
            BossKind.GangAdmiral => 1400,
            BossKind.GhostShip => 2800,
            BossKind.Kraken => 5200,
            BossKind.Poseidon => 10000,
            _ => 180
        };

        public static int GetContactDamage(BossKind boss) => boss switch
        {
            BossKind.Kraken => 120,
            BossKind.Poseidon => 220,
            BossKind.GhostShip => 85,
            BossKind.GangAdmiral => 60,
            _ => 30
        };

        public static ShipStatus GetInflictedStatus(BossKind boss) => boss switch
        {
            BossKind.GangAdmiral => ShipStatus.Burning,
            BossKind.GhostShip => ShipStatus.Poisoned,
            BossKind.Kraken => ShipStatus.Sticky,
            BossKind.Poseidon => ShipStatus.Frozen,
            _ => ShipStatus.None
        };

        public static CrewPerk RollPerk(BossKind boss, ulong encounterId)
        {
            ulong roll = WorldGenerator.Hash(unchecked((int)encounterId), (int)(encounterId >> 32), (int)boss, 991);
            if (roll % 100UL >= 42UL) return CrewPerk.None;
            CrewPerk[] pool = boss switch
            {
                BossKind.GangAdmiral => new[] { CrewPerk.PowderExpert, CrewPerk.FastHands, CrewPerk.RapidRepair },
                BossKind.GhostShip => new[] { CrewPerk.Firebrand, CrewPerk.VenomShot, CrewPerk.ConditionSpecialist },
                BossKind.Kraken => new[] { CrewPerk.AnchorMaster, CrewPerk.TarShot, CrewPerk.ReinforcedPatch },
                BossKind.Poseidon => new[] { CrewPerk.WindWhisperer, CrewPerk.Helmsman, CrewPerk.FrostShot },
                _ => new[] { CrewPerk.None }
            };
            return pool[(int)((roll >> 8) % (ulong)pool.Length)];
        }
    }

    public sealed class BossMotionController : MonoBehaviour
    {
        private BossKind kind;
        private Vector3 origin;
        private Transform[] animatedParts;

        public void Initialize(BossKind boss)
        {
            kind = boss;
            origin = transform.localPosition;
            animatedParts = new Transform[transform.childCount];
            for (int i = 0; i < animatedParts.Length; i++) animatedParts[i] = transform.GetChild(i);
        }

        private void Update()
        {
            float t = Time.time;
            transform.localPosition = origin + Vector3.up * Mathf.Sin(t * (kind == BossKind.GhostShip ? 1.8f : 1.1f)) * 0.06f;
            if (kind == BossKind.Kraken)
            {
                for (int i = 0; i < animatedParts.Length; i++)
                    if (animatedParts[i] != null && animatedParts[i].name.Contains("Tentacle"))
                        animatedParts[i].localRotation = Quaternion.Euler(18f + Mathf.Sin(t * 1.7f + i) * 13f, i * 45f, Mathf.Cos(t * 1.3f + i) * 10f);
            }
            else if (kind == BossKind.Poseidon)
            {
                for (int i = 0; i < animatedParts.Length; i++)
                    if (animatedParts[i] != null && animatedParts[i].name.Contains("Trident"))
                        animatedParts[i].localRotation = Quaternion.Euler(0f, 0f, -12f + Mathf.Sin(t * 1.4f) * 9f);
            }
            else transform.localRotation *= Quaternion.Euler(0f, Mathf.Sin(t * 0.8f) * 0.06f, 0f);
        }
    }
}
