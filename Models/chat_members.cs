using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

[Table("chat_members")]
public class ChatMember
{
    [Column("id_user")] public int IdUser { get; set; }
    [Column("id_chat")] public int IdChat { get; set; }
}