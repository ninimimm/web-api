namespace WebApi.MinimalApi.Models;

public class UpdateUserDto
{
    public string? login { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
}