using System.ComponentModel.DataAnnotations;

namespace CrewService.BlazorUI.Models;

/// <summary>
/// Shared input model for editable employee profile fields used by both
/// the Employee edit modal and the Account profile page.
/// </summary>
public class EmployeeProfileModel
{
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    public string? Gender { get; set; }
    public string? Race { get; set; }

    [Display(Name = "Driver's License")]
    public string? DriversLicenseNumber { get; set; }

    [Display(Name = "Issuing State")]
    public string? IssuingState { get; set; }

    public string? MaritalStatus { get; set; }
    public bool AllowFmlaMarkOff { get; set; }
    public bool CallForOvertime { get; set; }
    public bool ProcessPayroll { get; set; }
    public bool TieUpOffProperty { get; set; }
}
