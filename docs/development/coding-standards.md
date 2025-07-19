# Стандарты кодирования DeathRoom

## Обзор

Данный документ описывает стандарты кодирования для проекта DeathRoom, обеспечивающие единообразие кода, читаемость и поддерживаемость.

## Принципы

### 1. Читаемость превыше всего
- Код должен быть понятен другим разработчикам
- Имена должны быть описательными
- Комментарии для сложной логики

### 2. Единообразие
- Следование установленным соглашениям
- Использование единого стиля форматирования
- Консистентная архитектура

### 3. Простота
- Избегать излишней сложности
- Принцип KISS (Keep It Simple, Stupid)
- Понятная структура кода

## Соглашения по именованию

### C# (Сервер и клиент)

#### Классы и интерфейсы
```csharp
// ✅ Правильно
public class PlayerMovement
public interface IPacket
public class GameLoopService

// ❌ Неправильно
public class playerMovement
public interface packet
public class gameloopservice
```

#### Методы
```csharp
// ✅ Правильно
public void HandlePlayerConnected(NetPeer peer)
public bool ValidateHit(Player attacker, Player target)
private void OnNetworkReceive(NetPeer peer, NetPacketReader reader)

// ❌ Неправильно
public void handle_player_connected(NetPeer peer)
public bool validateHit(Player attacker, Player target)
private void onNetworkReceive(NetPeer peer, NetPacketReader reader)
```

#### Переменные и поля
```csharp
// ✅ Правильно
private readonly ILogger<GameServer> _logger;
private NetManager _netManager;
public string ServerAddress { get; set; }

// ❌ Неправильно
private readonly ILogger<GameServer> logger;
private NetManager netManager;
public string serverAddress { get; set; }
```

#### Константы
```csharp
// ✅ Правильно
public const int MaxPlayers = 100;
public const float MaxHitDistance = 50.0f;
private const string ConnectionKey = "DeathRoomSecret";

// ❌ Неправильно
public const int MAX_PLAYERS = 100;
public const float maxHitDistance = 50.0f;
private const string connection_key = "DeathRoomSecret";
```

### Unity (Клиент)

#### MonoBehaviour классы
```csharp
// ✅ Правильно
public class PlayerMovement : MonoBehaviour
public class NetworkPlayer : MonoBehaviour
public class GameUI : MonoBehaviour

// ❌ Неправильно
public class playerMovement : MonoBehaviour
public class networkPlayer : MonoBehaviour
public class gameUI : MonoBehaviour
```

#### Unity события
```csharp
// ✅ Правильно
private void Start()
private void Update()
private void OnDestroy()
private void OnTriggerEnter(Collider other)

// ❌ Неправильно
private void start()
private void update()
private void onDestroy()
private void onTriggerEnter(Collider other)
```

## Структура файлов

### Организация using директив
```csharp
// Системные using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Сторонние библиотеки
using LiteNetLib;
using MessagePack;

// Проектные using
using DeathRoom.Domain;
using DeathRoom.Common;

// Unity using (только в клиенте)
using UnityEngine;
using UnityEngine.UI;
```

### Структура класса
```csharp
public class ExampleClass
{
    // 1. Константы
    private const int DefaultValue = 100;
    
    // 2. Поля
    private readonly ILogger<ExampleClass> _logger;
    private NetManager _netManager;
    
    // 3. Свойства
    public string Name { get; set; }
    public int Count { get; private set; }
    
    // 4. Конструктор
    public ExampleClass(ILogger<ExampleClass> logger)
    {
        _logger = logger;
    }
    
    // 5. Публичные методы
    public void DoSomething()
    {
        // Реализация
    }
    
    // 6. Приватные методы
    private void InternalMethod()
    {
        // Реализация
    }
    
    // 7. События (если есть)
    public event Action<string> OnDataReceived;
}
```

## Форматирование

### Отступы и пробелы
```csharp
// ✅ Правильно
public class Example
{
    public void Method(int parameter)
    {
        if (parameter > 0)
        {
            DoSomething();
        }
    }
}

// ❌ Неправильно
public class Example {
    public void Method(int parameter) {
        if (parameter > 0) {
            DoSomething();
        }
    }
}
```

### Длина строк
- **Максимальная длина**: 120 символов
- **Рекомендуемая длина**: 80-100 символов
- **Перенос длинных строк**: По логическим границам

```csharp
// ✅ Правильно
var playerState = new PlayerState
{
    Id = playerId,
    Username = username,
    Position = new Vector3Serializable(position.x, position.y, position.z)
};

// ❌ Неправильно
var playerState = new PlayerState { Id = playerId, Username = username, Position = new Vector3Serializable(position.x, position.y, position.z) };
```

### Пустые строки
```csharp
// ✅ Правильно
public class Example
{
    public void Method1()
    {
        // Реализация
    }

    public void Method2()
    {
        // Реализация
    }
}
```

