using System.Globalization;
using Dapper;
using CounterStrikeSharp.API.Core;
using Microsoft.Data.Sqlite;

namespace XPXLevels;

public sealed class XPXLevelsRepository
{
    private readonly string _databasePath;

    public XPXLevelsRepository(string moduleDirectory)
    {
        var dataDirectory = Path.Combine(Application.RootDirectory, "data", "XPXLevels");
        Directory.CreateDirectory(dataDirectory);
        _databasePath = Path.Combine(dataDirectory, "xpx-levels.db");
        MigrateLegacyDatabase(moduleDirectory);
    }

    public void Initialize()
    {
        using var connection = OpenConnection();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS players (
                steamid INTEGER PRIMARY KEY,
                player_name TEXT NOT NULL,
                total_xp INTEGER NOT NULL DEFAULT 0,
                credits INTEGER NOT NULL DEFAULT 0,
                crate_tokens INTEGER NOT NULL DEFAULT 0,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
            """);

        EnsureColumn(connection, "players", "credits", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "players", "crate_tokens", "INTEGER NOT NULL DEFAULT 0");

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS player_stats (
                steamid INTEGER PRIMARY KEY,
                kills INTEGER NOT NULL DEFAULT 0,
                deaths INTEGER NOT NULL DEFAULT 0,
                assists INTEGER NOT NULL DEFAULT 0,
                headshots INTEGER NOT NULL DEFAULT 0,
                knife_kills INTEGER NOT NULL DEFAULT 0,
                round_wins INTEGER NOT NULL DEFAULT 0,
                bomb_plants INTEGER NOT NULL DEFAULT 0,
                bomb_defuses INTEGER NOT NULL DEFAULT 0,
                mvps INTEGER NOT NULL DEFAULT 0,
                first_bloods INTEGER NOT NULL DEFAULT 0,
                clutch_wins INTEGER NOT NULL DEFAULT 0,
                multi_kills INTEGER NOT NULL DEFAULT 0,
                best_kill_streak INTEGER NOT NULL DEFAULT 0,
                playtime_seconds INTEGER NOT NULL DEFAULT 0,
                crates_opened INTEGER NOT NULL DEFAULT 0,
                missions_completed INTEGER NOT NULL DEFAULT 0,
                achievements_unlocked INTEGER NOT NULL DEFAULT 0
            );
            """);

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS weapon_stats (
                steamid INTEGER NOT NULL,
                weapon_name TEXT NOT NULL,
                kills INTEGER NOT NULL DEFAULT 0,
                headshots INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (steamid, weapon_name)
            );
            """);

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS player_achievements (
                steamid INTEGER NOT NULL,
                achievement_key TEXT NOT NULL,
                unlocked_utc TEXT NOT NULL,
                PRIMARY KEY (steamid, achievement_key)
            );
            """);

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS player_missions (
                steamid INTEGER NOT NULL,
                mission_key TEXT NOT NULL,
                period_key TEXT NOT NULL,
                progress INTEGER NOT NULL DEFAULT 0,
                completed_utc TEXT NULL,
                PRIMARY KEY (steamid, mission_key, period_key)
            );
            """);

        connection.Execute("""
            CREATE INDEX IF NOT EXISTS idx_players_total_xp
            ON players (total_xp DESC);
            """);

        connection.Execute("""
            CREATE INDEX IF NOT EXISTS idx_weapon_stats_steamid
            ON weapon_stats (steamid);
            """);

        connection.Execute("""
            CREATE INDEX IF NOT EXISTS idx_player_missions_steamid
            ON player_missions (steamid);
            """);
    }

    public PlayerProgress GetOrCreatePlayer(ulong steamId, string playerName)
    {
        using var connection = OpenConnection();
        var row = GetPlayerRow(connection, steamId);

        if (row is null)
        {
            var created = new PlayerProgress
            {
                SteamId = steamId,
                PlayerName = playerName,
                TotalXp = 0,
                Credits = 0,
                CrateTokens = 0
            };

            SavePlayer(created);
            return created;
        }

        if (!string.Equals(row.PlayerName, playerName, StringComparison.Ordinal))
        {
            connection.Execute(
                """
                UPDATE players
                SET player_name = @PlayerName,
                    updated_utc = @UpdatedUtc
                WHERE steamid = @SteamId;
                """,
                new
                {
                    SteamId = (long)steamId,
                    PlayerName = playerName,
                    UpdatedUtc = DateTimeOffset.UtcNow.ToString("O")
                });
        }

        return MapPlayerRow(row);
    }

    public PlayerProgress? GetPlayer(ulong steamId)
    {
        using var connection = OpenConnection();
        var row = GetPlayerRow(connection, steamId);
        return row is null ? null : MapPlayerRow(row);
    }

    public void SavePlayer(PlayerProgress progress)
    {
        using var connection = OpenConnection();
        connection.Execute(
            """
            INSERT INTO players (steamid, player_name, total_xp, credits, crate_tokens, created_utc, updated_utc)
            VALUES (@SteamId, @PlayerName, @TotalXp, @Credits, @CrateTokens, @NowUtc, @NowUtc)
            ON CONFLICT(steamid) DO UPDATE SET
                player_name = excluded.player_name,
                total_xp = excluded.total_xp,
                credits = excluded.credits,
                crate_tokens = excluded.crate_tokens,
                updated_utc = excluded.updated_utc;
            """,
            new
            {
                SteamId = (long)progress.SteamId,
                progress.PlayerName,
                progress.TotalXp,
                progress.Credits,
                progress.CrateTokens,
                NowUtc = DateTimeOffset.UtcNow.ToString("O")
            });
    }

    public PlayerStats GetOrCreateStats(ulong steamId)
    {
        using var connection = OpenConnection();
        var row = connection.QuerySingleOrDefault<PlayerStatsRow>(
            """
            SELECT steamid, kills, deaths, assists, headshots, knife_kills, round_wins, bomb_plants,
                   bomb_defuses, mvps, first_bloods, clutch_wins, multi_kills, best_kill_streak,
                   playtime_seconds, crates_opened, missions_completed, achievements_unlocked
            FROM player_stats
            WHERE steamid = @SteamId;
            """,
            new { SteamId = (long)steamId });

        if (row is null)
        {
            var created = new PlayerStats { SteamId = steamId };
            SaveStats(created);
            return created;
        }

        return MapStatsRow(row);
    }

    public void SaveStats(PlayerStats stats)
    {
        using var connection = OpenConnection();
        connection.Execute(
            """
            INSERT INTO player_stats (
                steamid, kills, deaths, assists, headshots, knife_kills, round_wins, bomb_plants,
                bomb_defuses, mvps, first_bloods, clutch_wins, multi_kills, best_kill_streak,
                playtime_seconds, crates_opened, missions_completed, achievements_unlocked)
            VALUES (
                @SteamId, @Kills, @Deaths, @Assists, @Headshots, @KnifeKills, @RoundWins, @BombPlants,
                @BombDefuses, @Mvps, @FirstBloods, @ClutchWins, @MultiKills, @BestKillStreak,
                @PlaytimeSeconds, @CratesOpened, @MissionsCompleted, @AchievementsUnlocked)
            ON CONFLICT(steamid) DO UPDATE SET
                kills = excluded.kills,
                deaths = excluded.deaths,
                assists = excluded.assists,
                headshots = excluded.headshots,
                knife_kills = excluded.knife_kills,
                round_wins = excluded.round_wins,
                bomb_plants = excluded.bomb_plants,
                bomb_defuses = excluded.bomb_defuses,
                mvps = excluded.mvps,
                first_bloods = excluded.first_bloods,
                clutch_wins = excluded.clutch_wins,
                multi_kills = excluded.multi_kills,
                best_kill_streak = excluded.best_kill_streak,
                playtime_seconds = excluded.playtime_seconds,
                crates_opened = excluded.crates_opened,
                missions_completed = excluded.missions_completed,
                achievements_unlocked = excluded.achievements_unlocked;
            """,
            new
            {
                SteamId = (long)stats.SteamId,
                stats.Kills,
                stats.Deaths,
                stats.Assists,
                stats.Headshots,
                stats.KnifeKills,
                stats.RoundWins,
                stats.BombPlants,
                stats.BombDefuses,
                stats.Mvps,
                stats.FirstBloods,
                stats.ClutchWins,
                stats.MultiKills,
                stats.BestKillStreak,
                stats.PlaytimeSeconds,
                stats.CratesOpened,
                stats.MissionsCompleted,
                stats.AchievementsUnlocked
            });
    }

    public IReadOnlyList<WeaponStatProgress> GetWeaponStats(ulong steamId)
    {
        using var connection = OpenConnection();
        var rows = connection.Query<WeaponStatRow>(
            """
            SELECT steamid, weapon_name, kills, headshots
            FROM weapon_stats
            WHERE steamid = @SteamId
            ORDER BY kills DESC, weapon_name ASC;
            """,
            new { SteamId = (long)steamId });

        return rows.Select(row => new WeaponStatProgress
        {
            SteamId = (ulong)row.SteamId,
            WeaponName = row.WeaponName,
            Kills = row.Kills,
            Headshots = row.Headshots
        }).ToList();
    }

    public void SaveWeaponStats(ulong steamId, IEnumerable<WeaponStatProgress> weaponStats)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute(
            "DELETE FROM weapon_stats WHERE steamid = @SteamId;",
            new { SteamId = (long)steamId },
            transaction);

        foreach (var weaponStat in weaponStats.Where(static stat => !string.IsNullOrWhiteSpace(stat.WeaponName)))
        {
            connection.Execute(
                """
                INSERT INTO weapon_stats (steamid, weapon_name, kills, headshots)
                VALUES (@SteamId, @WeaponName, @Kills, @Headshots);
                """,
                new
                {
                    SteamId = (long)steamId,
                    weaponStat.WeaponName,
                    weaponStat.Kills,
                    weaponStat.Headshots
                },
                transaction);
        }

        transaction.Commit();
    }

    public IReadOnlyList<PlayerAchievementState> GetAchievements(ulong steamId)
    {
        using var connection = OpenConnection();
        return connection.Query<PlayerAchievementRow>(
                """
                SELECT steamid, achievement_key, unlocked_utc
                FROM player_achievements
                WHERE steamid = @SteamId
                ORDER BY unlocked_utc ASC;
                """,
                new { SteamId = (long)steamId })
            .Where(row => !string.IsNullOrWhiteSpace(row.AchievementKey))
            .Select(row =>
            {
                var unlockedUtc = DateTimeOffset.TryParse(row.UnlockedUtc, null, DateTimeStyles.RoundtripKind, out var parsedUnlockedUtc)
                    ? parsedUnlockedUtc
                    : DateTimeOffset.UtcNow;
                return new PlayerAchievementState
                {
                    SteamId = (ulong)row.SteamId,
                    AchievementKey = row.AchievementKey,
                    UnlockedUtc = unlockedUtc
                };
            })
            .ToList();
    }

    public void SaveAchievement(PlayerAchievementState achievement)
    {
        using var connection = OpenConnection();
        connection.Execute(
            """
            INSERT INTO player_achievements (steamid, achievement_key, unlocked_utc)
            VALUES (@SteamId, @AchievementKey, @UnlockedUtc)
            ON CONFLICT(steamid, achievement_key) DO NOTHING;
            """,
            new
            {
                SteamId = (long)achievement.SteamId,
                achievement.AchievementKey,
                UnlockedUtc = achievement.UnlockedUtc.ToString("O")
            });
    }

    public IReadOnlyList<PlayerMissionState> GetMissionStates(ulong steamId)
    {
        using var connection = OpenConnection();
        return connection.Query<PlayerMissionRow>(
                """
                SELECT steamid, mission_key, period_key, progress, completed_utc
                FROM player_missions
                WHERE steamid = @SteamId;
                """,
                new { SteamId = (long)steamId })
            .Select(row => new PlayerMissionState
            {
                SteamId = (ulong)row.SteamId,
                MissionKey = row.MissionKey,
                PeriodKey = row.PeriodKey,
                Progress = row.Progress,
                CompletedUtc = string.IsNullOrWhiteSpace(row.CompletedUtc)
                    ? null
                    : DateTimeOffset.Parse(row.CompletedUtc, null, DateTimeStyles.RoundtripKind)
            })
            .ToList();
    }

    public void SaveMissionState(PlayerMissionState missionState)
    {
        using var connection = OpenConnection();
        connection.Execute(
            """
            INSERT INTO player_missions (steamid, mission_key, period_key, progress, completed_utc)
            VALUES (@SteamId, @MissionKey, @PeriodKey, @Progress, @CompletedUtc)
            ON CONFLICT(steamid, mission_key, period_key) DO UPDATE SET
                progress = excluded.progress,
                completed_utc = excluded.completed_utc;
            """,
            new
            {
                SteamId = (long)missionState.SteamId,
                missionState.MissionKey,
                missionState.PeriodKey,
                missionState.Progress,
                CompletedUtc = missionState.CompletedUtc?.ToString("O")
            });
    }

    public (int Rank, int TotalPlayers) GetRank(ulong steamId, long totalXp)
    {
        using var connection = OpenConnection();
        var totalPlayers = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM players;");
        if (totalPlayers <= 0)
        {
            return (0, 0);
        }

        var rank = connection.ExecuteScalar<int>(
            """
            SELECT COUNT(*) + 1
            FROM players
            WHERE total_xp > @TotalXp
               OR (total_xp = @TotalXp AND steamid < @SteamId);
            """,
            new
            {
                SteamId = (long)steamId,
                TotalXp = totalXp
            });

        return (rank, totalPlayers);
    }

    public IReadOnlyList<TopPlayerEntry> GetTopPlayers(int limit)
    {
        using var connection = OpenConnection();
        var rows = connection.Query<PlayerRow>(
            """
            SELECT steamid, player_name, total_xp, credits, crate_tokens
            FROM players
            ORDER BY total_xp DESC, steamid ASC
            LIMIT @Limit;
            """,
            new { Limit = Math.Max(1, limit) }).ToList();

        return rows.Select((row, index) => new TopPlayerEntry(index + 1, (ulong)row.SteamId, row.PlayerName, row.TotalXp)).ToList();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        return connection;
    }

    private static PlayerRow? GetPlayerRow(SqliteConnection connection, ulong steamId)
    {
        return connection.QuerySingleOrDefault<PlayerRow>(
            """
            SELECT steamid, player_name, total_xp, credits, crate_tokens
            FROM players
            WHERE steamid = @SteamId;
            """,
            new { SteamId = (long)steamId });
    }

    private static PlayerProgress MapPlayerRow(PlayerRow row)
    {
        return new PlayerProgress
        {
            SteamId = (ulong)row.SteamId,
            PlayerName = row.PlayerName,
            TotalXp = row.TotalXp,
            Credits = row.Credits,
            CrateTokens = row.CrateTokens
        };
    }

    private static PlayerStats MapStatsRow(PlayerStatsRow row)
    {
        return new PlayerStats
        {
            SteamId = (ulong)row.SteamId,
            Kills = row.Kills,
            Deaths = row.Deaths,
            Assists = row.Assists,
            Headshots = row.Headshots,
            KnifeKills = row.KnifeKills,
            RoundWins = row.RoundWins,
            BombPlants = row.BombPlants,
            BombDefuses = row.BombDefuses,
            Mvps = row.Mvps,
            FirstBloods = row.FirstBloods,
            ClutchWins = row.ClutchWins,
            MultiKills = row.MultiKills,
            BestKillStreak = row.BestKillStreak,
            PlaytimeSeconds = row.PlaytimeSeconds,
            CratesOpened = row.CratesOpened,
            MissionsCompleted = row.MissionsCompleted,
            AchievementsUnlocked = row.AchievementsUnlocked
        };
    }

    private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        var columns = connection.Query<TableInfoRow>($"PRAGMA table_info({tableName});").ToList();
        if (columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        connection.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
    }

    private void MigrateLegacyDatabase(string moduleDirectory)
    {
        if (File.Exists(_databasePath))
        {
            return;
        }

        var pluginRoot = Directory.GetParent(moduleDirectory)?.FullName;
        var candidates = new List<string>
        {
            Path.Combine(moduleDirectory, "xpx-levels.db"),
            Path.Combine(moduleDirectory, "XPX-levels.db")
        };

        if (!string.IsNullOrWhiteSpace(pluginRoot) && Directory.Exists(pluginRoot))
        {
            foreach (var siblingDirectory in Directory.GetDirectories(pluginRoot)
                         .Where(path => !string.Equals(path, moduleDirectory, StringComparison.OrdinalIgnoreCase)))
            {
                candidates.AddRange(Directory.EnumerateFiles(siblingDirectory, "*levels.db", SearchOption.TopDirectoryOnly));
            }
        }

        foreach (var candidate in candidates.Where(static path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)))
        {
            File.Copy(candidate!, _databasePath, overwrite: false);
            break;
        }
    }

    private sealed class PlayerRow
    {
        public long SteamId { get; init; }
        public string PlayerName { get; init; } = string.Empty;
        public long TotalXp { get; init; }
        public int Credits { get; init; }
        public int CrateTokens { get; init; }
    }

    private sealed class PlayerStatsRow
    {
        public long SteamId { get; init; }
        public int Kills { get; init; }
        public int Deaths { get; init; }
        public int Assists { get; init; }
        public int Headshots { get; init; }
        public int KnifeKills { get; init; }
        public int RoundWins { get; init; }
        public int BombPlants { get; init; }
        public int BombDefuses { get; init; }
        public int Mvps { get; init; }
        public int FirstBloods { get; init; }
        public int ClutchWins { get; init; }
        public int MultiKills { get; init; }
        public int BestKillStreak { get; init; }
        public long PlaytimeSeconds { get; init; }
        public int CratesOpened { get; init; }
        public int MissionsCompleted { get; init; }
        public int AchievementsUnlocked { get; init; }
    }

    private sealed class WeaponStatRow
    {
        public long SteamId { get; init; }
        public string WeaponName { get; init; } = string.Empty;
        public int Kills { get; init; }
        public int Headshots { get; init; }
    }

    private sealed class PlayerAchievementRow
    {
        public long SteamId { get; init; }
        public string AchievementKey { get; init; } = string.Empty;
        public string UnlockedUtc { get; init; } = string.Empty;
    }

    private sealed class PlayerMissionRow
    {
        public long SteamId { get; init; }
        public string MissionKey { get; init; } = string.Empty;
        public string PeriodKey { get; init; } = string.Empty;
        public int Progress { get; init; }
        public string? CompletedUtc { get; init; }
    }

    private sealed class TableInfoRow
    {
        public long Cid { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
