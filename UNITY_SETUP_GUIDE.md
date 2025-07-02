# Unity Setup Guide - Настройка объектов для мультиплеера

## 🎯 Основные объекты сцены

### 1. **NetworkManager (пустой GameObject)**

Создайте пустой GameObject с именем "NetworkManager" и добавьте:

#### Обязательные компоненты:

- **Client.cs** - основной сетевой клиент
- **MultiplayerManager.cs** - управление UI и состоянием

#### Настройки Client.cs:

```
Server Address: 127.0.0.1
Server Port: 9050
Player Name: (оставьте пустым - автоматически сгенерируется)
Local Player Prefab: [перетащите префаб локального игрока]
Network Player Prefab: [перетащите префаб сетевого игрока]
Spawn Points: [массив точек спавна]
```

### 2. **UI Manager (пустой GameObject)**

Создайте пустой GameObject с именем "UI Manager" и добавьте:

#### Опциональные компоненты:

- **NetworkTestUI.cs** - UI для тестирования (рекомендуется)

#### Настройки NetworkTestUI.cs:

Привяжите UI элементы:

- Player Name Input: TMP_InputField для ввода имени
- Server Address Input: TMP_InputField для IP адреса
- Server Port Input: TMP_InputField для порта
- Connect Button: Button для подключения
- Disconnect Button: Button для отключения
- Status Text: TextMeshProUGUI для статуса
- Players List Text: TextMeshProUGUI для списка игроков

### 3. **Spawn Points (пустые GameObjects)**

Создайте несколько пустых GameObjects для точек спавна:

- SpawnPoint1 (позиция: например, 0, 0, 0)
- SpawnPoint2 (позиция: например, 10, 0, 0)
- SpawnPoint3 (позиция: например, -10, 0, 0)

## 🎭 Префабы игроков

### 1. **Local Player Prefab**

Создайте префаб локального игрока со следующими компонентами:

#### Основные компоненты:

- **Transform** - позиция, поворот, масштаб
- **CharacterController** - для движения (обязательно!)
- **Animator** - для анимаций (обязательно!)
- **PlayerMovement.cs** - управление движением
- **Playerhealth.cs** - система здоровья
- **PlayerCameraSystem.cs** - система камеры (если есть)
- **FootIK.cs** - IK для ног (опционально)

#### Иерархия объекта:

```
LocalPlayer (корневой объект)
├── Model (3D модель с Animator)
├── CameraPack (пустой объект для камеры)
│   └── MainCamera (Camera + AudioListener)
├── WeaponHolder (для оружия)
└── UI Canvas (UI элементы игрока)
```

#### Настройки Animator:

Должен содержать параметры:

- MoveX (float) - движение по X
- MoveZ (float) - движение по Z
- Sprint (bool) - бег
- Aim (bool) - прицеливание
- Shoot (bool) - стрельба
- OnAir (bool) - в воздухе

### 2. **Network Player Prefab**

Создайте префаб сетевого игрока со следующими компонентами:

#### Основные компоненты:

- **Transform** - позиция, поворот, масштаб
- **Animator** - для анимаций (обязательно!)
- **NetworkPlayer.cs** - сетевая синхронизация

#### ❌ НЕ добавляйте на Network Player:

- CharacterController (отключается автоматически)
- PlayerMovement (отключается автоматически)
- Camera (отключается автоматически)
- AudioListener (отключается автоматически)

#### Иерархия объекта:

```
NetworkPlayer (корневой объект)
├── Model (3D модель с Animator - тот же что у LocalPlayer)
└── WeaponHolder (для оружия - опционально)
```

#### Настройки NetworkPlayer.cs:

```
Interpolation Speed: 10
Max Distance: 1
Animator: [привязать компонент Animator]
```

## 🎮 Пример настройки сцены

### Шаг 1: Создание NetworkManager

1. Создайте пустой GameObject: "NetworkManager"
2. Добавьте компонент `Client`
3. Добавьте компонент `MultiplayerManager`
4. Настройте все поля в инспекторе

### Шаг 2: Создание точек спавна

1. Создайте пустые GameObjects: "SpawnPoint1", "SpawnPoint2", "SpawnPoint3"
2. Расставьте их по сцене
3. В Client.cs добавьте их в массив Spawn Points

### Шаг 3: Создание префабов

1. Создайте LocalPlayer префаб с полным набором компонентов
2. Создайте NetworkPlayer префаб только с NetworkPlayer.cs
3. Привяжите префабы к Client.cs

### Шаг 4: Настройка UI (опционально)

1. Создайте Canvas с UI элементами
2. Добавьте NetworkTestUI.cs
3. Привяжите все UI элементы

## ⚠️ Частые ошибки

### Ошибка 1: "No localPlayerPrefab assigned!"

**Решение:** Привяжите префаб локального игрока к полю Local Player Prefab в Client.cs

### Ошибка 2: "No networkPlayerPrefab assigned!"

**Решение:** Привяжите префаб сетевого игрока к полю Network Player Prefab в Client.cs

### Ошибка 3: NetworkPlayer не двигается

**Решение:** Убедитесь что у NetworkPlayer есть компонент Animator с правильными параметрами

### Ошибка 4: Дублирование игроков

**Решение:** Убедитесь что GameManager отключен или не спавнит игроков в сетевом режиме

### Ошибка 5: Камера не работает у LocalPlayer

**Решение:** Убедитесь что Camera и AudioListener включены только у LocalPlayer

## 🔧 Дополнительные настройки

### Настройки физики

В Project Settings → Physics:

- Убедитесь что есть слои для Player и NetworkPlayer
- Настройте коллизии между слоями

### Настройки анимации

Все префабы должны использовать один Animator Controller с параметрами:

- MoveX, MoveZ (float)
- Sprint, Aim, Shoot, OnAir, Dead (bool)

### Настройки тегов

Создайте теги:

- "Player" для локального игрока
- "NetworkPlayer" для сетевых игроков

## 📋 Чек-лист готовности

- [ ] NetworkManager с Client.cs и MultiplayerManager.cs
- [ ] LocalPlayer префаб с полным набором компонентов
- [ ] NetworkPlayer префаб только с NetworkPlayer.cs
- [ ] Точки спавна добавлены в массив Spawn Points
- [ ] UI элементы созданы и привязаны (опционально)
- [ ] Animator Controller настроен с нужными параметрами
- [ ] Сервер запущен и доступен
- [ ] Теги и слои настроены

После выполнения всех шагов ваш мультиплеер должен работать!
