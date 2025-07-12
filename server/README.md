# DeathRoom Server — Документация

## Оглавление

- [Общее описание](#общее-описание)
- [Архитектура и структура проекта](#архитектура-и-структура-проекта)
- [Слои и их назначение](#слои-и-их-назначение)
- [Запуск сервера](#запуск-сервера)
  - [Требования](#требования)
  - [Быстрый старт](#быстрый-старт)
  - [Запуск через .NET CLI](#запуск-через-net-cli)
  - [Запуск через Docker](#запуск-через-docker)
  - [Диагностика запуска](#диагностика-запуска)
  - [Остановка сервера](#остановка-сервера)
- [Конфигурация](#конфигурация)
  - [Переменные окружения](#переменные-окружения)
  - [Файл конфигурации](#файл-конфигурации)
- [Основные компоненты](#основные-компоненты)
- [Сетевые пакеты и DTO](#сетевые-пакеты-и-dto)
- [Логирование и завершение работы](#логирование-и-завершение-работы)
- [Тестирование](#тестирование)
- [Рекомендации по развитию](#рекомендации-по-развитию)
- [Устранение неполадок](#устранение-неполадок)

---

## Общее описание

**DeathRoom Server** — это серверная часть многопользовательской игры, реализованная по принципам DDD (Domain-Driven Design) и Clean Architecture. Сервер отвечает за хранение и обработку состояния игрового мира, управление сессиями игроков, обработку сетевых пакетов и игровой логики.

### Основные возможности

- **Многопользовательская игра** — поддержка множества одновременных игроков
- **Реальное время** — низкая задержка, быстрая обработка событий
- **Модульная архитектура** — легко расширяемая и поддерживаемая кодовая база
- **In-memory хранение** — быстрый доступ к данным без внешних зависимостей
- **Сетевая оптимизация** — эффективная передача данных через LiteNetLib

---

## Архитектура и структура проекта

```
server/
├── DeathRoom.sln                    # Solution файл
├── DeathRoom-Backend/              # Точка входа, запуск сервера, сетевой слой
│   ├── Program.cs                   # Точка входа, DI конфигурация
│   ├── ServerRunner.cs              # HostedService, жизненный цикл
│   ├── GameServer.cs                # Сетевой сервер, LiteNetLib интеграция
│   ├── appsettings.json             # Конфигурация
│   └── DeathRoom-Backend.csproj     # Проект
├── DeathRoom.Application/           # Бизнес-логика, сервисы
│   ├── GameLoopService.cs           # Основной игровой цикл
│   ├── PacketHandlerService.cs      # Обработка сетевых пакетов
│   ├── PlayerSessionService.cs      # Управление сессиями
│   ├── WorldStateService.cs         # Хранение состояния мира
│   ├── HitRegistrationService.cs    # Регистрация урона
│   ├── HitPhysicsService.cs         # Физика попаданий
│   └── DeathRoom.Application.csproj # Проект
├── DeathRoom.Domain/                # Бизнес-сущности, value objects
│   ├── Player.cs                    # Сущность игрока
│   ├── PlayerState.cs               # Состояние игрока
│   ├── PlayerSnapshot.cs            # Снапшот игрока
│   ├── WorldState.cs                # Состояние мира
│   ├── Match.cs                     # Сущность матча
│   ├── MatchPlayer.cs               # Игрок в матче
│   ├── Vector3.cs                   # Value object координат
│   └── DeathRoom.Domain.csproj      # Проект
├── DeathRoom.Common/                # DTO, сетевые пакеты, сериализация
│   ├── dto/                         # Data Transfer Objects
│   │   ├── PlayerState.cs           # DTO состояния игрока
│   │   ├── Vector3Serializable.cs   # DTO координат
│   │   └── PlayerSnapshot.cs        # DTO снапшота
│   ├── network/                     # Сетевые пакеты
│   │   ├── IPacket.cs               # Интерфейс пакета
│   │   ├── LoginPacket.cs           # Пакет авторизации
│   │   ├── PlayerMovePacket.cs      # Пакет движения
│   │   ├── PlayerHitPacket.cs       # Пакет попадания
│   │   ├── PickUpArmorPacket.cs     # Пакет поднятия брони
│   │   └── WorldStatePacket.cs      # Пакет состояния мира
│   └── DeathRoom.Common.csproj      # Проект
├── DeathRoom.Infrastructure/        # (Зарезервировано для инфраструктурных реализаций)
│   └── DeathRoom.Infrastructure.csproj
├── DeathRoom.Tests/                 # Тесты
│   └── DeathRoom.Tests.csproj       # Проект тестов
├── Dockerfile                       # Docker образ
├── docker-compose.yml               # Docker Compose для разработки
├── docker-compose.prod.yml          # Docker Compose для production
├── startup.sh                       # Скрипт запуска
└── README.md                        # Документация
```

---

## Слои и их назначение

### Domain Layer
**Назначение:** Бизнес-сущности, value objects, доменная логика
- **Не зависит ни от чего** — чистая бизнес-логика
- **Содержит:** Player, PlayerState, WorldState, Vector3, Match
- **Принципы:** Инкапсуляция, неизменяемость value objects

### Application Layer
**Назначение:** Сервисы, координация доменной логики
- **Зависит только от Domain** — не знает об инфраструктуре
- **Содержит:** GameLoopService, PacketHandlerService, PlayerSessionService
- **Принципы:** Оркестрация, координация, бизнес-процессы

### Backend Layer
**Назначение:** Точка входа, DI, запуск, сетевая интеграция
- **Зависит от Application и Common** — только делегирует события
- **Содержит:** Program.cs, ServerRunner, GameServer
- **Принципы:** Тонкий слой, только запуск и сетевая интеграция

### Common Layer
**Назначение:** DTO, пакеты, сериализация
- **Независимый слой** — используется всеми остальными
- **Содержит:** Сетевые пакеты, DTO, интерфейсы
- **Принципы:** Передача данных, контракты

### Infrastructure Layer (зарезервировано)
**Назначение:** Внешние зависимости (БД, файловая система)
- **Будет содержать:** Репозитории, внешние сервисы
- **Принципы:** Адаптеры к внешним системам

---

## Запуск сервера

### Требования

- **.NET 8.0** или выше
- **Linux/macOS/Windows** — кроссплатформенность
- **Порт 9050** — должен быть свободен (или изменить в коде)
- **Минимум 512MB RAM** — для in-memory хранения
- **Сеть** — для подключения клиентов

### Быстрый старт

```bash
# Клонирование репозитория (если нужно)
git clone <repository-url>
cd DeathRoom

# Запуск сервера
dotnet run --project server/DeathRoom-Backend/DeathRoom-Backend.csproj
```

После успешного запуска вы увидите:
```
DeathRoom сервер стартует...
[ServerRunner] Конструктор вызван
[ServerRunner] StartAsync: запуск сервера
[GameServer] Start: запуск NetManager и игрового цикла
[GameLoopService] RunAsync: старт игрового цикла
```

### Запуск через .NET CLI

#### Разработка
```bash
# Переход в папку проекта
cd /path/to/DeathRoom

# Запуск в режиме разработки
dotnet run --project server/DeathRoom-Backend/DeathRoom-Backend.csproj

# Запуск с дополнительными параметрами
dotnet run --project server/DeathRoom-Backend/DeathRoom-Backend.csproj --environment Development
```

#### Production
```bash
# Сборка release версии
dotnet build --configuration Release --project server/DeathRoom-Backend/DeathRoom-Backend.csproj

# Запуск release версии
dotnet run --configuration Release --project server/DeathRoom-Backend/DeathRoom-Backend.csproj
```

#### С переменными окружения
```bash
# Настройка производительности
export DEATHROOM_BROADCAST_INTERVAL_MS=10
export DEATHROOM_IDLE_INTERVAL_MS=50
export DEATHROOM_WORLDSTATE_HISTORY_LENGTH=50

# Запуск с настройками
dotnet run --project server/DeathRoom-Backend/DeathRoom-Backend.csproj
```

### Запуск через Docker

#### Разработка
```bash
# Сборка образа
docker build -t deathroom-server:dev ./server

# Запуск контейнера
docker run -p 9050:9050 --name deathroom-dev deathroom-server:dev

# Запуск с переменными окружения
docker run -p 9050:9050 \
  -e DEATHROOM_BROADCAST_INTERVAL_MS=10 \
  -e DEATHROOM_IDLE_INTERVAL_MS=50 \
  --name deathroom-dev deathroom-server:dev
```

#### Production
```bash
# Использование docker-compose
docker-compose -f docker-compose.prod.yml up -d

# Или напрямую
docker run -d --restart unless-stopped \
  -p 9050:9050 \
  --name deathroom-prod \
  deathroom-server:latest
```

### Диагностика запуска

#### Проверка статуса сервера
```bash
# Проверка процесса
ps aux | grep DeathRoom-Backend

# Проверка портов
netstat -tuln | grep 9050
# или
ss -tuln | grep 9050

# Проверка логов
journalctl -u deathroom -f  # для systemd
# или
tail -f /var/log/deathroom/server.log
```

#### Проверка подключений
```bash
# Тест подключения к серверу
telnet localhost 9050
# или
nc -zv localhost 9050
```

#### Мониторинг производительности
```bash
# Мониторинг процесса
htop -p $(pgrep -f DeathRoom-Backend)

# Мониторинг сети
iftop -i lo  # для локального трафика
```

### Остановка сервера

#### Graceful shutdown
```bash
# Через Ctrl+C в консоли
# или
kill $(pgrep -f DeathRoom-Backend)

# Для systemd
sudo systemctl stop deathroom
```

#### Принудительная остановка
```bash
# Если сервер завис
kill -9 $(pgrep -f DeathRoom-Backend)

# Для Docker
docker stop deathroom-prod
docker kill deathroom-prod  # принудительно
```

---

## Конфигурация

### Переменные окружения

| Переменная | Описание | По умолчанию | Рекомендации |
|------------|----------|--------------|--------------|
| `DEATHROOM_BROADCAST_INTERVAL_MS` | Интервал рассылки состояния мира (мс) | 15 | 10-20 для быстрых игр |
| `DEATHROOM_IDLE_INTERVAL_MS` | Интервал ожидания при отсутствии игроков (мс) | 100 | 50-200 для экономии ресурсов |
| `DEATHROOM_WORLDSTATE_HISTORY_LENGTH` | Длина истории состояний мира | 20 | 10-50 в зависимости от памяти |
| `DEATHROOM_WORLDSTATE_SAVE_INTERVAL` | Интервал сохранения состояния мира | 10 | 5-20 для точности |

#### Примеры настройки

**Высокая производительность (быстрая игра):**
```bash
export DEATHROOM_BROADCAST_INTERVAL_MS=10
export DEATHROOM_IDLE_INTERVAL_MS=50
export DEATHROOM_WORLDSTATE_HISTORY_LENGTH=30
export DEATHROOM_WORLDSTATE_SAVE_INTERVAL=5
```

**Экономия ресурсов (медленная игра):**
```bash
export DEATHROOM_BROADCAST_INTERVAL_MS=30
export DEATHROOM_IDLE_INTERVAL_MS=200
export DEATHROOM_WORLDSTATE_HISTORY_LENGTH=10
export DEATHROOM_WORLDSTATE_SAVE_INTERVAL=20
```

### Файл конфигурации

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=deathroom;Username=postgres;Password=aboba"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## Основные компоненты

### Backend (DeathRoom-Backend)

- **Program.cs** — точка входа, настройка DI, запуск Host.
- **ServerRunner.cs** — HostedService, управляет жизненным циклом сервера.
- **GameServer.cs** — сетевой сервер, интеграция с LiteNetLib, делегирование событий в Application.

### Application

- **GameLoopService.cs** — основной игровой цикл, рассылка состояния мира.
- **PacketHandlerService.cs** — обработка входящих сетевых пакетов.
- **PlayerSessionService.cs** — управление сессиями игроков.
- **WorldStateService.cs** — хранение истории состояний мира.
- **HitRegistrationService.cs** — регистрация урона и смерти.
- **HitPhysicsService.cs** — проверка попаданий (физика).

### Domain

- **Player.cs, PlayerState.cs, PlayerSnapshot.cs** — сущности игрока и его состояния.
- **WorldState.cs** — состояние игрового мира.
- **Match.cs, MatchPlayer.cs** — сущности матча.
- **Vector3.cs** — value object для координат.

### Common

- **dto/** — DTO для передачи состояния (PlayerState, Vector3Serializable и др.).
- **network/** — сетевые пакеты (LoginPacket, PlayerMovePacket, PlayerHitPacket, WorldStatePacket и др.), интерфейс IPacket.

---

## Сетевые пакеты и DTO

- **LoginPacket** — авторизация игрока.
- **PlayerMovePacket** — движение игрока.
- **PlayerHitPacket** — попадание по игроку.
- **PickUpArmorPacket** — поднятие брони.
- **WorldStatePacket** — рассылка состояния мира всем клиентам.
- **PlayerState, Vector3Serializable** — DTO для передачи состояния игрока и координат.

---

## Логирование и завершение работы

- Все ключевые этапы старта и завершения сервера логируются в консоль.
- Для гарантированного завершения процесса после shutdown используется `Environment.Exit(0)` в Program.cs.
- Для диагностики добавлены логи в жизненный цикл HostedService и игровых сервисов.

---

## Тестирование

- Тесты располагаются в папке `DeathRoom.Tests/`.
- Рекомендуется покрыть тестами бизнес-логику (Domain, Application).


---


#### Сервер не запускается
```bash
# Проверка зависимостей
dotnet --version
dotnet restore server/DeathRoom-Backend/DeathRoom-Backend.csproj

# Проверка порта
netstat -tuln | grep 9050
lsof -i :9050

# Проверка логов
dotnet run --project server/DeathRoom-Backend/DeathRoom-Backend.csproj --verbosity detailed
```

#### Сервер зависает при остановке
```bash
# Проверка процессов
ps aux | grep DeathRoom-Backend

# Принудительная остановка
kill -9 $(pgrep -f DeathRoom-Backend)

# Проверка потоков
ps -T -p $(pgrep -f DeathRoom-Backend)
```

#### Высокая загрузка CPU
```bash
# Мониторинг процесса
htop -p $(pgrep -f DeathRoom-Backend)

# Уменьшение частоты обновлений
export DEATHROOM_BROADCAST_INTERVAL_MS=30
```

#### Проблемы с сетью
```bash
# Проверка подключений
netstat -an | grep 9050

# Тест подключения
telnet localhost 9050

# Проверка firewall
sudo ufw status
```
