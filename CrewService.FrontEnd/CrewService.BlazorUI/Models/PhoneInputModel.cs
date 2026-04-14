using System.ComponentModel.DataAnnotations;

namespace CrewService.BlazorUI.Models;

public class PhoneInputModel
{
    [Required, Display(Name = "Phone Number")]
    public string? Number { get; set; }

    [Display(Name = "Calling Order")]
    public int CallingOrder { get; set; } = 1;

    [Display(Name = "Dial One")]
    public bool DialOne { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Please select a phone type.")]
    public long PhoneTypeCtrlNbr { get; set; }
}
