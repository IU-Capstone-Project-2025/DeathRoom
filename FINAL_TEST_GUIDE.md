# 🎯 Финальное руководство по тестированию мультиплеера

## ✅ Исправления завершены!

Все скрипты теперь используют единый тип `Vector3Serializable` для синхронизации между клиентом и сервером.

### 🔧 Исправленные файлы:

#### Клиент:

- ✅ `Vector3Serializable.cs` - добавлены все необходимые операторы и методы
- ✅ `PlayerMovePacket.cs` - использует Vector3Serializable
- ✅ `PlayerShootPacket.cs` - использует Vector3Serializable
- ✅ `PlayerHitPacket.cs` - использует Vector3Serializable
- ✅ `WorldStatePacket.cs` - использует Vector3Serializable
- ✅ `NetworkDebugger.cs` - обновлен для тестирования
- ✅ `Client.cs` - отправляет Vector3Serializable пакеты

#### Сервер:

- ✅ `Vector3Serializable.cs` - полная совместимость с клиентом
- ✅ `GameServer.cs` - улучшенная обработка ошибок и логирование
- ✅ Все пакеты используют Vector3Serializable

## 🚀 Порядок тестирования:

### Шаг 1: Проверка сериализации в Unity

1. Запустите игру в Unity Editor
2. Нажмите клавишу **P** для тестирования пакетов
3. Должны появиться логи:
   ```
   === Testing Packet Serialization ===
   ✅ LoginPacket test passed: TestPlayer
   ✅ PlayerMovePacket test passed: pos(1.00, 2.00, 3.00)
   ✅ PlayerShootPacket test passed: dir(0.00, 0.00, 1.00)
   === All serialization tests passed! ===
   ```

### Шаг 2: Запуск и проверка сервера

1. Сервер должен запуститься без ошибок:
   ```bash
   cd server/DeathRoom-Backend
   dotnet run
   ```
2. Ожидаемый вывод:
   ```
   Server started on port 9050
   info: Microsoft.Hosting.Lifetime[0]
         Application started. Press Ctrl+C to shut down.
   ```

### Шаг 3: Тестирование подключения в Unity

1. В Unity нажмите клавишу **T** для подключения
2. Проверьте логи сервера:
   ```
   Peer connected: [port]. Waiting for login.
   Received packet from [port], size: 23 bytes
   Deserialized packet type: LoginPacket
   Player [name] logged in from [port]
   ```
3. **Игрок НЕ должен отключаться!**

### Шаг 4: Сборка и тестирование двух клиентов

1. **Build клиент** в Unity (File → Build Settings → Build)
2. **Запустите первый экземпляр** билда
3. **Запустите второй экземпляр** билда
4. В каждом установите уникальные имена:
   - Клиент 1: `Player1`
   - Клиент 2: `Player2`
5. Подключитесь к серверу в обоих

## 📊 Ожидаемые результаты:

### ✅ Успешное подключение двух игроков:

**Логи сервера:**

```
Peer connected: 52341. Waiting for login.
Received packet from 52341, size: 23 bytes
Deserialized packet type: LoginPacket
Player Player1 logged in from 52341

Peer connected: 53422. Waiting for login.
Received packet from 53422, size: 23 bytes
Deserialized packet type: LoginPacket
Player Player2 logged in from 53422

Received packet from 52341, size: 32 bytes
Deserialized packet type: PlayerMovePacket
Received packet from 53422, size: 32 bytes
Deserialized packet type: PlayerMovePacket
```

**Логи клиента 1:**

```
Connected to server
Processing WorldStatePacket with 2 players
My player name: Player1, My ID: 1
Player in packet: Player1 (ID: 1) at position 0.00, 0.00, 0.00
Player in packet: Player2 (ID: 2) at position 0.00, 0.00, 0.00
Set local player ID to: 1
Skipping local player Player1 (ID: 1)
NetworkPlayer initialized: Player2 (ID: 2) at (0.00, 0.00, 0.00)
```

**Логи клиента 2:**

```
Connected to server
Processing WorldStatePacket with 2 players
My player name: Player2, My ID: 2
Player in packet: Player1 (ID: 1) at position 0.00, 0.00, 0.00
Player in packet: Player2 (ID: 2) at position 0.00, 0.00, 0.00
Set local player ID to: 2
NetworkPlayer initialized: Player1 (ID: 1) at (0.00, 0.00, 0.00)
Skipping local player Player2 (ID: 2)
```

## 🎮 В игре должно быть видно:

1. **Клиент 1:**

   - Свой локальный игрок (управляемый)
   - NetworkPlayer "Player2" (синхронизируется с сервера)

2. **Клиент 2:**

   - Свой локальный игрок (управляемый)
   - NetworkPlayer "Player1" (синхронизируется с сервера)

3. **Движения игроков должны синхронизироваться** между клиентами

## ❌ Если проблемы остались:

### Проблема: Ошибки сериализации

**Решение:** Убедитесь что все файлы обновлены и используют Vector3Serializable

### Проблема: Игроки отключаются

**Решение:** Проверьте логи сервера на наличие "ERROR processing packet"

### Проблема: Игроки не видят друг друга

**Решение:** Убедитесь что имена уникальны и localPlayerId устанавливается

## 🔧 Компоненты Unity для клиента:

### NetworkManager (GameObject):

- ✅ Client.cs
- ✅ MultiplayerManager.cs
- ✅ NetworkDebugger.cs (для тестирования)

### LocalPlayer (Prefab):

- ✅ CharacterController
- ✅ Animator
- ✅ PlayerMovement.cs
- ✅ Playerhealth.cs
- ✅ Camera (дочерний)
- ✅ AudioListener (дочерний)

### NetworkPlayer (Prefab):

- ✅ Animator (тот же контроллер что у LocalPlayer)
- ✅ NetworkPlayer.cs
- ❌ БЕЗ CharacterController, PlayerMovement, Camera, AudioListener

Теперь ваш мультиплеер должен работать корректно! 🎉
