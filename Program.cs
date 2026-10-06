using Microsoft.EntityFrameworkCore;
using StankinMessengerApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseDefaultFiles(); 
app.UseStaticFiles();

// ===================== АВТОРИЗАЦИЯ =====================
app.MapPost("/api/login", async (LoginDto dto, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Login == dto.Login && u.PasswordHash == dto.Password);
    if (user == null) return Results.Unauthorized();

    return Results.Ok(new
    {
        idUser = user.IdUser,
        login = user.Login,
        fullName = user.FullName,
        studyGroup = user.StudyGroup,
        role = user.Role
    });
});

// ===================== ЧАТЫ ПОЛЬЗОВАТЕЛЯ =====================
app.MapGet("/api/users/{userId:int}/chats", async (int userId, AppDbContext db) =>
{
    var chatIds = await db.ChatMembers
        .Where(cm => cm.IdUser == userId)
        .Select(cm => cm.IdChat)
        .ToListAsync();

    var chats = await db.GroupChats
        .Where(c => chatIds.Contains(c.IdChat))
        .Select(c => new
        {
            idChat = c.IdChat,
            title = c.Title,
            idOwner = c.IdOwner,
            isOpen = c.IsOpen,
            membersCount = db.ChatMembers.Count(cm => cm.IdChat == c.IdChat)
        })
        .ToListAsync();

    return Results.Ok(chats);
});

// ===================== СООБЩЕНИЯ =====================
app.MapGet("/api/chats/{chatId:int}/messages", async (int chatId, AppDbContext db) =>
{
    var list = await db.Messages
        .Where(m => m.IdChat == chatId)
        .OrderBy(m => m.DispatchTime)
        .Select(m => new
        {
            idMessage = m.IdMessage,
            idChat = m.IdChat,
            idUser = m.IdUser,
            dispatchTime = m.DispatchTime,
            info = m.Info,
            authorName = db.Users.Where(u => u.IdUser == m.IdUser).Select(u => u.FullName ?? u.Login).FirstOrDefault(),
            authorRole = db.Users.Where(u => u.IdUser == m.IdUser).Select(u => u.Role).FirstOrDefault()
        }).ToListAsync();

    return Results.Ok(list);
});

app.MapPost("/api/messages", async (MessageDto dto, AppDbContext db) =>
{
    var chat = await db.GroupChats.FindAsync(dto.IdChat);
    if (chat == null) return Results.BadRequest(new { error = "Чат не найден" });

    var sender = await db.Users.FindAsync(dto.IdUser);
    if (sender == null) return Results.BadRequest(new { error = "Пользователь не найден" });

    // Преподаватели, администраторы и владелец чата пишут всегда.
    // Студенты — только если чат открыт.
    bool isPrivileged =
        sender.Role == "teacher" ||
        sender.Role == "admin"   ||
        chat.IdOwner == dto.IdUser;

    if (!chat.IsOpen && !isPrivileged)
        return Results.BadRequest(new { error = "Отправка сообщений закрыта" });

    var msg = new Message
    {
        IdChat = dto.IdChat,
        IdUser = dto.IdUser,
        Info = dto.Info,
        DispatchTime = DateTime.UtcNow
    };
    db.Messages.Add(msg);
    await db.SaveChangesAsync();
    return Results.Ok(new { idMessage = msg.IdMessage });
});

// ===================== УЧЕБНЫЕ ГРУППЫ (общие, для преподавателя) =====================
// Список всех учебных групп (уникальные study_group у студентов)
app.MapGet("/api/study-groups", async (AppDbContext db) =>
{
    var groups = await db.Users
        .Where(u => u.Role == "student" && u.StudyGroup != null)
        .GroupBy(u => u.StudyGroup!)
        .Select(g => new { name = g.Key, studentsCount = g.Count() })
        .OrderBy(g => g.name)
        .ToListAsync();
    return Results.Ok(groups);
});

// Студенты конкретной учебной группы
app.MapGet("/api/study-groups/{name}/students", async (string name, AppDbContext db) =>
{
    var students = await db.Users
        .Where(u => u.Role == "student" && u.StudyGroup == name)
        .Select(u => new {
            idUser = u.IdUser,
            login = u.Login,
            fullName = u.FullName
        })
        .OrderBy(u => u.fullName)
        .ToListAsync();
    return Results.Ok(students);
});

