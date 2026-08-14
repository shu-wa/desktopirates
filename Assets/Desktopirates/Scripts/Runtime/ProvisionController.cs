using UnityEngine;

namespace Desktopirates
{
    /// <summary>Background voyage clock for crew food and fresh-water use.</summary>
    public sealed class ProvisionController : MonoBehaviour
    {
        private GameState state;
        private SaveSystem saves;

        public void Initialize(GameState gameState, SaveSystem saveSystem)
        {
            state = gameState;
            saves = saveSystem;
        }

        private void Update()
        {
            if (state == null || state.Crew <= 0) return;
            state.ProvisionClock += Time.unscaledDeltaTime;
            if (state.ProvisionClock < ProvisionModel.ConsumptionInterval) return;
            int cycles = Mathf.FloorToInt(state.ProvisionClock / ProvisionModel.ConsumptionInterval);
            state.ProvisionClock -= cycles * ProvisionModel.ConsumptionInterval;
            int used = ProvisionModel.GetUnitsPerInterval(state.Crew) * cycles;
            state.Food = Mathf.Max(0, state.Food - used);
            state.Water = Mathf.Max(0, state.Water - used);
            if (state.Food == 0 || state.Water == 0)
                state.Hull = Mathf.Max(1, state.Hull - cycles); // fatigue/neglect pressure, never an off-screen game-over.
            saves?.Save(state);
        }
    }
}
