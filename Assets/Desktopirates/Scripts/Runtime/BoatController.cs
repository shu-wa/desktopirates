using UnityEngine;

namespace Desktopirates
{
    public sealed class BoatController : MonoBehaviour
    {
        public Vector2 LogicalPosition { get; private set; }
        public float HeadingDegrees { get; private set; }
        public float Speed { get; private set; }
        public GameState State { get; private set; }

        private Transform boatVisual;
        private float wakePulse;

        public void Initialize(Transform visual, GameState state)
        {
            boatVisual = visual;
            State = state;
            HeadingDegrees = state.HeadingDegrees;
            LogicalPosition = state.PlayerPosition;
        }

        private void Update()
        {
            if (boatVisual == null) return;

            float throttle = Input.GetAxisRaw("Vertical");
            float steering = Input.GetAxisRaw("Horizontal");
            float maxSpeed = 3.4f + State.EngineLevel * 0.32f;
            Speed = Mathf.MoveTowards(Speed, throttle * maxSpeed, Time.deltaTime * 2.25f);

            float steerStrength = 34f + Mathf.Abs(Speed) * 13f;
            HeadingDegrees = Mathf.Repeat(HeadingDegrees + steering * steerStrength * Time.deltaTime, 360f);
            float radians = HeadingDegrees * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            LogicalPosition += forward * Speed * Time.deltaTime;
            State.PlayerPosition = LogicalPosition;
            State.HeadingDegrees = HeadingDegrees;

            boatVisual.localRotation = Quaternion.Euler(0f, HeadingDegrees, 0f);
            wakePulse += Time.deltaTime * (2f + Mathf.Abs(Speed));
            boatVisual.localPosition = new Vector3(0f, 0.28f + Mathf.Sin(wakePulse) * 0.025f, 0f);
        }

        public void TowTo(Vector2 position)
        {
            LogicalPosition = position;
            State.PlayerPosition = position;
            Speed = 0f;
        }
    }
}
