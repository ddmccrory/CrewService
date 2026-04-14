using System.ComponentModel.DataAnnotations;

namespace CrewService.BlazorUI.Models;

public class EmailInputModel
{
    [Required, EmailAddress, Display(Name = "Email Address")]
    public string? Email { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Please select an email type.")]
    public long EmailTypeCtrlNbr { get; set; }
}
