using System.ComponentModel.DataAnnotations;

namespace CrewService.BlazorUI.Models;

public class AddressInputModel
{
    [Required, Display(Name = "Address 1")]
    public string? Address1 { get; set; }

    [Display(Name = "Address 2")]
    public string? Address2 { get; set; }

    [Required]
    public string? City { get; set; }

    [Required]
    public string? State { get; set; }

    [Required, Display(Name = "Zip Code")]
    public string? ZipCode { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "Please select an address type.")]
    public long AddressTypeCtrlNbr { get; set; }
}
