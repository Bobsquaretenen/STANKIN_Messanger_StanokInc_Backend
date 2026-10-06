# Stankin Messenger — Установка и запуск на Debian Linux

Инструкция описывает полный процесс установки окружения и запуска проекта ASP.NET Core Minimal API с PostgreSQL на чистой системе Debian 12 (Bookworm) или новее.

---

## 1. Обновление системы и базовые пакеты

Откройте терминал и выполните:

```bash
sudo apt update
sudo apt upgrade -y
sudo apt install -y curl wget unzip git apt-transport-https ca-certificates
```

---

## 2. Установка .NET 10 SDK

Проект нацелен на `net10.0`, поэтому нужен именно .NET 10 SDK.

### 2.1. Добавление репозитория Microsoft

```bash
# Скачиваем пакет с ключом Microsoft
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

# Устанавливаем его
sudo dpkg -i packages-microsoft-prod.deb

# Удаляем временный файл
rm packages-microsoft-prod.deb

# Обновляем список пакетов
sudo apt update
```

### 2.2. Установка SDK

```bash
sudo apt install -y dotnet-sdk-10.0
```

Если `dotnet-sdk-10.0` в репозитории ещё нет (на момент написания .NET 10 может быть в preview), установите через официальный скрипт:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0

# Добавьте в PATH (в ~/.bashrc):
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.bashrc
source ~/.bashrc
```

### 2.3. Проверка

```bash
dotnet --version
```

Должно вернуть версию, начинающуюся с `10.`.

---

## 3. Установка PostgreSQL

### 3.1. Установка из репозитория Debian

```bash
sudo apt install -y postgresql postgresql-contrib
```

Проверка статуса:

```bash
sudo systemctl status postgresql
```

Должно быть `active (running)`.

### 3.2. Настройка пользователя и БД

Откройте консоль PostgreSQL:

```bash
sudo -u postgres psql
```

Внутри выполните:

```sql
-- Создаём пользователя для приложения с паролем
CREATE USER stankin WITH PASSWORD 'ваш_пароль';

-- Создаём базу
CREATE DATABASE messenger OWNER stankin;

-- Даём права
GRANT ALL PRIVILEGES ON DATABASE messenger TO stankin;

-- Выходим
\q
```

Проверка:

```bash
sudo -u postgres psql -c "\l"
```

В списке должна появиться база `messenger`.

### 3.3. Права на схему public

По умолчанию PostgreSQL 15+ запрещает создавать таблицы в схеме `public` обычным пользователям. Нужно явно дать права:

```bash
sudo -u postgres psql -d messenger
```

```sql
GRANT ALL ON SCHEMA public TO stankin;
ALTER SCHEMA public OWNER TO stankin;
\q
```

---

## 4. Клонирование репозитория

```bash
# Перейдите в домашнюю директорию
cd ~

# Клонируйте проект
git clone https://github.com/ВАШ_ЛОГИН/ВАШ_РЕПОЗИТОРИЙ.git stankin-messenger

cd stankin-messenger
```

---

## 5. Импорт схемы БД

Загрузите SQL-дамп в базу `messenger`:

```bash
sudo -u postgres psql -d messenger -f messenger_db_backup.sql
```

> Если в дампе есть строки `\restrict` и `\unrestrict` — они не вызовут проблем при использовании `psql` (в отличие от pgAdmin).

Проверка:

```bash
sudo -u postgres psql -d messenger -c "\dt"
```

Должны появиться таблицы: `users`, `group_chat`, `message`, `chat_members`.

### 5.1. Если дамп пустой — добавьте пользователей

```bash
sudo -u postgres psql -d messenger
```

```sql
INSERT INTO public.users (login, password_hash, full_name, study_group, role) VALUES
('admin',    'admin', 'Администратор Системы',    NULL,        'admin'),
('teacher1', '12345', 'Иванов Иван Иванович',     NULL,        'teacher'),
('student1', '12345', 'Петров Пётр Петрович',     'ИДБ-22-01', 'student'),
('student2', '12345', 'Смирнов Алексей Олегович', 'ИДБ-22-01', 'student');
\q
```

---

## 6. Настройка строки подключения

Откройте `appsettings.json`:

```bash
nano appsettings.json
```

Замените `ConnectionStrings:DefaultConnection` на:

```json
"ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=messenger;Username=stankin;Password=ваш_пароль"
}
```

Сохраните: **Ctrl+O**, Enter, **Ctrl+X**.

---

## 7. Восстановление зависимостей и запуск

### 7.1. Восстановление NuGet-пакетов

```bash
dotnet restore
```

### 7.2. Запуск

```bash
dotnet run
```

В консоли появится:

```
Now listening on: http://localhost:5230
```

### 7.3. Проверка

Откройте в браузере (если есть графика) или через `curl`:

```bash
curl http://localhost:5230/
```

Должен вернуться HTML-код главной страницы.

---

## 8. Публикация и запуск как сервис (systemd)

Для работы в фоне лучше собрать приложение и зарегистрировать его как systemd-сервис.

### 8.1. Публикация в Release

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish
```

