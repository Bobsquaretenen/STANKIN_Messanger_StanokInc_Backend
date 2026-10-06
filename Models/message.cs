using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

[Table("message")]
public class Message
{
    [Key, Column("id_message")] public int IdMessage { get; set; }
    [Column("id_chat")] public int IdChat { get; set; }
    [Column("id_user")] public int IdUser { get; set; }
    [Column("dispatch_time")] public DateTime DispatchTime { get; set; } = DateTime.UtcNow;
    [Column("info")] public string Info { get; set; } = string.Empty;
}