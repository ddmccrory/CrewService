using System.ComponentModel.DataAnnotations;

namespace CrewService.BlazorUI.Models.Entities;

public sealed class Railroad
{
    public long CtrlNbr { get; set; }

    [Required(ErrorMessage = "The Parent field is required.")]
    public long ParentCtrlNbr { get; set; }

    [Required(ErrorMessage = "The Railroad Mark field is required.")]
    [StringLength(4)]
    public required string RRMark { get; set; }

    [Required]
    [StringLength(50)]
    public required string Name { get; set; }
}
