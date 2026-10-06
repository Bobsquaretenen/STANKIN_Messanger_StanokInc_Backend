# Stankin Messenger — Инструкция по запуску

Инструкция описывает полный процесс запуска проекта ASP.NET Core Minimal API с PostgreSQL на новом ПК, где из инструментов разработки установлен только Rider.

---

## 1. Установка необходимого ПО

### 1.1. .NET 10 SDK

Проект нацелен на `net10.0`, нужен именно .NET 10 SDK.

**Через PowerShell (от имени администратора):**

```powershell
winget install Microsoft.DotNet.SDK.10
```

**Вручную:** https://dotnet.microsoft.com/download/dotnet/10.0 → скачать SDK 10.0.x для Windows x64.

Проверка:

```powershell
dotnet --version
```

После установки **перезапустите Rider**.

### 1.2. PostgreSQL и pgAdmin

1. Откройте https://www.postgresql.org/download/windows/
2. Скачайте PostgreSQL 16 (или новее) для Windows x64.
3. Установщик:
   - Пароль пользователя `postgres` — задайте и **запомните** (например, `secret`).
   - Порт — оставьте `5432`.
   - Stack Builder — можно не запускать.

pgAdmin появится в меню «Пуск» вместе с PostgreSQL.

---

## 2. Клонирование репозитория

### Через Rider

1. Откройте Rider → **Git → Clone**.
2. Вставьте ссылку на репозиторий:
   ```
   https://github.com/ВАШ_ЛОГИН/ВАШ_РЕПОЗИТОРИЙ.git
   ```
3. Выберите папку и нажмите **Clone**.
4. На запрос «Trust and Open» → **Trust Project**.

### Через терминал

```powershell
git clone https://github.com/ВАШ_ЛОГИН/ВАШ_РЕПОЗИТОРИЙ.git
```

Затем в Rider: **File → Open** → папка проекта.

---

## 3. Восстановление зависимостей

Откройте вкладку **Terminal** в Rider и выполните:

```bash
dotnet restore
```

Будут скачаны NuGet-пакеты: Entity Framework Core, Npgsql и другие.

Если Rider всё ещё показывает ошибки — **File → Invalidate Caches / Restart**.

---

## 4. Настройка подключения к базе данных

Откройте `appsettings.json` и замените `ВАШ_ПАРОЛЬ` на пароль пользователя `postgres`:

```json
"ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=messenger;Username=postgres;Password=ВАШ_ПАРОЛЬ"
}
```

Пример:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=messenger;Username=postgres;Password=secret"
```

Сохраните файл (Ctrl+S).

---

## 5. Создание базы данных и импорт схемы

### 5.1. Создайте пустую базу

1. Откройте **pgAdmin 4**.
2. Введите пароль `postgres`.
3. Правой кнопкой по **Databases** → **Create** → **Database...**.
4. Имя: `messenger`. Нажмите **Save**.

### 5.2. Импортируйте схему

1. Правой кнопкой по базе `messenger` → **Query Tool**.
2. Откройте файл `messenger_db_backup.sql` (из репозитория).
3. **Удалите** из файла строки `\restrict` и `\unrestrict` — pgAdmin их не понимает.
4. Нажмите **Execute** (F5).

Должно появиться **Query returned successfully**.

### 5.3. Проверьте таблицы

Правой кнопкой по `messenger` → **Refresh** → **Schemas → public → Tables**. Должны быть:
- `users`
- `group_chat`
- `message`
- `chat_members`

### 5.4. Если дамп пустой — добавьте пользователей

```sql
INSERT INTO public.users (login, password_hash, full_name, study_group, role) VALUES
('admin',    'admin', 'Администратор Системы',    NULL,        'admin'),
('teacher1', '12345', 'Иванов Иван Иванович',     NULL,        'teacher'),
('student1', '12345', 'Петров Пётр Петрович',     'ИДБ-22-01', 'student'),
('student2', '12345', 'Смирнов Алексей Олегович', 'ИДБ-22-01', 'student');
```

---

## 6. Запуск проекта

1. В правом верхнем углу Rider выберите конфигурацию **StankinMessengerApi**.
2. Нажмите **Run** (Shift+F10).
3. Дождитесь строки в консоли:
   ```
   Now listening on: http://localhost:5230
   ```
4. Откройте в браузере: `http://localhost:5230/`.

---

## 7. Учётные записи для входа

| Роль | Логин | Пароль |
|---|---|---|
| Преподаватель | `teacher1` | `12345` |
| Студент | `student1` | `12345` |
| Администратор | `admin` | `admin` |

---

## 8. Возможные проблемы

| Ошибка | Решение |
|---|---|
| Rider не видит .NET 10 SDK | Установите SDK, перезапустите Rider |
| `dotnet restore` падает | Проверьте интернет, попробуйте `dotnet restore --interactive` |
| Ошибка подключения к БД | Проверьте `appsettings.json` и наличие базы `messenger` в pgAdmin |
| `relation "users" does not exist` | Схема не импортирована — выполните п. 5.2 |
| `password authentication failed` | Неверный пароль — проверьте `appsettings.json` |
| Ошибка `\restrict` при импорте | Удалите строки `\restrict` и `\unrestrict` из SQL-файла |
| Вход даёт 401 Unauthorized | Нет пользователей в БД — добавьте через SQL (п. 5.4) |
| Порт 5230 занят | Измените порт в `Properties/launchSettings.json` |

---

## 9. Краткая шпаргалка

Для повторного запуска на уже настроенном ПК:

```
1. Открыть проект в Rider.
2. Убедиться, что PostgreSQL запущен (Служба Windows: postgresql-x64-16).
3. Run (Shift+F10).
4. Открыть http://localhost:5230/.
```

Всё остальное — только для первой настройки.
