# Настройка окружения разработки

## Обзор

Данное руководство описывает процесс настройки окружения разработки для проекта DeathRoom, включая установку необходимых инструментов и настройку проекта.

## Требования к системе

### Минимальные требования
- **ОС**: Windows 10/11, macOS 10.15+, Linux (Ubuntu 20.04+)
- **Процессор**: Intel i3 / AMD Ryzen 3 или выше
- **Память**: 8GB RAM (рекомендуется 16GB)
- **Диск**: 10GB свободного места
- **Сеть**: Стабильное интернет-соединение

### Рекомендуемые требования
- **ОС**: Windows 11, macOS 12+, Ubuntu 22.04+
- **Процессор**: Intel i5 / AMD Ryzen 5 или выше
- **Память**: 16GB RAM
- **Диск**: SSD с 20GB свободного места
- **Видеокарта**: DirectX 11 совместимая

## Установка инструментов разработки

### 1. Git
**Назначение**: Система контроля версий
**Установка**:
```bash
# Windows (через Chocolatey)
choco install git

# macOS (через Homebrew)
brew install git

# Ubuntu/Debian
sudo apt update
sudo apt install git
```

**Настройка**:
```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### 2. .NET 8 SDK
**Назначение**: Разработка серверной части
**Установка**:
```bash
# Windows
# Скачать с https://dotnet.microsoft.com/download

# macOS
brew install dotnet

# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install dotnet-sdk-8.0
```

**Проверка**:
```bash
dotnet --version
# Должно показать 8.0.x
```

### 3. Unity 2022.3 LTS
**Назначение**: Разработка клиентской части
**Установка**:
1. Скачать Unity Hub с https://unity.com/download
2. Установить Unity 2022.3 LTS через Unity Hub
3. Добавить модули:
   - Microsoft Visual Studio Community
   - Universal Render Pipeline
   - TextMeshPro

**Настройка**:
- Открыть Unity Hub
- Добавить проект DeathRoom
- Настроить внешний редактор (Visual Studio/Rider)

### 4. IDE/Редактор кода

#### Visual Studio 2022 (рекомендуется)
**Установка**:
```bash
# Windows
# Скачать Community Edition с https://visualstudio.microsoft.com/

# Настройка workload:
# - .NET desktop development
# - Game development with Unity
# - ASP.NET and web development
```

#### Visual Studio Code
**Установка**:
```bash
# Скачать с https://code.visualstudio.com/
# Установить расширения:
# - C#
# - Unity
# - C# Dev Kit
# - .NET Install Tool
```

#### JetBrains Rider
**Установка**:
- Скачать с https://www.jetbrains.com/rider/
- Настроить Unity integration

### 5. Docker (опционально)
**Назначение**: Контейнеризация и развертывание
**Установка**:
```bash
# Windows/macOS
# Скачать Docker Desktop с https://www.docker.com/products/docker-desktop

# Ubuntu
sudo apt update
sudo apt install docker.io docker-compose
sudo usermod -aG docker $USER
```

## Клонирование проекта

### 1. Клонирование репозитория
```bash
git clone <repository-url>
cd DeathRoom
```

### 2. Проверка структуры
```bash
ls -la
# Должно показать:
# - client/
# - server/
# - docs/
# - README.md
```

### 3. Настройка Git hooks (опционально)
```bash
# Копирование pre-commit hooks
cp .git/hooks/pre-commit.sample .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

## Настройка серверной части

### 1. Восстановление зависимостей
```bash
cd server
dotnet restore
```

### 2. Сборка проекта
```bash
dotnet build
```

### 3. Запуск сервера
```bash
# Разработка
dotnet run --project DeathRoom-Backend/DeathRoom-Backend.csproj

# Production
dotnet run --configuration Release --project DeathRoom-Backend/DeathRoom-Backend.csproj
```

### 4. Проверка работы
```bash
# Проверка порта
netstat -an | grep 9050

# Проверка логов
# Должны появиться сообщения о запуске сервера
```

## Настройка клиентской части

### 1. Открытие проекта в Unity
1. Запустить Unity Hub
2. Добавить проект (папка client)
3. Открыть проект в Unity

### 2. Настройка сцены
1. Открыть сцену "MainMenu"
2. Проверить настройки в Client.cs:
   ```csharp
   public string serverAddress = "localhost"; // или IP сервера
   public int serverPort = 9050;
   ```

