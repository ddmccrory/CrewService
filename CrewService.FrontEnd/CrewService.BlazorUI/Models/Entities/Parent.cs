using System.ComponentModel.DataAnnotations;

namespace CrewService.BlazorUI.Models.Entities;

public sealed class Parent
{
    public long CtrlNbr { get; set; }

    [Required]
    [StringLength(50)]
    public string? Name { get; set; }

    public List<Railroad> Railroads { get; set; } = [];
}
