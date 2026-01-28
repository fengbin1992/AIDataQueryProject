using System.ComponentModel.DataAnnotations;
using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.DTOs.User;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "昵称不能为空")]
    [StringLength(50, ErrorMessage = "昵称长度不能超过50个字符")]
    public string Nickname { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }

    public List<string>? PlatformCodes { get; set; }
}