Флаг `--self-contained true` включает .NET Runtime внутрь, чтобы не зависеть от установленного SDK.

### 8.2. Перемещение в `/var/www`

```bash
sudo mkdir -p /var/www/stankin
sudo cp -r ./publish/* /var/www/stankin/
sudo chown -R www-data:www-data /var/www/stankin
sudo chmod -R 755 /var/www/stankin
```

### 8.3. Файл production-конфигурации

```bash
sudo nano /var/www/stankin/appsettings.Production.json
```

Содержимое:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=messenger;Username=stankin;Password=ваш_пароль"
  }
}
```

Права:

```bash
sudo chown www-data:www-data /var/www/stankin/appsettings.Production.json
sudo chmod 600 /var/www/stankin/appsettings.Production.json
```

### 8.4. systemd-сервис

```bash
sudo nano /etc/systemd/system/stankin.service
```

Содержимое:

```ini
[Unit]
Description=Stankin Messenger ASP.NET Core App
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/stankin
ExecStart=/var/www/stankin/StankinMessengerApi
Restart=always
RestartSec=10
SyslogIdentifier=stankin
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

Активируем:

```bash
sudo systemctl daemon-reload
sudo systemctl enable stankin
sudo systemctl start stankin
sudo systemctl status stankin
```

Проверка:

```bash
curl http://127.0.0.1:5000/
```

Логи:

```bash
sudo journalctl -u stankin -f
```

---

## 9. Nginx как обратный прокси

### 9.1. Установка

```bash
sudo apt install -y nginx
```

### 9.2. Конфигурация

```bash
sudo nano /etc/nginx/sites-available/stankin
```

Содержимое:

```nginx
server {
    listen 80;
    server_name ваш-домен.ru www.ваш-домен.ru;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Активировать:

```bash
sudo ln -s /etc/nginx/sites-available/stankin /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
```

### 9.3. Firewall

```bash
sudo apt install -y ufw
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

---

## 10. HTTPS через Let's Encrypt

После того как домен указывает на сервер:

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d ваш-домен.ru -d www.ваш-домен.ru
```

Certbot сам настроит HTTPS и автопродление сертификата.

Проверка автопродления:

```bash
sudo certbot renew --dry-run
```

---

## 11. Учётные записи для входа

| Роль | Логин | Пароль |
|---|---|---|
| Преподаватель | `teacher1` | `12345` |
| Студент | `student1` | `12345` |
| Администратор | `admin` | `admin` |

---

## 12. Возможные проблемы

| Ошибка | Решение |
|---|---|
| `dotnet: command not found` | Добавьте `~/.dotnet` в `PATH` (см. п. 2.2) |
| `Connection refused` при подключении к БД | Проверьте `sudo systemctl status postgresql` |
| `password authentication failed` | Неверный пароль в `appsettings.json` |
| `permission denied for schema public` | Выполните `GRANT ALL ON SCHEMA public TO stankin;` (п. 3.3) |
| `relation "users" does not exist` | Импортируйте SQL-дамп (п. 5) |
| Nginx 502 Bad Gateway | Приложение не запущено — `sudo systemctl status stankin` |
| Порт 5000 занят | Измените порт в `stankin.service` |

---

## 13. Полезные команды

```bash
# Перезапуск приложения
sudo systemctl restart stankin

# Просмотр логов
sudo journalctl -u stankin -n 100

# Просмотр логов Nginx
sudo tail -f /var/log/nginx/error.log

# Обновление проекта
cd ~/stankin-messenger
git pull
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish
sudo systemctl stop stankin
sudo cp -r ./publish/* /var/www/stankin/
sudo chown -R www-data:www-data /var/www/stankin
sudo systemctl start stankin

# Бэкап БД
sudo -u postgres pg_dump messenger > backup_$(date +%F).sql

# Восстановление из бэкапа
sudo -u postgres psql -d messenger < backup_2026-10-06.sql
```

---

## 14. Быстрая шпаргалка

```
1. sudo apt install -y curl wget git postgresql nginx
2. Установить .NET 10 SDK (через Microsoft-репозиторий или dotnet-install.sh)
3. Создать пользователя stankin и БД messenger
4. git clone <репозиторий>
5. Импортировать messenger_db_backup.sql
6. Указать строку подключения в appsettings.json
7. dotnet run  →  http://localhost:5230/
```

Для продакшена дополнительно: `dotnet publish`, systemd-сервис, Nginx, certbot.
