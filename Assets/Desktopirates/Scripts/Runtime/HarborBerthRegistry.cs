using System.Collections.Generic;

namespace Desktopirates
{
    /// <summary>
    /// Deterministic berth ownership kept separate from the pilot. A future network session can
    /// feed every vessel through this registry without changing docking geometry or UI code.
    /// </summary>
    public sealed class HarborBerthRegistry
    {
        private readonly Dictionary<ulong, ulong[]> occupiedByPort = new Dictionary<ulong, ulong[]>();

        public bool TryReserve(ulong portId, ulong vesselId, out int berthIndex)
            => TryReserve(portId, vesselId, (int)(vesselId % (ulong)DockingModel.BerthCount), out berthIndex);

        public bool TryReserve(ulong portId, ulong vesselId, int preferredBerthIndex, out int berthIndex)
        {
            berthIndex = -1;
            if (portId == 0UL || vesselId == 0UL) return false;
            if (!occupiedByPort.TryGetValue(portId, out ulong[] berths))
            {
                berths = new ulong[DockingModel.BerthCount];
                occupiedByPort.Add(portId, berths);
            }

            for (int i = 0; i < berths.Length; i++)
            {
                if (berths[i] != vesselId) continue;
                berthIndex = i;
                return true;
            }

            int preferred = DockingModel.IsValidBerth(preferredBerthIndex)
                ? preferredBerthIndex
                : (int)(vesselId % (ulong)DockingModel.BerthCount);
            for (int offset = 0; offset < berths.Length; offset++)
            {
                int candidate = (preferred + offset) % berths.Length;
                if (berths[candidate] != 0UL) continue;
                berths[candidate] = vesselId;
                berthIndex = candidate;
                return true;
            }
            return false;
        }

        public bool IsOccupied(ulong portId, int berthIndex)
            => DockingModel.IsValidBerth(berthIndex)
               && occupiedByPort.TryGetValue(portId, out ulong[] berths)
               && berths[berthIndex] != 0UL;

        public void Release(ulong portId, ulong vesselId)
        {
            if (!occupiedByPort.TryGetValue(portId, out ulong[] berths)) return;
            bool anyOccupied = false;
            for (int i = 0; i < berths.Length; i++)
            {
                if (berths[i] == vesselId) berths[i] = 0UL;
                anyOccupied |= berths[i] != 0UL;
            }
            if (!anyOccupied) occupiedByPort.Remove(portId);
        }
    }
}
