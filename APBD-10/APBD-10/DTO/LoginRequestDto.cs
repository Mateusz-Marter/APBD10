using System.ComponentModel.DataAnnotations;

namespace APBD_10.DTO;

public class LoginRequestDto
{
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
}