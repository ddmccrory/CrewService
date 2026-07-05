namespace CrewService.Domain.Models.Seniority;

public enum StateType
{
    Active = 1,
    CutBack = 2,
    Inactive = 3,

    /// <summary>
    /// The employee is off the property entirely (e.g. terminated, dismissed, retired).
    /// Unlike the roster-scoped states, transitioning a seniority record into an off-property
    /// state applies employee-wide: every seniority record is vacated and end-dated, individual
    /// qualifications are removed, and active certifications are cancelled.
    /// </summary>
    OffProperty = 4
}
