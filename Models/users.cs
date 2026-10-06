using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("users")]
public class User
{
    [Key, Column("id_user")] public int IdUser { get; set; }
    [Column("login")] public string Login { get; set; } = string.Empty;
    [Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
    [Column("full_name")] public string? FullName { get; set; }
    [Column("study_group")] public string? StudyGroup { get; set; }
    [Column("role")] public string Role { get; set; } = string.Empty;
} 