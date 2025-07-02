# ⚡ Быстрая настройка мультиплеера

## 📦 Основные компоненты

### 1. NetworkManager (GameObject)

```
Компоненты:
✅ Client.cs
✅ MultiplayerManager.cs

Настройки Client.cs:
- Server Address: 127.0.0.1
- Server Port: 9050
- Local Player Prefab: [ваш локальный префаб]
- Network Player Prefab: [ваш сетевой префаб]
- Spawn Points: [массив точек спавна]
```

### 2. LocalPlayer (Prefab)

```
Обязательные компоненты:
✅ CharacterController
✅ Animator (с параметрами MoveX, MoveZ, Sprint, etc.)
✅ PlayerMovement.cs
✅ Playerhealth.cs
✅ Camera (дочерний объект)
✅ AudioListener (дочерний объект)
```

### 3. NetworkPlayer (Prefab)

```
Обязательные компоненты:
✅ Animator (тот же что у LocalPlayer)
✅ NetworkPlayer.cs

❌ НЕ добавляйте:
- CharacterController
- PlayerMovement
- Camera
- AudioListener
```

### 4. Точки спавна (GameObjects)

```
Создайте 2-3 пустых GameObject:
- SpawnPoint1
- SpawnPoint2
- SpawnPoint3

Добавьте их в массив Spawn Points в Client.cs
```

## 🚀 Проверочный список

1. [ ] Сервер запущен (`cd server/DeathRoom-Backend && dotnet run`)
2. [ ] NetworkManager с Client.cs и MultiplayerManager.cs
3. [ ] LocalPlayer префаб с полным набором компонентов
4. [ ] NetworkPlayer префаб только с NetworkPlayer.cs и Animator
5. [ ] Префабы привязаны к Client.cs
6. [ ] Точки спавна созданы и привязаны
7. [ ] Билд собран и готов к тестированию

## 🔧 Быстрое тестирование

1. Запустите 2 экземпляра билда
2. В каждом установите уникальное имя
3. Подключитесь к серверу
4. Проверьте что игроки видят друг друга

## ❗ Частые проблемы

**"No localPlayerPrefab assigned!"** → Привяжите LocalPlayer префаб
**"No networkPlayerPrefab assigned!"** → Привяжите NetworkPlayer префаб  
**Игроки не видят друг друга** → Проверьте уникальность имен
**Дублирование игроков** → GameManager должен быть отключен в сетевом режиме
