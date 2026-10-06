using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("group_chat")]
public class GroupChat
{
    [Key, Column("id_chat")] public int IdChat { get; set; }
    [Column("title")] public string Title { get; set; } = string.Empty;
    [Column("id_owner")] public int IdOwner { get; set; }
    [Column("rules")] public string? Rules { get; set; }
    [Column("is_open")] public bool IsOpen { get; set; } = true;
}