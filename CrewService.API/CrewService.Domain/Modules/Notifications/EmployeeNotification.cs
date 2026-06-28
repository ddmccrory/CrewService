using CrewService.Domain.DomainEvents.Notifications;
using CrewService.Domain.Primitives;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Notifications;

/// <summary>
/// A recipient-centric, source-agnostic notification to an employee. Replaces the legacy
/// position-anchored RailroadPositionChange: the subject is now optional and polymorphic
/// (<see cref="NotificationSubject"/>), so any category of notification is supported
/// without schema changes.
/// </summary>
public sealed class EmployeeNotification : Entity
{
    private readonly List<NotificationAcknowledgement> _acknowledgements = [];

    public ControlNumber RailroadCtrlNbr { get; private set; }
    public ControlNumber EmployeeCtrlNbr { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public NotificationSubject? Subject { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime? EffectiveAtUtc { get; private set; }
    public bool RequiresAcknowledgement { get; private set; }
    public NotificationAudience Audience { get; private set; }
    public bool IncludeInHistory { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<NotificationAcknowledgement> Acknowledgements => _acknowledgements.AsReadOnly();

    /// <summary>
    /// Mirrors the legacy IsComplete semantics: if acknowledgement is required, the notice
    /// is acknowledged only once any attempt is Confirmed; otherwise it is always considered
    /// acknowledged.
    /// </summary>
    public bool IsAcknowledged =>
        !RequiresAcknowledgement || _acknowledgements.Any(a => a.Confirmed);

    private EmployeeNotification()
    {
        RailroadCtrlNbr = null!;
        EmployeeCtrlNbr = null!;
    }

    public static EmployeeNotification Create(
        ControlNumber railroadCtrlNbr,
        ControlNumber employeeCtrlNbr,
        string category,
        string message,
        bool requiresAcknowledgement,
        NotificationSubject? subject = null,
        DateTime? effectiveAtUtc = null,
        NotificationAudience audience = NotificationAudience.Employee,
        bool includeInHistory = true)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        var notification = new EmployeeNotification
        {
            RailroadCtrlNbr = railroadCtrlNbr,
            EmployeeCtrlNbr = employeeCtrlNbr,
            Category = category,
            Message = message,
            RequiresAcknowledgement = requiresAcknowledgement,
            Subject = subject,
            EffectiveAtUtc = effectiveAtUtc,
            Audience = audience,
            IncludeInHistory = includeInHistory,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Drive (currently stubbed) external delivery; the in-app record is persisted
        // atomically with the originating action by the caller's unit of work.
        notification.Raise(new EmployeeNotifiedDomainEvent(
            notification.CtrlNbr, railroadCtrlNbr, employeeCtrlNbr, category, requiresAcknowledgement));

        return notification;
    }

    /// <summary>
    /// Records a manual notification/acknowledgement attempt (dispatcher contacting the
    /// employee by phone, verbally, etc.). Equivalent to the legacy CreateChangeNotification.
    /// </summary>
    public NotificationAcknowledgement RecordAcknowledgement(
        AcknowledgementMethod method,
        bool confirmed,
        string acknowledgedByUser,
        DateTime? notifiedAtUtc = null,
        string? phoneNumber = null,
        string? notes = null)
    {
        var ack = NotificationAcknowledgement.Create(
            CtrlNbr, method, confirmed, notifiedAtUtc ?? DateTime.UtcNow, phoneNumber, notes);
        ack.CreatedBy = AuditStamp.Create(acknowledgedByUser);

        _acknowledgements.Add(ack);
        return ack;
    }

    /// <summary>
    /// Employee self-acknowledgement from the in-app notifications surface. Equivalent to the
    /// legacy AcceptNotification action: creates a confirmed Electronic acknowledgement.
    /// </summary>
    public NotificationAcknowledgement AcknowledgeElectronically(string acknowledgedByUser, string? notes = null)
    {
        return RecordAcknowledgement(
            AcknowledgementMethod.Electronic,
            confirmed: true,
            acknowledgedByUser: acknowledgedByUser,
            notifiedAtUtc: DateTime.UtcNow,
            phoneNumber: null,
            notes: notes);
    }
}
