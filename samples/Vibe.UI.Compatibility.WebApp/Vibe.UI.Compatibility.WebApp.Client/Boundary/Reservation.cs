using System.ComponentModel.DataAnnotations;

namespace Vibe.UI.Compatibility.WebApp.Client.Boundary;

public sealed class Reservation
{
    [Required]
    [Display(Name = "Guest name")]
    public string? Name { get; set; }

    [Range(1, 12)]
    public int Guests { get; set; } = 2;
}