## Комментарии

### Документационные комментарии
```csharp
/// <summary>
/// Обрабатывает подключение нового игрока к серверу.
/// </summary>
/// <param name="peer">Сетевой пир игрока</param>
/// <param name="loginPacket">Пакет авторизации</param>
/// <returns>Результат обработки подключения</returns>
public bool HandlePlayerLogin(NetPeer peer, LoginPacket loginPacket)
{
    // Реализация
}
```

### Комментарии к коду
```csharp
// ✅ Правильно
// Проверяем валидность движения игрока
if (!IsValidMovement(newPosition))
{
    return;
}

// Применяем урон с учетом брони
var damageToHealth = damage;
if (currentArmor > 0)
{
    var damageToArmor = damage * armorDamageReduction;
    damageToHealth = damage * (1 - armorDamageReduction);
    currentArmor -= damageToArmor;
}

// ❌ Неправильно
// проверка
if (!IsValidMovement(newPosition))
{
    return;
}

// урон
var damageToHealth = damage;
```

## Архитектурные принципы

### SOLID принципы

#### Single Responsibility Principle (SRP)
```csharp
// ✅ Правильно
public class PlayerMovement
{
    public void Move(Vector3 direction) { }
}

public class PlayerHealth
{
    public void TakeDamage(int damage) { }
}

// ❌ Неправильно
public class Player
{
    public void Move(Vector3 direction) { }
    public void TakeDamage(int damage) { }
    public void Shoot() { }
    public void PickUpItem() { }
}
```

#### Open/Closed Principle (OCP)
```csharp
// ✅ Правильно
public interface IPacketHandler
{
    void HandlePacket(IPacket packet);
}

public class LoginPacketHandler : IPacketHandler
{
    public void HandlePacket(IPacket packet) { }
}

public class MovePacketHandler : IPacketHandler
{
    public void HandlePacket(IPacket packet) { }
}
```

#### Dependency Inversion Principle (DIP)
```csharp
// ✅ Правильно
public class GameServer
{
    private readonly IPacketHandlerService _packetHandler;
    
    public GameServer(IPacketHandlerService packetHandler)
    {
        _packetHandler = packetHandler;
    }
}

// ❌ Неправильно
public class GameServer
{
    private readonly PacketHandlerService _packetHandler;
    
    public GameServer()
    {
        _packetHandler = new PacketHandlerService();
    }
}
```

### Clean Architecture

#### Слои архитектуры
```csharp
// Domain Layer
public class Player
{
    public int Id { get; set; }
    public string Username { get; set; }
    public PlayerState State { get; set; }
}

// Application Layer
public class PlayerService
{
    private readonly IPlayerRepository _repository;
    
    public PlayerService(IPlayerRepository repository)
    {
        _repository = repository;
    }
}

// Infrastructure Layer
public class PlayerRepository : IPlayerRepository
{
    public Player GetById(int id) { }
}
```

## Обработка ошибок

### Исключения
```csharp
// ✅ Правильно
public void ProcessPacket(byte[] data)
{
    try
    {
        var packet = MessagePackSerializer.Deserialize<IPacket>(data);
        HandlePacket(packet);
    }
    catch (MessagePackSerializationException ex)
    {
        _logger.LogError($"Failed to deserialize packet: {ex.Message}");
        // Обработка ошибки сериализации
    }
    catch (Exception ex)
    {
        _logger.LogError($"Unexpected error: {ex.Message}");
        // Общая обработка ошибок
    }
}

// ❌ Неправильно
public void ProcessPacket(byte[] data)
{
    var packet = MessagePackSerializer.Deserialize<IPacket>(data);
    HandlePacket(packet);
}
```

### Логирование
```csharp
// ✅ Правильно
_logger.LogInformation($"Player {player.Username} connected (ID: {player.Id})");
_logger.LogWarning($"High ping detected for player {player.Username}: {ping}ms");
_logger.LogError($"Failed to handle packet: {ex.Message}");

// ❌ Неправильно
Console.WriteLine($"Player {player.Username} connected");
Debug.Log($"High ping: {ping}ms");
```

## Производительность

### Оптимизация памяти
```csharp
// ✅ Правильно
// Использование object pooling
private readonly Queue<PlayerState> _playerStatePool = new();

public PlayerState GetPlayerState()
{
    return _playerStatePool.Count > 0 
        ? _playerStatePool.Dequeue() 
        : new PlayerState();
}

public void ReturnPlayerState(PlayerState state)
{
    _playerStatePool.Enqueue(state);
}

// ❌ Неправильно
public PlayerState GetPlayerState()
{
    return new PlayerState(); // Создание нового объекта каждый раз
}
```

