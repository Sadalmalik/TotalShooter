# TotalShooter — технические спецификации для реализации

Конкретные детали (поля/методы/потоки), дополняющие решения из `.claude/architecture.md`.
Читать при работе над конкретной задачей реализации — не грузится автоматически.

## Жизненный цикл сессии — пошагово

1. `GameStarter` (на стартовой сцене) поднимает `UILoadingScreen` (сразу активен),
   инстанциирует `NetworkManager`, `GameManager`, `WorldManager`, список UI-префабов из
   `GameStarter`, показывает `UIMainMenu`.
2. Игрок 1 жмёт "Создать" → `UICreateGame` (название/пароль/выбор мира) → "создать":
   `NetworkManager` поднимает Relay-сессию + регистрирует комнату в Unity Lobby →
   `GameManager` загружает мир через `WorldManager` → создаёт `GameState`, `AIController`,
   `AIDirector` (один раз на сессию) → спавнится `PlayerState`+`PlayerController` для
   игрока 1 → `PlayerController.Possess(defaultGhost)` → скрывается `UILoadingScreen` →
   показывается `UIInGameHUD`.
3. Игрок 2 жмёт "Присоединиться" → `UIJoinGame` — список комнат из Unity Lobby, выбор,
   пароль (если нужен) → "присоединиться": клиент подключается через Relay → на хосте
   `NetworkManager` (в колбэке подключения NGO) спавнит `PlayerState`+`PlayerController` для
   игрока 2 → мир уже загружен, реплицируется в рамках подключения → `Possess(defaultGhost)`.
4. Каждый игрок жмёт "готов" (MVP: кнопка в `UIInGameHUD`, не скрипт — см.
   architecture.md "Заметки на будущее") → `GameManager` считает готовых, при всех готовых
   стартует 10-секундный countdown (пишет в `GameState.NetworkVariable`), новый коннект во
   время countdown — сброс.
5. Countdown = 0 → `GameManager` спавнит боевых персонажей всем, `Possess(character)` для
   каждого `PlayerController`, включает `AIController`/`AIDirector`.
6. Смерть персонажа → `Health` кидает событие смерти → `PlayerController.Unpossess()` +
   `Possess(existingGhost)` (тот же инстанс призрака, не новый) → таймер респавна (управляется
   скриптом, слушает событие смерти, по таймауту зовёт `Possess(character)` заново).
7. Хост в любой момент: `GameManager.SetSandboxMode(player, enabled)` — переключает
   призрак игрока на золотой вариант, останавливает `AIController`/`AIDirector` опционально
   по отдельности.

## GameManager — состояния матча

Простой enum/state machine, без over-engineering:

```
enum MatchPhase { Lobby, Countdown, InProgress, /* Ended — по необходимости позже */ }
```

`GameManager` — единственный, кто меняет `MatchPhase` и решает, что делать при смене (спавн
персонажей, вкл/выкл AI). `GameState.NetworkVariable<MatchPhase>` + `NetworkVariable<float>
CountdownRemaining` + `NetworkVariable<int> ReadyCount` — то, что реально нужно клиентам для UI.

## PlayerController — possession API

```
class PlayerController // NetworkObject, NetworkHide от не-владельца
{
    Entity CurrentPawn { get; }
    void Possess(Entity pawn);   // отвязать текущую (если есть), привязать новую,
                                  // переключить CameraOperator, начать читать инпут в pawn
    void Unpossess();            // отвязать текущую pawn, не уничтожая её
}
```

Призрак игрока — Entity, создаётся один раз при заходе в мир, не уничтожается при
Possess/Unpossess персонажа — только скрывается/показывается.

## EntityVariables — Lua-биндинг

Индексатор, не геттер/сеттер:

```lua
someEntity["Keys"] = someEntity["Keys"] + 1
```

Реализуется через NLua-таблицу/userdata с метаметодами `__index`/`__newindex`, которые внутри
дергают C#-словарь на компоненте `EntityVariables` конкретного `EntityId`. Промоушен в
репликацию — отдельный явный вызов/атрибут на конкретном ключе (не автоматический), например
концептуально `entityVariables.SetReplicated("Keys", true)` — конкретный API проектируется
при реализации.

## Health — модель урона

```
class Health // MonoBehaviour-компонент на Entity
{
    float Current;
    float Max;
    // Резисты/множители по типу урона — простая модель по аналогии с Factorio
    Dictionary<DamageType, float> Resistances; // или массив по фиксированному enum DamageType

    void ApplyDamage(float amount, DamageType type, EntityId source);
    event Action<EntityId source> OnDeath; // для скриптов/AIController/лута/killstreak-логики
}
```

