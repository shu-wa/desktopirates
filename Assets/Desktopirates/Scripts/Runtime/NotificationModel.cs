using System;
using UnityEngine;

namespace Desktopirates
{
    public readonly struct EnemyDamageNotice
    {
        public readonly Vector3 WorldPosition;
        public readonly int Damage;
        public readonly bool Sinking;
        public readonly string Statuses;

        public EnemyDamageNotice(Vector3 worldPosition, int damage, bool sinking, string statuses)
        {
            WorldPosition = worldPosition;
            Damage = Mathf.Max(0, damage);
            Sinking = sinking;
            Statuses = statuses ?? string.Empty;
        }
    }

    public readonly struct PlayerDamageNotice
    {
        public readonly int Damage;
        public readonly ShipStatus Status;

        public PlayerDamageNotice(int damage, ShipStatus status)
        {
            Damage = Mathf.Max(0, damage);
            Status = status;
        }
    }

    public readonly struct LootNotice
    {
        public readonly PoiKind Source;
        public readonly int Gold;
        public readonly int Supplies;
        public readonly SalvageDrop[] Drops;

        public LootNotice(PoiKind source, int gold, int supplies, SalvageDrop[] drops)
        {
            Source = source;
            Gold = Mathf.Max(0, gold);
            Supplies = Mathf.Max(0, supplies);
            Drops = drops ?? Array.Empty<SalvageDrop>();
        }
    }

    public static class NotificationUiModel
    {
        public const float FeedLifetime = 5f;
        public const float FeedFadeStart = 3.15f;
        public const float FeedStride = 48f;
        public const float FeedBaseY = 218f;
        public const float DamageLifetime = 1.35f;

        public static float GetFeedAlpha(float age)
        {
            if (age <= FeedFadeStart) return 1f;
            return 1f - Mathf.InverseLerp(FeedFadeStart, FeedLifetime, age);
        }

        public static float GetDamageAlpha(float age)
            => 1f - Mathf.InverseLerp(DamageLifetime * 0.48f, DamageLifetime, age);

        public static float GetStackY(int indexFromNewest)
            => Mathf.Max(0, indexFromNewest) * FeedStride;
    }
}