### Асинхронное программирование
```csharp
// ✅ Правильно
public async Task ProcessAsync()
{
    await Task.Delay(100);
    // Асинхронная обработка
}

public async Task<Player> GetPlayerAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

// ❌ Неправильно
public void Process()
{
    Thread.Sleep(100); // Блокирующий вызов
}
```

## Тестирование

### Unit тесты
```csharp
[Test]
public void TestPlayerDamage_WithArmor_ShouldReduceArmorFirst()
{
    // Arrange
    var player = new Player { Id = 1, Username = "TestPlayer" };
    player.PlayerState.HealthPoint = 100;
    player.PlayerState.ArmorPoint = 50;
    
    // Act
    var isKill = player.TakeDamage(30, 1000);
    
    // Assert
    Assert.IsFalse(isKill);
    Assert.AreEqual(70, player.PlayerState.HealthPoint);
    Assert.AreEqual(20, player.PlayerState.ArmorPoint);
}
```

### Интеграционные тесты
```csharp
[Test]
public async Task TestPlayerConnection_ShouldCreateSession()
{
    // Arrange
    var peer = CreateMockPeer();
    var loginPacket = new LoginPacket { Username = "TestPlayer" };
    
    // Act
    var result = await _playerSessionService.HandleLoginAsync(peer, loginPacket);
    
    // Assert
    Assert.IsTrue(result);
    Assert.IsNotNull(_playerSessionService.GetPlayer(peer.Id));
}
```

## Безопасность

### Валидация входных данных
```csharp
// ✅ Правильно
public void HandlePlayerMove(PlayerMovePacket packet)
{
    // Валидация координат
    if (packet.Position.X < -1000 || packet.Position.X > 1000)
    {
        _logger.LogWarning($"Invalid position X: {packet.Position.X}");
        return;
    }
    
    // Валидация скорости движения
    var distance = Vector3.Distance(_lastPosition, packet.Position);
    if (distance > MaxMoveDistance)
    {
        _logger.LogWarning($"Player moved too fast: {distance}");
        return;
    }
    
    // Обработка валидного движения
    ProcessMovement(packet);
}

// ❌ Неправильно
public void HandlePlayerMove(PlayerMovePacket packet)
{
    // Отсутствие валидации
    ProcessMovement(packet);
}
```

### Защита от инъекций
```csharp
// ✅ Правильно
public void HandleLogin(LoginPacket packet)
{
    // Валидация имени пользователя
    if (string.IsNullOrWhiteSpace(packet.Username) || 
        packet.Username.Length > MaxUsernameLength)
    {
        return;
    }
    
    // Санитизация данных
    var sanitizedUsername = SanitizeUsername(packet.Username);
    
    // Обработка
    ProcessLogin(sanitizedUsername);
}

// ❌ Неправильно
public void HandleLogin(LoginPacket packet)
{
    // Отсутствие валидации и санитизации
    ProcessLogin(packet.Username);
}
```

## Инструменты

### Анализаторы кода
```xml
<!-- .editorconfig -->
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# правила
dotnet_diagnostic.CA1001.severity = warning
dotnet_diagnostic.CA1002.severity = warning
dotnet_diagnostic.CA1005.severity = warning
```

### Автоматическое форматирование
```json
// .vscode/settings.json
{
    "editor.formatOnSave": true,
    "editor.formatOnPaste": true,
    "editor.codeActionsOnSave": {
        "source.fixAll": true
    },
    "csharp.format.enable": true,
    "csharp.format.newLines.braces": "endOfLine",
    "csharp.format.newLines.elseOnNewLine": true,
    "csharp.format.newLines.catchOnNewLine": true,
    "csharp.format.newLines.finallyOnNewLine": true
}
```

## Рекомендации

### Для новых разработчиков
1. **Изучите существующий код**: Поняйте стиль проекта
2. **Следуйте примерам**: Используйте похожие паттерны
3. **Задавайте вопросы**: Уточняйте непонятные моменты
4. **Пишите тесты**: Обеспечивайте качество кода

### Для опытных разработчиков
1. **Менторство**: Помогайте новым разработчикам
2. **Рефакторинг**: Улучшайте существующий код
3. **Документирование**: Обновляйте стандарты
4. **Автоматизация**: Настройте инструменты

### Для архитекторов
1. **Обзор кода**: Проверяйте соответствие стандартам
2. **Обучение команды**: Проводите code review
3. **Эволюция стандартов**: Адаптируйте под потребности
4. **Инструменты**: Внедряйте автоматизацию

## Заключение

Следование данным стандартам обеспечивает:
- **Единообразие**: Понятный и предсказуемый код
- **Читаемость**: Легкое понимание логики
- **Поддерживаемость**: Простота внесения изменений
- **Качество**: Меньше ошибок и багов
- **Командную работу**: Эффективное сотрудничество 