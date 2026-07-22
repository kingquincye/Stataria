using System;

namespace Stataria.Core
{
    public readonly struct AdaptationKey : IEquatable<AdaptationKey>
    {
        public AdaptationCategory Category { get; }
        public string TargetId { get; }
        public string DisplayName { get; }
        public bool IsOffensive { get; }

        public AdaptationKey(AdaptationCategory category, string targetId, string displayName, bool isOffensive = false)
        {
            Category = category;
            TargetId = targetId ?? string.Empty;
            DisplayName = displayName ?? targetId ?? "Unknown";
            IsOffensive = isOffensive;
        }

        public override bool Equals(object obj)
        {
            return obj is AdaptationKey key && Equals(key);
        }

        public bool Equals(AdaptationKey other)
        {
            return Category == other.Category &&
                   TargetId == other.TargetId &&
                   IsOffensive == other.IsOffensive;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Category, TargetId, IsOffensive);
        }

        public static bool operator ==(AdaptationKey left, AdaptationKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AdaptationKey left, AdaptationKey right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            string prefix = IsOffensive ? "[OFFENSE] " : "[DEFENSE] ";
            return $"{prefix}{Category}: {DisplayName} ({TargetId})";
        }
    }
}
