using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MelonLoader;
#if IL2CPP
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.Property;
#if HAS_STEAMNETWORKLIB
using Il2CppSteamworks;
using SteamNetworkLib;
using SteamNetworkLib.Events;
using SteamNetworkLib.Exceptions;
using SteamNetworkLib.Models;
#endif
#else
using ScheduleOne.DevUtilities;
using ScheduleOne.Networking;
using ScheduleOne.Property;
#if HAS_STEAMNETWORKLIB
using Steamworks;
using SteamNetworkLib;
using SteamNetworkLib.Events;
using SteamNetworkLib.Exceptions;
using SteamNetworkLib.Models;
#endif
#endif

namespace OverheadSprinkler;

/// <summary>
/// Optional co-op sync bridge using SteamNetworkLib P2P messages to synchronize
/// spray particles/audio (S8) and property watering locks (S9) across all players.
/// Falls back cleanly to a no-op when SteamNetworkLib is not installed in UserLibs\.
/// </summary>
public static class SprinklerCoopSync
{
    private const string ProtocolPrefix = "OVHSPR|1|";

#if HAS_STEAMNETWORKLIB
    private const double InitRetryIntervalSeconds = 2.0;

    private interface ISyncBackend
    {
        bool Initialize();
        void ProcessIncoming();
        void Broadcast(string payload);
        void ResetLocks();
    }

    private static ISyncBackend? _backend;
    private static bool _permanentlyUnavailable;
    private static bool _loggedUnavailable;
    private static DateTime _nextInitAttemptUtc = DateTime.MinValue;

    public static bool IsAvailable => _backend != null;

