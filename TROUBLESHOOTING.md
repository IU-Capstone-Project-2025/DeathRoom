# 🔧 Руководство по устранению неполадок мультиплеера

## 🚨 Исправленная критическая ошибка

### Проблема: Игроки сразу отключаются после подключения

**Причина:** Несоответствие типов Vector3 между клиентом и сервером

- Клиент использовал: `Vector3Serializable`
- Сервер ожидал: `Vector3`

**Решение:** ✅ **ИСПРАВЛЕНО**

- Переименовали `Vector3Serializable` → `Vector3`
- Обновили все пакеты для использования единого типа
- Добавили улучшенную обработку ошибок на сервере

## 🧪 Тестирование исправлений

### 1. Добавьте NetworkDebugger

Добавьте компонент `NetworkDebugger` на любой объект в сцене для:

- ✅ Тестирования сериализации пакетов (клавиша P)
- ✅ Проверки соединения (клавиша T)
- ✅ Отображения статуса подключения

### 2. Проверьте логи сервера

После перезапуска сервер должен показывать:

```
Received packet from [port], size: [bytes] bytes
Deserialized packet type: LoginPacket
Player [name] logged in from [port]
```

### 3. Проверьте логи клиента

В Unity Console должны появиться:

```
=== Testing Packet Serialization ===
✅ LoginPacket test passed: TestPlayer
✅ PlayerMovePacket test passed: pos(1.00, 2.00, 3.00)
✅ PlayerShootPacket test passed: dir(0.00, 0.00, 1.00)
=== All serialization tests passed! ===
```

## 📝 Пошаговая диагностика

### Шаг 1: Проверка сериализации

1. Запустите игру в редакторе Unity
2. Нажмите клавишу **P** для тестирования пакетов
3. Убедитесь что все тесты прошли успешно

### Шаг 2: Проверка сервера

1. Запустите сервер: `cd server/DeathRoom-Backend && dotnet run`
2. Убедитесь что видите: `Server started on port 9050`
3. Сервер не должен показывать ошибки

### Шаг 3: Тестирование подключения

1. В Unity нажмите клавишу **T** для тестирования подключения
2. Проверьте логи сервера - должно появиться:
   ```
   Peer connected: [port]. Waiting for login.
   Received packet from [port], size: [bytes] bytes
   Deserialized packet type: LoginPacket
   Player [name] logged in from [port]
   ```
3. Игрок НЕ должен сразу отключаться

### Шаг 4: Двойное подключение

1. Соберите билд клиента
2. Запустите первый экземпляр
3. Запустите второй экземпляр
4. В каждом установите уникальное имя игрока
5. Подключитесь к серверу в обоих клиентах

## ❗ Ожидаемые результаты

### ✅ Успешное подключение:

```
Server logs:
Peer connected: 52341. Waiting for login.
Received packet from 52341, size: 23 bytes
Deserialized packet type: LoginPacket
Player Player1 logged in from 52341
Received packet from 52341, size: 32 bytes
Deserialized packet type: PlayerMovePacket

Client logs:
Connected to server: [NetPeer]
Processing WorldStatePacket with 1 players
My player name: Player1, My ID: 1
Player in packet: Player1 (ID: 1) at position 0, 0, 0
Set local player ID to: 1
```

### ❌ Если всё ещё есть проблемы:

1. **"Unknown packet type received"**

   - Проверьте что все пакеты используют правильный Vector3
   - Убедитесь в правильности namespace

2. **"ERROR processing packet"**

   - Проверьте логи сервера для деталей ошибки
   - Возможно нужно перестроить проект

3. **Игроки не видят друг друга**
   - Убедитесь что имена игроков уникальны
   - Проверьте что NetworkPlayer создаётся

## 🔄 Быстрое восстановление

Если что-то сломалось:

1. **Перезапустите сервер:**

   ```bash
   cd server/DeathRoom-Backend
   dotnet clean
   dotnet run
   ```

2. **Пересоберите клиент:**

   - File → Build Settings → Build
   - Убедитесь что все скрипты компилируются без ошибок

3. **Проверьте префабы:**
   - LocalPlayer должен иметь все компоненты из UNITY_SETUP_GUIDE.md
   - NetworkPlayer должен иметь только Animator и NetworkPlayer.cs

## 📞 Дополнительная отладка

### Включите подробные логи:

В Client.cs временно добавьте в Update():

```csharp
if (Time.frameCount % 300 == 0) // каждые 5 секунд
{
    Debug.Log($"Client status: Connected={isConnected}, LocalPlayer={localPlayer != null}, NetworkPlayers={networkPlayers.Count}");
}
```

### Мониторинг сервера:

Сервер теперь показывает:

- Размер получаемых пакетов
- Тип десериализованных пакетов
- Подробные ошибки с stack trace

Это поможет быстро найти оставшиеся проблемы!