`DamageType` — enum, конкретный список типов урона определяется при дизайне оружия/абилок
(не сейчас). Подсчёт фрагов/assist — отдельная логика поверх `OnDeath` + host-validated hits,
не внутри `Health`.

## Spawner — поля и API

```
class Spawner // компонент на Entity
{
    List<string> Tags;              // чтобы AIDirector отличал спавнеры друг от друга
    List<string> Units;              // имена типов юнитов (резолвятся через ResolvePrototype,
                                      // не прямые GameObject-ссылки — см. "Реестр контента"
                                      // в .claude/architecture.md)
    float SpawnCooldown;             // пауза между спавнами
    bool AutoSpawn;                  // спавнит ли сам по себе
    int AutoSpawnLimit;              // после скольких спавнов AutoSpawn сбрасывается в false

    void Spawn(int count, float interval); // вызывается вручную (AIDirector/зональный скрипт),
                                             // игнорирует/переопределяет AutoSpawn-лимит
}
```

Пример использования: игрок заходит в зону → зональный Lua-скрипт вызывает
`spawner.Spawn(20, 2.0f)` на нужных спавнерах по тегу.

## Реестр контента: формат data.json / world.json и резолвинг

`data.json` (локальные для мира определения, prototype-цепочка):

```json
[
  {
    "name": "world/ultra-lite-bug",
    "prototype": "core/lite-bug",
    "Health": { "Max": 40 }
  },
  {
    "name": "core/lite-bug",
    "prototype": "core/lite-bug",
    "prefab": "core/huge-bug",
    "Health": { "Max": 300 }
  }
]
```

`world.json` (инстансы на карте):

```json
[
  {
    "EntityId": 1,
    "Prefab": "core/wall_1",
    "Transform": { "Position": [0, 0, 10], "Rotation": [0, 0, 0], "Scale": [1, 1, 1] }
  },
  {
    "EntityId": 2,
    "Transform": { "Position": [0, 0, 5] },
    "Spawner": {
      "Tags": ["start-area", "difficulty:easy"],
      "Units": ["core/lite-bug", "core/medium-bug", "world/ultra-lite-bug"]
    }
  },
  {
    "EntityId": 3,
    "Prefab": "core/huge-bug",
    "Health": { "Current": 1400, "Max": 1400 }
  }
]
```

Резолвинг имени в объект (`ResolvePrototype(name)`), псевдокод:

```
ResolvePrototype(name, visited = {}):
    if name in visited: error "цикл в цепочке прототипов"
    visited.add(name)

    def = LookupInOrder(name)   // локальный data.json мира → (моды, когда появятся) → core-ассеты
    base = def.prototype != null
        ? ResolvePrototype(def.prototype, visited)   // рекурсия на N уровней
        : LoadPrefab(def.prefab ?? name)              // дно цепочки — реальный префаб

    return ApplyOverrides(base, def)  // те же оверрайды полей компонентов, что и при загрузке world.json
```

Весь код, спавнящий контент по имени (Spawner, Ability и т.п.), обязан идти через
`ResolvePrototype`, не напрямую через `Resources.Load`/`AssetBundle` — иначе локальные
переопределения `data.json` не применятся молча. Сохранение мира сейчас — полный дамп текущих
значений полей компонентов Entity (не дельта от резолвленных дефолтов) — проще для старта.

## Sandbox-редактирование не-хостом — поток

1. Клиент (не хост) в sandbox-режиме открывает инспектор `Entity` X, меняет поле компонента
   (например `Spawner.SpawnCooldown`).
2. Клиент отправляет хосту запрос: `{ EntityId, ComponentType, FieldName, NewValue }` через
   reliable RPC/event-канал.
3. Хост валидирует (права/группы — когда будут реализованы; сейчас можно пропускать проверку,
   т.к. защита от гриферства не в приоритете) и применяет изменение к реальному компоненту.
4. Хост рассылает применённое значение всем клиентам, у кого открыт инспектор этого Entity
   (или всем — проще для MVP), тем же event-каналом.

## UI — список экранов (для справки при разбивке на задачи)

`UIMainMenu` → `UICreateGame` | `UIJoinGame` (список через Unity Lobby) | `UISettings` | Выход.
`UIInGameHUD` — после `UICreateGame`/`UIJoinGame`, на время матча.
`UILoadingScreen` — единственный активный сразу на старте, `SetActive` поверх остального на
время загрузки мира.