// ===================== УПРАВЛЕНИЕ ЧАТОМ =====================
// Создание чата с опциональным списком учебных групп
app.MapPost("/api/chats", async (CreateChatDto dto, AppDbContext db) =>
{
    var chat = new GroupChat
    {
        Title = dto.Title,
        IdOwner = dto.IdOwner,
        IsOpen = dto.IsOpen
    };
    db.GroupChats.Add(chat);
    await db.SaveChangesAsync();

    // Владелец автоматически становится участником
    db.ChatMembers.Add(new ChatMember { IdUser = dto.IdOwner, IdChat = chat.IdChat });

    // Добавляем всех студентов выбранных учебных групп
    var groupNames = dto.GroupNames ?? new List<string>();
    int addedStudents = 0;

    if (groupNames.Count > 0)
    {
        var studentIds = await db.Users
            .Where(u => u.Role == "student"
                     && u.StudyGroup != null
                     && groupNames.Contains(u.StudyGroup!))
            .Select(u => u.IdUser)
            .ToListAsync();

        foreach (var id in studentIds)
        {
            if (id != dto.IdOwner)
            {
                db.ChatMembers.Add(new ChatMember { IdUser = id, IdChat = chat.IdChat });
                addedStudents++;
            }
        }
    }

    await db.SaveChangesAsync();

    return Results.Ok(new {
        idChat = chat.IdChat,
        title = chat.Title,
        isOpen = chat.IsOpen,
        addedStudents = addedStudents
    });
});

