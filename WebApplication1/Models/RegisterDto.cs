namespace UserRegisterApi.Models;

public class RegisterDto
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Firstname { get; set; } = default!;
    public string Surname { get; set; } = default!;
    public string DateOfBirth { get; set; } = default!; // รองรับ "1990-02-29" หรือ "29-02-1990"
    public string Gender { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Profile_Img { get; set; } // base64 หรือ null
}
