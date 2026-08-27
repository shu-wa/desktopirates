using System;
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
        public float TargetSpeed => CruiseModel.GetTargetSpeed(CruiseStep, State != null ? State.EngineLevel : 0) * (State != null ? ShipCustomizationModel.GetSpeedMultiplier(State) : 1f);
        public float MaxSpeed => CruiseModel.GetMaxSpeed(State != null ? State.EngineLevel : 0) * (State != null ? ShipCustomizationModel.GetSpeedMultiplier(State) : 1f);
        public bool IsCoasting => CruiseModel.IsCoasting(Speed, TargetSpeed);
        public bool IsDocking { get; private set; }
        public bool IsVoyageAutopilot { get; private set; }
        public bool IsAutoNavigating => IsDocking || IsVoyageAutopilot;
        public bool IsMoored { get; private set; }
        public int DockedBerthIndex { get; private set; } = -1;
        public Transform Visual => boatVisual;
        public GameState State { get; private set; }
        public event Action MooringReleased;

        private Transform boatVisual;
        private float wakePulse;
        private Vector2 dockApproach;
        private Vector2 dockBerth;
        private int dockingLeg;
        private Action dockingCompleted;
        private Vector2 voyageTarget;
        private int voyageCruiseStep;
        private bool voyageAllowsEngineStop;

        public void Initialize(Transform visual, GameState state)
        {
            boatVisual = visual;
            State = state;
            HeadingDegrees = state.HeadingDegrees;
            LogicalPosition = state.PlayerPosition;
            CruiseStep = 0;
            Speed = 0f;
            RefreshCustomizationVisual();
        }

        private void Update()
        {
            if (boatVisual == null) return;

            if (IsDocking)
            {
                AdvanceDocking(Time.deltaTime);
                return;
            }
            if (IsMoored)
            {
                Speed = 0f;
                CruiseStep = 0;
                ApplyVisual(Time.deltaTime);
                return;
            }

            if (IsVoyageAutopilot)
            {
                AdvanceVoyageAutopilot(Time.deltaTime);
                return;
            }

            if (Input.GetKeyDown(KeyCode.W)) IncreaseCruiseStep();
            if (Input.GetKeyDown(KeyCode.S)) DecreaseCruiseStep();
            Advance(Time.deltaTime, Input.GetAxisRaw("Horizontal"));
        }

        public void Advance(float deltaTime, float steering)
        {
            if (boatVisual == null || State == null) return;
            Vector2 previousPosition = LogicalPosition;
            CruiseStep = Mathf.Clamp(CruiseStep, 0, MaxCruiseStep);
            float brakingTime = TargetSpeed <= 0f ? deltaTime * CrewManagementModel.GetAnchorBrakingMultiplier(State) : deltaTime;
            Speed = CruiseModel.IntegrateForwardSpeed(Speed, TargetSpeed, brakingTime);

            // A rudder needs water flowing over it; a stopped ship cannot spin in place.
            float steerStrength = Speed <= CruiseModel.StopSnapSpeed
                ? 0f
                : Mathf.Lerp(18f, 62f, Mathf.Clamp01(Speed / Mathf.Max(0.01f, MaxSpeed))) * ShipCustomizationModel.GetTurningMultiplier(State);
            HeadingDegrees = Mathf.Repeat(HeadingDegrees + steering * steerStrength * deltaTime, 360f);
            float radians = HeadingDegrees * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            LogicalPosition += forward * Speed * deltaTime;
            State.Captain.AddDistance(Vector2.Distance(previousPosition, LogicalPosition));
            State.PlayerPosition = LogicalPosition;
            State.HeadingDegrees = HeadingDegrees;

            ApplyVisual(deltaTime);
        }

        private void ApplyVisual(float deltaTime)
        {
            boatVisual.localRotation = Quaternion.Euler(0f, HeadingDegrees, 0f);
            wakePulse += deltaTime * (2f + Mathf.Max(0f, Speed));
            boatVisual.localPosition = new Vector3(0f, 0.36f + Mathf.Sin(wakePulse) * 0.022f, 0f);
        }

        public void BeginDocking(Vector2 portPosition, Action completed)
            => BeginDocking(portPosition, DockingModel.DefaultBerthIndex, completed);

        public void BeginDocking(Vector2 portPosition, int berthIndex, Action completed)
        {
            if (IsDocking) return;
            DisengageVoyageAutopilot(false);
            dockApproach = DockingModel.GetApproach(portPosition, berthIndex);
            dockBerth = DockingModel.GetBerth(portPosition, berthIndex);
            dockingLeg = Vector2.Distance(LogicalPosition, dockApproach) <= 0.25f ? 1 : 0;
            dockingCompleted = completed;
            DockedBerthIndex = Mathf.Clamp(berthIndex, 0, DockingModel.BerthCount - 1);
            IsMoored = false;
            IsDocking = true;
            CruiseStep = 0;
        }

        private void AdvanceDocking(float deltaTime)
        {
            Vector2 previousPosition = LogicalPosition;
            Vector2 target = dockingLeg == 0 ? dockApproach : dockBerth;
            float distance = Vector2.Distance(LogicalPosition, target);
            bool finalLeg = dockingLeg == 1;
            float desiredSpeed = DockingModel.GetPilotSpeed(distance, finalLeg);
            Speed = Mathf.MoveTowards(Speed, desiredSpeed, deltaTime * 2.2f);
            float desiredHeading = distance > DockingModel.ArrivalDistance
                ? DockingModel.GetHeading(LogicalPosition, target)
                : DockingModel.FinalHeading;
            HeadingDegrees = Mathf.MoveTowardsAngle(HeadingDegrees, desiredHeading, deltaTime * (finalLeg ? 95f : 125f));
            LogicalPosition = Vector2.MoveTowards(LogicalPosition, target, Speed * deltaTime);
            State.Captain.AddDistance(Vector2.Distance(previousPosition, LogicalPosition));

            if (distance <= DockingModel.ArrivalDistance)
            {
                LogicalPosition = target;
                if (!finalLeg)
                {
                    dockingLeg = 1;
                }
                else if (Mathf.Abs(Mathf.DeltaAngle(HeadingDegrees, DockingModel.FinalHeading)) <= 1.5f)
                {
                    HeadingDegrees = DockingModel.FinalHeading;
                    Speed = 0f;
                    IsDocking = false;
                    IsMoored = true;
                    Action completed = dockingCompleted;
                    dockingCompleted = null;
                    completed?.Invoke();
                }
            }

            State.PlayerPosition = LogicalPosition;
            State.HeadingDegrees = HeadingDegrees;
            ApplyVisual(deltaTime);
        }

        public void HoldAtMooring()
        {
            IsDocking = false;
            IsVoyageAutopilot = false;
            IsMoored = true;
            CruiseStep = 0;
            Speed = 0f;
        }

        public void ReleaseMooring()
        {
            bool heldBerth = IsMoored || IsDocking || DockedBerthIndex >= 0;
            IsMoored = false;
            IsDocking = false;
            DockedBerthIndex = -1;
            if (heldBerth) MooringReleased?.Invoke();
        }

        public void SetVoyageCourse(Vector2 target, int cruiseStep, bool allowEngineStop = false)
        {
            if (IsDocking || IsMoored) return;
            voyageTarget = target;
            voyageAllowsEngineStop = allowEngineStop;
            voyageCruiseStep = Mathf.Clamp(cruiseStep, allowEngineStop ? 0 : 1, MaxCruiseStep);
            IsVoyageAutopilot = true;
        }

        public void DisengageVoyageAutopilot(bool stopEngines = true)
        {
            IsVoyageAutopilot = false;
            voyageAllowsEngineStop = false;
            if (stopEngines) CruiseStep = 0;
        }

        private void AdvanceVoyageAutopilot(float deltaTime)
        {
            Vector2 direction = voyageTarget - LogicalPosition;
            if (direction.sqrMagnitude <= AutoVoyageModel.WaypointArrivalDistance * AutoVoyageModel.WaypointArrivalDistance)
            {
                CruiseStep = 0;
                Advance(deltaTime, 0f);
                return;
            }

            float desiredHeading = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            float error = Mathf.DeltaAngle(HeadingDegrees, desiredHeading);
            float steering = AutoVoyageModel.GetSteeringInput(HeadingDegrees, desiredHeading);
            CruiseStep = voyageAllowsEngineStop
                ? Mathf.Clamp(voyageCruiseStep, 0, MaxCruiseStep)
                : Mathf.Abs(error) > AutoVoyageModel.SlowTurnAngle
                    ? Mathf.Min(1, voyageCruiseStep)
                    : Mathf.Clamp(voyageCruiseStep, 1, MaxCruiseStep);
            Advance(deltaTime, steering);
        }

        public void IncreaseCruiseStep() => CruiseStep = CruiseModel.ChangeStep(CruiseStep, 1, State != null ? State.EngineLevel : 0);

        public void DecreaseCruiseStep() => CruiseStep = CruiseModel.ChangeStep(CruiseStep, -1, State != null ? State.EngineLevel : 0);

        public void RefreshCustomizationVisual()
        {
            if (boatVisual != null && State != null) ProceduralSceneFactory.ApplyPlayerCustomization(boatVisual, State);
        }

        public void ResolveCollision(Vector2 displacement)
        {
            if (State == null || IsDocking || IsMoored || displacement.sqrMagnitude <= 0f) return;
            LogicalPosition += displacement;
            State.PlayerPosition = LogicalPosition;
        }

        public void TowTo(Vector2 position)
        {
            bool heldBerth = IsMoored || IsDocking || DockedBerthIndex >= 0;
            IsDocking = false;
            IsVoyageAutopilot = false;
            IsMoored = false;
            dockingCompleted = null;
            LogicalPosition = position;
            State.PlayerPosition = position;
            CruiseStep = 0;
            Speed = 0f;
            DockedBerthIndex = -1;
            if (heldBerth) MooringReleased?.Invoke();
        }
    }
}