app.MapPut("/api/chats/{chatId:int}", async (int chatId, UpdateChatDto dto, AppDbContext db) =>
{
    var chat = await db.GroupChats.FindAsync(chatId);
    if (chat == null) return Results.NotFound();
    if (!string.IsNullOrWhiteSpace(dto.Title)) chat.Title = dto.Title!;
    if (dto.IsOpen.HasValue) chat.IsOpen = dto.IsOpen.Value;
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapDelete("/api/chats/{chatId:int}", async (int chatId, AppDbContext db) =>
{
    var chat = await db.GroupChats.FindAsync(chatId);
    if (chat == null) return Results.NotFound();

    db.Messages.RemoveRange(db.Messages.Where(m => m.IdChat == chatId));
    db.ChatMembers.RemoveRange(db.ChatMembers.Where(cm => cm.IdChat == chatId));
    db.GroupChats.Remove(chat);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// ===================== ДОБАВЛЕНИЕ УЧЕБНЫХ ГРУПП В СУЩЕСТВУЮЩИЙ ЧАТ =====================
app.MapPost("/api/chats/{chatId:int}/study-groups",
    async (int chatId, AddGroupsDto dto, AppDbContext db) =>
{
    var chat = await db.GroupChats.FindAsync(chatId);
    if (chat == null) return Results.NotFound(new { error = "Чат не найден" });

    var groupNames = dto.GroupNames ?? new List<string>();
    if (groupNames.Count == 0)
        return Results.Ok(new { added = 0 });

    // Все студенты выбранных групп
    var studentIds = await db.Users
        .Where(u => u.Role == "student"
                 && u.StudyGroup != null
                 && groupNames.Contains(u.StudyGroup!))
        .Select(u => u.IdUser)
        .ToListAsync();

    // Уже есть в чате
    var existingIds = await db.ChatMembers
        .Where(cm => cm.IdChat == chatId)
        .Select(cm => cm.IdUser)
        .ToListAsync();

    int added = 0;
    foreach (var id in studentIds)
    {
        if (!existingIds.Contains(id))
        {
            db.ChatMembers.Add(new ChatMember { IdUser = id, IdChat = chatId });
            added++;
        }
    }
    await db.SaveChangesAsync();

    return Results.Ok(new { added = added });
});

// ===================== УЧАСТНИКИ ЧАТА =====================
app.MapGet("/api/chats/{chatId:int}/members", async (int chatId, AppDbContext db) =>
{
    var members = await db.ChatMembers
        .Where(cm => cm.IdChat == chatId)
        .Join(db.Users, cm => cm.IdUser, u => u.IdUser,
              (cm, u) => new {
                  idUser = u.IdUser,
                  login = u.Login,
                  fullName = u.FullName,
                  studyGroup = u.StudyGroup,
                  role = u.Role
              })
        .ToListAsync();
    return Results.Ok(members);
});

app.MapPost("/api/chats/{chatId:int}/members", async (int chatId, AddMemberDto dto, AppDbContext db) =>
{
    if (!await db.ChatMembers.AnyAsync(cm => cm.IdChat == chatId && cm.IdUser == dto.IdUser))
    {
        db.ChatMembers.Add(new ChatMember { IdChat = chatId, IdUser = dto.IdUser });
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

app.MapDelete("/api/chats/{chatId:int}/members/{userId:int}",
    async (int chatId, int userId, AppDbContext db) =>
{
    var cm = await db.ChatMembers.FindAsync(userId, chatId);
    if (cm != null)
    {
        db.ChatMembers.Remove(cm);
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

// ===================== АДМИН-ПАНЕЛЬ =====================
app.MapGet("/api/admin/stats", async (AppDbContext db) => Results.Ok(new
{
    students = await db.Users.CountAsync(u => u.Role == "student"),
    teachers = await db.Users.CountAsync(u => u.Role == "teacher"),
    groups = await db.GroupChats.CountAsync()
}));

app.MapGet("/api/admin/users", async (string? role, AppDbContext db) =>
{
    var q = db.Users.AsQueryable();
    if (!string.IsNullOrWhiteSpace(role)) q = q.Where(u => u.Role == role);

    var list = await q.OrderBy(u => u.IdUser)
        .Select(u => new {
            idUser = u.IdUser,
            login = u.Login,
            fullName = u.FullName,
            password = u.PasswordHash, 
            studyGroup = u.StudyGroup,
            role = u.Role
        })
        .ToListAsync();
    return Results.Ok(list);
});

app.MapPost("/api/admin/users", async (CreateUserDto dto, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Login == dto.Login))
        return Results.BadRequest(new { error = "Логин уже занят" });

    var user = new User {
        Login = dto.Login,
        PasswordHash = string.IsNullOrWhiteSpace(dto.Password) ? "12345" : dto.Password,
        FullName = dto.FullName,
        StudyGroup = string.IsNullOrWhiteSpace(dto.StudyGroup) ? null : dto.StudyGroup,
        Role = dto.Role
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { idUser = user.IdUser });
});

app.MapDelete("/api/admin/users/{id:int}", async (int id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();

    db.Messages.RemoveRange(db.Messages.Where(m => m.IdUser == id));
    db.ChatMembers.RemoveRange(db.ChatMembers.Where(cm => cm.IdUser == id));
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/api/admin/study-groups", async (AppDbContext db) =>
{
    var groups = await db.Users
        .Where(u => u.Role == "student" && u.StudyGroup != null)
        .GroupBy(u => u.StudyGroup!)
        .Select(g => new { name = g.Key, studentsCount = g.Count() })
        .OrderBy(g => g.name)
        .ToListAsync();
    return Results.Ok(groups);
});

app.MapGet("/api/admin/study-groups/{name}/students", async (string name, AppDbContext db) =>
{
    var students = await db.Users
        .Where(u => u.Role == "student" && u.StudyGroup == name)
        .Select(u => new {
            idUser = u.IdUser,
            login = u.Login, 
            password = u.PasswordHash, 
            fullName = u.FullName
        })
        .OrderBy(u => u.fullName)
        .ToListAsync();
    return Results.Ok(students);
});

app.Run();

// ===================== DTO =====================
public record LoginDto(string Login, string Password);
public record MessageDto(int IdChat, int IdUser, string Info);
public record CreateChatDto(string Title, int IdOwner, bool IsOpen = true, List<string>? GroupNames = null);
public record UpdateChatDto(string? Title, bool? IsOpen);
public record AddMemberDto(int IdUser);
public record AddGroupsDto(List<string> GroupNames);
public record CreateUserDto(string Login, string Password, string? FullName, string? StudyGroup, string Role);