    public static void TryInitialize()
    {
        if (_backend != null || _permanentlyUnavailable)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (now < _nextInitAttemptUtc)
        {
            return;
        }
        _nextInitAttemptUtc = now.AddSeconds(InitRetryIntervalSeconds);

        try
        {
            ISyncBackend? candidate = CreateAndInitBackend();
            if (candidate != null)
            {
                _backend = candidate;
                MelonLogger.Msg("Co-op effect & lock sync enabled via SteamNetworkLib.");
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is TypeLoadException || ex is BadImageFormatException)
        {
            _permanentlyUnavailable = true;
            if (!_loggedUnavailable)
            {
                _loggedUnavailable = true;
                MelonLogger.Msg(
                    "SteamNetworkLib was not found in UserLibs\\; co-op pot watering and additives still work, " +
                    "but spray visuals/audio and watering locks stay local to each player.");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"SteamNetworkLib init deferred: {ex.Message}");
        }
    }

    public static void ProcessIncoming()
    {
        _backend?.ProcessIncoming();
    }

    public static void ResetSession()
    {
        _backend?.ResetLocks();
        SprinklerController.ClearAllRemoteWateringLocks();
    }

    public static void BroadcastSpray(Property property, float durationSeconds, string kind)
    {
        if (property == null)
        {
            return;
        }

        TryInitialize();
        if (_backend == null)
        {
            return;
        }

        string key = GetPropertyKey(property);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        float clamped = Math.Clamp(durationSeconds, 0.4f, 30f);
        string payload = string.Concat(
            ProtocolPrefix,
            "SPRAY|",
            SanitizeField(key),
            "|",
            clamped.ToString("F3", CultureInfo.InvariantCulture),
            "|",
            SanitizeField(string.IsNullOrEmpty(kind) ? "water" : kind));
        _backend.Broadcast(payload);
    }

    public static void BroadcastLock(string propertyCode, bool isLocked)
    {
        if (string.IsNullOrEmpty(propertyCode))
        {
            return;
        }

        TryInitialize();
        if (_backend == null)
        {
            return;
        }

        string payload = string.Concat(
            ProtocolPrefix,
            "LOCK|",
            SanitizeField(propertyCode),
            "|",
            isLocked ? "1" : "0");
        _backend.Broadcast(payload);
    }
#else
    public static bool IsAvailable => false;

    public static void TryInitialize() { }

    public static void ProcessIncoming() { }

    public static void ResetSession()
    {
        SprinklerController.ClearAllRemoteWateringLocks();
    }

    public static void BroadcastSpray(Property property, float durationSeconds, string kind) { }

    public static void BroadcastLock(string propertyCode, bool isLocked) { }
#endif

    public static string GetPropertyKey(Property property)
    {
        if (property == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrEmpty(property.PropertyCode)
            ? property.PropertyCode
            : (property.PropertyName ?? string.Empty);
    }

    private static string SanitizeField(string value)
    {
        return value.Replace('|', '_').Trim();
    }

#if HAS_STEAMNETWORKLIB
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ISyncBackend? CreateAndInitBackend()
    {
        var backend = new SteamBackend();
        return backend.Initialize() ? backend : null;
    }

    private sealed class SteamBackend : ISyncBackend
    {
        private readonly struct RemoteLockEntry
        {
            public RemoteLockEntry(ulong senderId, DateTime expiresUtc)
            {
                SenderId = senderId;
                ExpiresUtc = expiresUtc;
            }

            public ulong SenderId { get; }
            public DateTime ExpiresUtc { get; }
        }

        private readonly SteamNetworkClient _client = new();
        private readonly Dictionary<string, RemoteLockEntry> _remoteLocks = new(StringComparer.OrdinalIgnoreCase);
        private DateTime _nextSweepUtc = DateTime.MinValue;

        public bool Initialize()
        {
            if (!_client.TryInitialize(out SteamNetworkException? error))
            {
                if (error != null)
                {
                    MelonLogger.Warning($"SteamNetworkLib not ready yet: {error.Message}");
                }
                return false;
            }

            _client.RegisterMessageHandler<TextMessage>(OnTextMessageReceived);
            _client.OnMemberLeft += OnMemberLeft;
            _client.OnLobbyLeft += OnLobbyLeft;
            return true;
        }

        public void ProcessIncoming()
        {
            if (!_client.IsInitialized)
            {
                return;
            }

            _client.ProcessIncomingMessages();
            SweepExpiredLocks();
        }

        public void Broadcast(string payload)
        {
            if (!_client.IsInitialized)
            {
                return;
            }

            if (_client.IsInLobby && _client.GetRemoteMembers().Count > 0)
            {
                _client.BroadcastTextMessage(payload);
                return;
            }

            // Fallback when Schedule I created the Steam lobby before SteamNetworkClient attached.
            if (!PersistentSingleton<Lobby>.InstanceExists || !PersistentSingleton<Lobby>.Instance.IsInLobby)
            {
                return;
            }

            ulong lobbyIdRaw = PersistentSingleton<Lobby>.Instance.LobbyID;
            if (lobbyIdRaw == 0)
            {
                return;
            }

            CSteamID lobbyId = new(lobbyIdRaw);
            CSteamID localId = GetLocalSteamId();
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
                if (memberId != CSteamID.Nil && memberId != localId)
                {
                    ObserveSend(_client.SendTextMessageAsync(memberId, payload));
                }
            }
        }

        public void ResetLocks()
        {
            _remoteLocks.Clear();
        }

        private CSteamID GetLocalSteamId()
        {
            CSteamID localId = _client.LocalPlayerId;
            if (localId == CSteamID.Nil)
            {
                localId = SteamUser.GetSteamID();
            }
            return localId;
        }

        private bool IsSenderInCurrentLobby(CSteamID senderId)
        {
            if (senderId == CSteamID.Nil)
            {
                return false;
            }

            if (_client.IsInLobby && _client.TryGetMember(senderId.m_SteamID, out _))
            {
                return true;
            }

            if (!PersistentSingleton<Lobby>.InstanceExists || !PersistentSingleton<Lobby>.Instance.IsInLobby)
            {
                return false;
            }

            ulong lobbyIdRaw = PersistentSingleton<Lobby>.Instance.LobbyID;
            if (lobbyIdRaw == 0)
            {
                return false;
            }

            CSteamID lobbyId = new(lobbyIdRaw);
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            for (int i = 0; i < memberCount; i++)
            {
                if (SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i) == senderId)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnTextMessageReceived(TextMessage message, CSteamID senderId)
        {
            if (message == null || string.IsNullOrEmpty(message.Content))
            {
                return;
            }

            if (!message.Content.StartsWith(ProtocolPrefix, StringComparison.Ordinal))
            {
                return;
            }

            CSteamID localId = GetLocalSteamId();
            if (senderId == localId || !IsSenderInCurrentLobby(senderId))
            {
                return;
            }

            string[] parts = message.Content.Split('|');
            if (parts.Length < 4)
            {
                return;
            }

            string command = parts[2];
            string propertyCode = parts[3];
            if (string.IsNullOrEmpty(propertyCode))
            {
                return;
            }

            if (string.Equals(command, "SPRAY", StringComparison.Ordinal) && parts.Length >= 6)
            {
                if (float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
                {
                    float clampedDuration = Math.Clamp(duration, 0.4f, 30f);
                    string kind = parts[5];
                    SprinklerController.PlayRemoteSprayEffect(propertyCode, clampedDuration, kind);
                }
            }
            else if (string.Equals(command, "LOCK", StringComparison.Ordinal) && parts.Length >= 5)
            {
                bool locked = string.Equals(parts[4], "1", StringComparison.Ordinal);
                if (locked)
                {
                    double lockSeconds = Math.Max(30.0, SprinklerConfig.SprayDurationSeconds + 10.0);
                    _remoteLocks[propertyCode] = new RemoteLockEntry(
                        senderId.m_SteamID,
                        DateTime.UtcNow.AddSeconds(lockSeconds));
                }
                else
                {
                    _remoteLocks.Remove(propertyCode);
                }

                SprinklerController.SetRemoteWateringLock(propertyCode, locked);
            }
        }

        private void OnMemberLeft(object? sender, MemberLeftEventArgs args)
        {
            if (args?.Member == null || _remoteLocks.Count == 0)
            {
                return;
            }

            ulong leavingId = args.Member.SteamId.m_SteamID;
            List<string>? toRelease = null;
            foreach (KeyValuePair<string, RemoteLockEntry> kvp in _remoteLocks)
            {
                if (kvp.Value.SenderId == leavingId)
                {
                    toRelease ??= new List<string>();
                    toRelease.Add(kvp.Key);
                }
            }

            if (toRelease == null)
            {
                return;
            }

            for (int i = 0; i < toRelease.Count; i++)
            {
                string key = toRelease[i];
                _remoteLocks.Remove(key);
                SprinklerController.SetRemoteWateringLock(key, isLocked: false);
            }
        }

        private void OnLobbyLeft(object? sender, LobbyLeftEventArgs args)
        {
            _remoteLocks.Clear();
            SprinklerController.ClearAllRemoteWateringLocks();
        }

        private void SweepExpiredLocks()
        {
            if (_remoteLocks.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (now < _nextSweepUtc)
            {
                return;
            }
            _nextSweepUtc = now.AddSeconds(2.0);

            List<string>? expired = null;
            foreach (KeyValuePair<string, RemoteLockEntry> kvp in _remoteLocks)
            {
                if (now >= kvp.Value.ExpiresUtc)
                {
                    expired ??= new List<string>();
                    expired.Add(kvp.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            for (int i = 0; i < expired.Count; i++)
            {
                string key = expired[i];
                _remoteLocks.Remove(key);
                SprinklerController.SetRemoteWateringLock(key, isLocked: false);
            }
        }

        private static void ObserveSend(Task<bool> sendTask)
        {
            sendTask.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        MelonLogger.Warning($"Co-op P2P send failed: {t.Exception.GetBaseException().Message}");
                    }
                },
                TaskScheduler.Default);
        }
    }
#endif
}
