namespace CrewService.Domain.Models.Seniority;

public enum RosterType
{
    /// <summary>Employees in full active service subject to normal seniority rules.</summary>
    Active = 0,

    /// <summary>New hires in the training pipeline — pending FRA certification completion.</summary>
    Training = 1
}
