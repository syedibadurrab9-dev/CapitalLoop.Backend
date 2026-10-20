using System.Collections.Concurrent;

namespace WebApi.Hubs;

public class InvestorChatSession
{
    public string InvestorEmail { get; set; } = string.Empty;
    public string InvestorName { get; set; } = string.Empty;
    public string? AssignedAdvisorEmail { get; set; }
    public string? AssignedAdvisorName { get; set; }
    public string? AssignedAdvisorLicense { get; set; }
    public string Status { get; set; } = "UNASSIGNED"; // UNASSIGNED, ENGAGED, RESOLVED
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EngagedAt { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; } = true;
    public int UnreadCountByAdvisor { get; set; } = 0;
    public List<ChatMessagePayload> Messages { get; set; } = new();
}

public class ChatMessagePayload
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsAgentSender { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public interface ISupportSessionManager
{
    void AddConnection(string connectionId, string email, string name, bool isAdvisor);
    (string Email, string Name, bool IsAdvisor, bool IsCompletelyDisconnected) RemoveConnection(string connectionId);
    IReadOnlyCollection<string> GetUserConnections(string email, bool isAdvisor);
    bool IsUserOnline(string email, bool isAdvisor);

    (bool Success, string Message, InvestorChatSession Session) TryClaimSession(string investorEmail, string advisorEmail, string advisorName, string? advisorLicense, bool forceTakeover = false);
    (bool Success, string Message, InvestorChatSession Session) ReleaseSession(string investorEmail, string advisorEmail);
    InvestorChatSession GetOrCreateSession(string investorEmail, string investorName);
    InvestorChatSession? GetSession(string investorEmail);
    IReadOnlyCollection<InvestorChatSession> GetAllSessions();
    
    ChatMessagePayload AddMessage(string investorEmail, string senderEmail, string senderName, string text, bool isAgentSender);
    IReadOnlyList<ChatMessagePayload> GetHistory(string investorEmail);
    void MarkThreadRead(string investorEmail);
}

public class SupportSessionManager : ISupportSessionManager
{
    private readonly ConcurrentDictionary<string, (string Email, string Name, bool IsAdvisor)> _connectionMetadata = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _clientConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _advisorConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InvestorChatSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lockObj = new();

    public void AddConnection(string connectionId, string email, string name, bool isAdvisor)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        _connectionMetadata[connectionId] = (normalizedEmail, name, isAdvisor);

        var targetDict = isAdvisor ? _advisorConnections : _clientConnections;
        var connectionSet = targetDict.GetOrAdd(normalizedEmail, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        connectionSet.TryAdd(connectionId, 0);

        if (!isAdvisor)
        {
            var session = GetOrCreateSession(normalizedEmail, name);
            session.IsOnline = true;
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    public (string Email, string Name, bool IsAdvisor, bool IsCompletelyDisconnected) RemoveConnection(string connectionId)
    {
        if (!_connectionMetadata.TryRemove(connectionId, out var meta))
        {
            return (string.Empty, string.Empty, false, false);
        }

        var targetDict = meta.IsAdvisor ? _advisorConnections : _clientConnections;
        bool isCompletelyDisconnected = false;

        if (targetDict.TryGetValue(meta.Email, out var connectionSet))
        {
            connectionSet.TryRemove(connectionId, out _);
            if (connectionSet.IsEmpty)
            {
                targetDict.TryRemove(meta.Email, out _);
                isCompletelyDisconnected = true;
            }
        }
        else
        {
            isCompletelyDisconnected = true;
        }

        if (!meta.IsAdvisor && isCompletelyDisconnected)
        {
            if (_sessions.TryGetValue(meta.Email, out var session))
            {
                session.IsOnline = false;
                session.LastActivityAt = DateTime.UtcNow;
            }
        }

        return (meta.Email, meta.Name, meta.IsAdvisor, isCompletelyDisconnected);
    }

    public IReadOnlyCollection<string> GetUserConnections(string email, bool isAdvisor)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var targetDict = isAdvisor ? _advisorConnections : _clientConnections;
        if (targetDict.TryGetValue(normalizedEmail, out var connectionSet))
        {
            return connectionSet.Keys.ToList();
        }
        return Array.Empty<string>();
    }

    public bool IsUserOnline(string email, bool isAdvisor)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var targetDict = isAdvisor ? _advisorConnections : _clientConnections;
        return targetDict.TryGetValue(normalizedEmail, out var connectionSet) && !connectionSet.IsEmpty;
    }

    public (bool Success, string Message, InvestorChatSession Session) TryClaimSession(
        string investorEmail, string advisorEmail, string advisorName, string? advisorLicense, bool forceTakeover = false)
    {
        var normalizedInvestor = investorEmail.Trim().ToLowerInvariant();
        var normalizedAdvisor = advisorEmail.Trim().ToLowerInvariant();

        lock (_lockObj)
        {
            var session = GetOrCreateSession(normalizedInvestor, "CapitalLoop Investor");

            // If already claimed by this exact advisor, it's already theirs
            if (session.Status == "ENGAGED" && string.Equals(session.AssignedAdvisorEmail, normalizedAdvisor, StringComparison.OrdinalIgnoreCase))
            {
                return (true, "Already engaged with this investor session.", session);
            }

            // If claimed by another advisor and not forcing takeover
            if (session.Status == "ENGAGED" && !string.IsNullOrEmpty(session.AssignedAdvisorEmail) && !forceTakeover)
            {
                return (false, $"Session is already engaged by Advisor: {session.AssignedAdvisorName} ({session.AssignedAdvisorEmail})", session);
            }

            // Claim / Engage
            session.AssignedAdvisorEmail = normalizedAdvisor;
            session.AssignedAdvisorName = advisorName;
            session.AssignedAdvisorLicense = advisorLicense ?? "SECP-ADV-REG";
            session.Status = "ENGAGED";
            session.EngagedAt = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;

            return (true, "Investor session successfully claimed and engaged.", session);
        }
    }

    public (bool Success, string Message, InvestorChatSession Session) ReleaseSession(string investorEmail, string advisorEmail)
    {
        var normalizedInvestor = investorEmail.Trim().ToLowerInvariant();
        var normalizedAdvisor = advisorEmail.Trim().ToLowerInvariant();

        lock (_lockObj)
        {
            var session = GetOrCreateSession(normalizedInvestor, "CapitalLoop Investor");

            session.AssignedAdvisorEmail = null;
            session.AssignedAdvisorName = null;
            session.AssignedAdvisorLicense = null;
            session.Status = "UNASSIGNED";
            session.LastActivityAt = DateTime.UtcNow;

            return (true, "Investor session returned to common advisory queue.", session);
        }
    }

    public InvestorChatSession GetOrCreateSession(string investorEmail, string investorName)
    {
        var normalized = investorEmail.Trim().ToLowerInvariant();
        return _sessions.GetOrAdd(normalized, email => new InvestorChatSession
        {
            InvestorEmail = email,
            InvestorName = string.IsNullOrWhiteSpace(investorName) ? "CapitalLoop Investor" : investorName,
            Status = "UNASSIGNED",
            OpenedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsOnline = true
        });
    }

    public InvestorChatSession? GetSession(string investorEmail)
    {
        var normalized = investorEmail.Trim().ToLowerInvariant();
        _sessions.TryGetValue(normalized, out var session);
        return session;
    }

    public IReadOnlyCollection<InvestorChatSession> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    public ChatMessagePayload AddMessage(string investorEmail, string senderEmail, string senderName, string text, bool isAgentSender)
    {
        var session = GetOrCreateSession(investorEmail, isAgentSender ? "CapitalLoop Investor" : senderName);
        var msg = new ChatMessagePayload
        {
            SenderEmail = senderEmail,
            SenderName = senderName,
            Text = text,
            IsAgentSender = isAgentSender,
            Timestamp = DateTime.UtcNow
        };

        lock (_lockObj)
        {
            session.Messages.Add(msg);
            session.LastActivityAt = DateTime.UtcNow;
            if (!isAgentSender)
            {
                session.UnreadCountByAdvisor++;
            }
        }

        return msg;
    }

    public IReadOnlyList<ChatMessagePayload> GetHistory(string investorEmail)
    {
        var session = GetSession(investorEmail);
        if (session == null) return Array.Empty<ChatMessagePayload>();
        lock (_lockObj)
        {
            return session.Messages.ToList();
        }
    }

    public void MarkThreadRead(string investorEmail)
    {
        var session = GetSession(investorEmail);
        if (session != null)
        {
            lock (_lockObj)
            {
                session.UnreadCountByAdvisor = 0;
            }
        }
    }
}