### 3. Сборка проекта
1. File → Build Settings
2. Выбрать платформу (Windows/macOS/Linux)
3. Настроить качество графики
4. Build

## Настройка окружения разработки

### 1. Переменные окружения
```bash
# Создать .env файл в корне проекта
DEATHROOM_BROADCAST_INTERVAL_MS=15
DEATHROOM_IDLE_INTERVAL_MS=100
DEATHROOM_WORLDSTATE_HISTORY_LENGTH=20
DEATHROOM_WORLDSTATE_SAVE_INTERVAL=10
```

### 2. Настройка IDE
#### Visual Studio
- Открыть DeathRoom.sln
- Настроить startup project (DeathRoom-Backend)
- Настроить debugging

#### Unity
- Настроить External Tools (Edit → Preferences)
- Выбрать Visual Studio как External Script Editor
- Настроить API Compatibility Level (.NET Standard 2.1)

### 3. Настройка отладки
```json
// .vscode/launch.json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch Server",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/server/DeathRoom-Backend/bin/Debug/net8.0/DeathRoom-Backend.dll",
            "args": [],
            "cwd": "${workspaceFolder}/server/DeathRoom-Backend",
            "console": "internalConsole",
            "stopAtEntry": false
        }
    ]
}
```

## Тестирование окружения

### 1. Тест сервера
```bash
cd server
dotnet test
```

### 2. Тест клиента
- Запустить Unity
- Открыть Test Runner (Window → General → Test Runner)
- Запустить все тесты

### 3. Интеграционный тест
1. Запустить сервер
2. Запустить клиент в Unity
3. Подключиться к серверу
4. Проверить синхронизацию

## Устранение неполадок

### Проблемы с .NET
```bash
# Очистка кэша
dotnet clean
dotnet restore

# Проверка версии
dotnet --list-sdks
dotnet --list-runtimes
```

### Проблемы с Unity
1. **Ошибки компиляции**:
   - Проверить версию .NET
   - Очистить Library папку
   - Reimport All Assets

2. **Проблемы с пакетами**:
   - Window → Package Manager
   - Обновить пакеты
   - Проверить зависимости

### Проблемы с сетью
```bash
# Проверка портов
netstat -an | grep 9050

# Проверка firewall
# Windows: Проверить Windows Defender
# Linux: sudo ufw status
```

### Проблемы с Docker
```bash
# Перезапуск Docker
sudo systemctl restart docker

# Очистка контейнеров
docker system prune -a
```

## Рекомендации по производительности

### Настройки Unity
- **Quality Settings**: Настроить для разработки
- **Resolution**: 1920x1080 для разработки
- **VSync**: Отключить для минимальной задержки
- **Anti-aliasing**: Отключить для производительности

### Настройки .NET
```json
// server/DeathRoom-Backend/DeathRoom-Backend.csproj
<PropertyGroup>
    <Optimize>true</Optimize>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

### Настройки IDE
- **IntelliSense**: Включить для Unity
- **Auto-save**: Настроить частоту сохранения
- **Git integration**: Настроить для автоматических коммитов

## Следующие шаги

### 1. Изучение документации
- Прочитать [README.md](../README.md)
- Изучить архитектуру в [docs/](..)
- Понять сетевые пакеты в [PACKETS.md](../../PACKETS.md)

### 2. Первые изменения
1. Изменить адрес сервера в Client.cs
2. Добавить логирование в GameLoopService
3. Протестировать подключение

### 3. Разработка
1. Создать feature branch
2. Внести изменения
3. Протестировать
4. Создать Pull Request

## Полезные команды

### Git
```bash
# Создание ветки
git checkout -b feature/new-feature

# Коммит изменений
git add .
git commit -m "Add new feature"

# Push изменений
git push origin feature/new-feature
```

### .NET
```bash
# Сборка
dotnet build

# Запуск
dotnet run

# Тесты
dotnet test

# Очистка
dotnet clean
```

### Unity
- **Play**: Ctrl+P
- **Pause**: Ctrl+Shift+P
- **Step**: Ctrl+Alt+P
- **Build**: Ctrl+Shift+B

### Docker
```bash
# Сборка образа
docker build -t deathroom-server .

# Запуск контейнера
docker run -p 9050:9050 deathroom-server

# Просмотр логов
docker logs <container-id>
``` 