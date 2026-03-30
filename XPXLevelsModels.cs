namespace XPXLevels;

public sealed class PlayerProgress
{
    public ulong SteamId { get; init; }
    public string PlayerName { get; set; } = string.Empty;
    public long TotalXp { get; set; }
    public int Credits { get; set; }
    public int CrateTokens { get; set; }
    public int XpBoostPercent { get; set; }
    public DateTimeOffset? XpBoostExpiresUtc { get; set; }
    public DateTimeOffset LastGambleAttemptUtc { get; set; } = DateTimeOffset.MinValue;
}

public sealed class PlayerStats
{
    public ulong SteamId { get; init; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Headshots { get; set; }
    public int KnifeKills { get; set; }
    public int RoundWins { get; set; }
    public int BombPlants { get; set; }
    public int BombDefuses { get; set; }
    public int Mvps { get; set; }
    public int FirstBloods { get; set; }
    public int ClutchWins { get; set; }
    public int MultiKills { get; set; }
    public int BestKillStreak { get; set; }
    public long PlaytimeSeconds { get; set; }
    public int CratesOpened { get; set; }
    public int MissionsCompleted { get; set; }
    public int AchievementsUnlocked { get; set; }
}

public sealed class WeaponStatProgress
{
    public ulong SteamId { get; init; }
    public string WeaponName { get; init; } = string.Empty;
    public int Kills { get; set; }
    public int Headshots { get; set; }
}

public sealed class MissionDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = "daily";
    public MissionObjective Objective { get; set; } = MissionObjective.Kills;
    public int Goal { get; set; } = 1;
    public int RewardXp { get; set; }
    public int RewardCredits { get; set; }
}

public sealed class PlayerMissionState
{
    public ulong SteamId { get; init; }
    public string MissionKey { get; init; } = string.Empty;
    public string PeriodKey { get; init; } = string.Empty;
    public int Progress { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
}

public sealed class AchievementDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public MissionObjective Objective { get; set; } = MissionObjective.Kills;
    public int Goal { get; set; } = 1;
    public int RewardCredits { get; set; }
}

public sealed class PlayerAchievementState
{
    public ulong SteamId { get; init; }
    public string AchievementKey { get; init; } = string.Empty;
    public DateTimeOffset UnlockedUtc { get; init; }
}

public sealed class ShopItemDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ShopRewardType RewardType { get; set; } = ShopRewardType.Xp;
    public int RewardAmount { get; set; }
    public int CostCredits { get; set; }
}

public sealed class CrateDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CostCredits { get; set; }
    public List<CrateRewardDefinition> Rewards { get; set; } = [];
}

public sealed class CrateRewardDefinition
{
    public string Label { get; set; } = string.Empty;
    public string Rarity { get; set; } = "Common";
    public ShopRewardType RewardType { get; set; } = ShopRewardType.Credits;
    public int RewardAmount { get; set; }
    public int DurationMinutes { get; set; }
    public int Weight { get; set; } = 1;
}

public sealed class PendingRoundModifier
{
    public SpecialRoundType Type { get; set; } = SpecialRoundType.None;
    public string SetBy { get; set; } = string.Empty;
}

public readonly record struct LevelState(int Level, long TotalXp, long XpIntoLevel, long XpNeededForNextLevel);

public sealed record TopPlayerEntry(int Rank, ulong SteamId, string PlayerName, long TotalXp);

public sealed record ServerMapOption(string Key, string DisplayName, string CommandTarget, bool IsWorkshop);

public sealed class TransitionSnapshot
{
    public DateTimeOffset CreatedUtc { get; set; }
    public List<TransitionSnapshotEntry> Players { get; set; } = [];
}

public sealed class TransitionSnapshotEntry
{
    public ulong SteamId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public long TotalXp { get; set; }
}

public sealed class MapVoteSession
{
    public MapVoteSession(IReadOnlyList<ServerMapOption> options, string startedBy, DateTimeOffset endsAtUtc)
    {
        Options = options;
        StartedBy = startedBy;
        EndsAtUtc = endsAtUtc;
    }

    public IReadOnlyList<ServerMapOption> Options { get; }
    public string StartedBy { get; }
    public DateTimeOffset EndsAtUtc { get; }
    public Dictionary<ulong, string> VotesBySteamId { get; } = new();
}

public sealed class LevelCurve
{
    private long[] _xpToNextLevel = Array.Empty<long>();
    private long[] _cumulativeXp = Array.Empty<long>();

    public int MaxLevel { get; private set; } = 1;
    public long MaxTotalXp { get; private set; }

    public void Rebuild(XPXLevelsConfig config)
    {
        MaxLevel = Math.Max(1, config.MaxLevel);
        _xpToNextLevel = new long[MaxLevel + 1];
        _cumulativeXp = new long[MaxLevel + 1];

        _cumulativeXp[1] = 0;
        for (var level = 1; level < MaxLevel; level++)
        {
            var curveIndex = level - 1;
            var xpForLevel = (long)Math.Round(
                config.BaseXpToLevel +
                (config.XpLinearGrowthPerLevel * curveIndex) +
                (config.XpQuadraticGrowthPerLevel * curveIndex * curveIndex));
            _xpToNextLevel[level] = Math.Max(1L, xpForLevel);
            _cumulativeXp[level + 1] = _cumulativeXp[level] + _xpToNextLevel[level];
        }

        MaxTotalXp = _cumulativeXp[MaxLevel];
    }

    public LevelState GetState(long totalXp)
    {
        if (_cumulativeXp.Length == 0)
        {
            return new LevelState(1, 0, 0, 0);
        }

        var clampedXp = Math.Clamp(totalXp, 0, MaxTotalXp);
        var level = 1;
        for (var nextLevel = 2; nextLevel <= MaxLevel; nextLevel++)
        {
            if (clampedXp < _cumulativeXp[nextLevel])
            {
                break;
            }

            level = nextLevel;
        }

        if (level >= MaxLevel)
        {
            return new LevelState(MaxLevel, clampedXp, 0, 0);
        }

        var xpIntoLevel = clampedXp - _cumulativeXp[level];
        return new LevelState(level, clampedXp, xpIntoLevel, _xpToNextLevel[level]);
    }
}

public enum MissionObjective
{
    Kills,
    Headshots,
    Wins,
    KnifeKills,
    Assists,
    Mvps,
    BombPlants,
    BombDefuses,
    BombObjectives,
    FirstBloods,
    ClutchWins,
    PlayMinutes,
    CratesOpened
}

public enum ShopRewardType
{
    Xp,
    Credits,
    CrateToken,
    XpBoost
}

public enum SpecialRoundType
{
    None,
    Knife,
    Pistol
}

public enum WarmupEventType
{
    Default,
    PistolsOnly,
    KnivesOnly,
    ScoutsOnly
}
