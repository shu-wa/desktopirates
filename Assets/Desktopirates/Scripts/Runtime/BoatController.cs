using UnityEngine;

namespace Desktopirates
{
    public sealed class BoatController : MonoBehaviour
    {
        public Vector2 LogicalPosition { get; private set; }
        public float HeadingDegrees { get; private set; }
        public float Speed { get; private set; }
        public int CruiseStep { get; private set; }
        public int MaxCruiseStep => CruiseModel.GetMaxStep(State != null ? State.EngineLevel : 0);
        public float TargetSpeed => CruiseModel.GetTargetSpeed(CruiseStep, State != null ? State.EngineLevel : 0);
        public float MaxSpeed => CruiseModel.GetMaxSpeed(State != null ? State.EngineLevel : 0);
        public bool IsCoasting => CruiseModel.IsCoasting(Speed, TargetSpeed);
        public GameState State { get; private set; }

        private Transform boatVisual;
        private float wakePulse;

        public void Initialize(Transform visual, GameState state)
        {
            boatVisual = visual;
            State = state;
            HeadingDegrees = state.HeadingDegrees;
            LogicalPosition = state.PlayerPosition;
            CruiseStep = 0;
            Speed = 0f;
        }

        private void Update()
        {
            if (boatVisual == null) return;

            if (Input.GetKeyDown(KeyCode.W)) IncreaseCruiseStep();
            if (Input.GetKeyDown(KeyCode.S)) DecreaseCruiseStep();
            Advance(Time.deltaTime, Input.GetAxisRaw("Horizontal"));
        }

        public void Advance(float deltaTime, float steering)
        {
            if (boatVisual == null || State == null) return;
            CruiseStep = Mathf.Clamp(CruiseStep, 0, MaxCruiseStep);
            Speed = CruiseModel.IntegrateForwardSpeed(Speed, TargetSpeed, deltaTime);

            // A rudder needs water flowing over it; a stopped ship cannot spin in place.
            float steerStrength = Speed <= CruiseModel.StopSnapSpeed
                ? 0f
                : Mathf.Lerp(18f, 62f, Mathf.Clamp01(Speed / Mathf.Max(0.01f, MaxSpeed)));
            HeadingDegrees = Mathf.Repeat(HeadingDegrees + steering * steerStrength * deltaTime, 360f);
            float radians = HeadingDegrees * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            LogicalPosition += forward * Speed * deltaTime;
            State.PlayerPosition = LogicalPosition;
            State.HeadingDegrees = HeadingDegrees;

            boatVisual.localRotation = Quaternion.Euler(0f, HeadingDegrees, 0f);
            wakePulse += deltaTime * (2f + Speed);
            boatVisual.localPosition = new Vector3(0f, 0.28f + Mathf.Sin(wakePulse) * 0.025f, 0f);
        }

        public void IncreaseCruiseStep() => CruiseStep = CruiseModel.ChangeStep(CruiseStep, 1, State != null ? State.EngineLevel : 0);

        public void DecreaseCruiseStep() => CruiseStep = CruiseModel.ChangeStep(CruiseStep, -1, State != null ? State.EngineLevel : 0);

        public void TowTo(Vector2 position)
        {
            LogicalPosition = position;
            State.PlayerPosition = position;
            CruiseStep = 0;
            Speed = 0f;
        }
    }
}
