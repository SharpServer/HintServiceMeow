# Core Features

This documentation covers the public API of HintServiceMeow, organized by functional module.

---

## Table of Contents

- [Enums](#enums)
- [Hint Models](#hint-models)
  - [AbstractHint](#abstracthint)
  - [Hint](#hint)
  - [DynamicHint](#dynamichint)
- [Hint Content](#hint-content)
  - [AbstractHintContent](#abstracthintcontent)
  - [StringContent](#stringcontent)
  - [AutoContent](#autocontent)
- [HintCollection](#hintcollection)
- [PlayerDisplay](#playerdisplay)
- [Extension Methods](#extension-methods)
  - [HintExtension](#hintextension)
  - [PlayerDisplayExtension](#playerdisplayextension)
  - [NWPlayerExtension (Core)](#nwplayerextension-core)
  - [ExiledPlayerExtension (Core)](#exiledplayerextension-core)
- [UI Layer](#ui-layer)
  - [PlayerUI](#playerui)
  - [CommonHint](#commonhint)
  - [NWPlayerExtension (UI)](#nwplayerextension-ui)
  - [ExiledPlayerExtension (UI)](#exiledplayerextension-ui)
- [Interfaces](#interfaces)
  - [IPlayerDisplay](#iplayerdisplay)
  - [IHintParser](#ihintparser)
  - [IDisplayOutput](#idisplayoutput)
  - [ICompatibilityAdaptor](#icompatibilityadaptor)
  - [IDestructible](#idestructible)
  - [ILogger](#ilogger)
  - [IUpdateAnalyser](#iupdateanalyser)
- [Argument Classes](#argument-classes)

---

## Enums

All enums are in the namespace `HintServiceMeow.Core.Enum`.

### HintAlignment

Horizontal alignment of a hint's text.

| Value | Description |
|-------|-------------|
| `Left` | Align text to the left |
| `Right` | Align text to the right |
| `Center` | Align text to the center |

### HintVerticalAlign

Vertical alignment of a hint relative to its Y coordinate.

| Value | Description |
|-------|-------------|
| `Top` | Y coordinate represents the top edge of the text |
| `Middle` | Y coordinate represents the vertical center of the text |
| `Bottom` | Y coordinate represents the bottom edge of the text |

### HintSyncSpeed

Controls how quickly a hint's changes are synced to the player's screen. Higher values sync faster.

| Value | Numeric | Description |
|-------|---------|-------------|
| `Fastest` | 192 | Updates as soon as possible; may delay other hints |
| `Fast` | 160 | Plans an update immediately when the hint changes |
| `Normal` | 128 | Standard update speed |
| `Slow` | 96 | Waits for other hints to update first |
| `Slowest` | 64 | Waits longer than Slow |
| `UnSync` | 32 | Does not auto-sync on change; still updates when other hints trigger a sync |

### HintPriority

Priority for DynamicHint placement. Higher priority hints are arranged first.

| Value | Numeric |
|-------|---------|
| `Highest` | 192 |
| `High` | 160 |
| `Medium` | 128 |
| `Low` | 96 |
| `Lowest` | 64 |

### DynamicHintStrategy

Behavior when a DynamicHint cannot find available space.

| Value | Description |
|-------|-------------|
| `Hide` | Hide the hint when no position is available |
| `StayInPosition` | Keep the hint at its target position |

### AfterShowAction

Action to perform after a timed hint finishes displaying.

| Value | Description |
|-------|-------------|
| `Remove` | Remove the hint from the display |
| `Hide` | Set the hint's `Hide` property to `true` |

### DelayType

Strategy for scheduling delayed actions when multiple delays conflict.

| Value | Description |
|-------|-------------|
| `KeepFastest` | Keep the earliest (fastest) scheduled action time |
| `KeepSlowest` | Keep the latest (slowest) scheduled action time |
| `Override` | Always overwrite the previous action time |

---

## Hint Models

### AbstractHint

> Namespace: `HintServiceMeow.Core.Models.Hints`

Base class for all hint types. Implements `INotifyPropertyChanged` so that property changes automatically trigger display updates.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| Guid | `Guid` (readonly) | Auto-generated unique identifier |
| Id | `string` | Custom string identifier for lookup. Default: `""` |
| SyncSpeed | `HintSyncSpeed` | Update priority. Values: `Fastest`, `Fast`, `Normal`, `Slow`, `Slowest`, `UnSync`. Default: `Normal` |
| FontSize | `int` | Text font size. Default: `20` |
| LineHeight | `float` | Extra vertical spacing between lines |
| Content | `AbstractHintContent` | The content provider for this hint. Default: `StringContent("")` |
| Text | `string?` | Shortcut to get/set static text. Setting this replaces `Content` with a new `StringContent` |
| AutoText | `AutoContent.TextUpdateHandler?` | Shortcut to get/set a dynamic text delegate. Setting this replaces `Content` with a new `AutoContent` |
| Hide | `bool` | Whether the hint is hidden. Default: `false` |
| UpdateAnalyser | `IUpdateAnalyser` | Analyser that estimates when the next update will occur |

**Usage Example:**

```csharp
// Properties auto-sync to the player's screen
hint.Text = "Updated text";
hint.FontSize = 30;
// No additional method calls needed
```

---

### Hint

> Namespace: `HintServiceMeow.Core.Models.Hints`

A fixed-position hint displayed at specific screen coordinates. Inherits from [AbstractHint](#abstracthint).

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| YCoordinate | `float` | Vertical position. Higher values move the text lower on screen. Default: `700` |
| XCoordinate | `float` | Horizontal offset. Higher values move the text to the right. Default: `0` |
| Alignment | `HintAlignment` | Text alignment. Values: `Left`, `Right`, `Center`. Default: `Center` |
| YCoordinateAlign | `HintVerticalAlign` | How Y coordinate aligns to the text. Values: `Top`, `Middle`, `Bottom`. Default: `Middle` |

![Y Coordinate Example](Images/YCoordinateExample.jpg)

**Usage Example:**

```csharp
Hint hint = new Hint
{
    Text = "Hello World",
    FontSize = 40,
    YCoordinate = 700,
    Alignment = HintAlignment.Left
};

PlayerDisplay playerDisplay = PlayerDisplay.Get(player);
playerDisplay.AddHint(hint);
```

Since HSM has an auto-update feature, any changes to a property will automatically reflect on the player's screen without any further method calls.

```csharp
hint.Text = "Some New Text";
// No additional method calls needed
```

---

### DynamicHint

> Namespace: `HintServiceMeow.Core.Models.Hints`

A hint that is automatically positioned to avoid overlapping with other hints. Inherits from [AbstractHint](#abstracthint).

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| TopBoundary | `float` | Upper boundary for placement. Default: `0` |
| BottomBoundary | `float` | Lower boundary for placement. Default: `1000` |
| LeftBoundary | `float` | Left boundary for placement. Default: `-1200` |
| RightBoundary | `float` | Right boundary for placement. Default: `1200` |
| TargetX | `float` | Preferred horizontal position. Default: `0` |
| TargetY | `float` | Preferred vertical position. Default: `700` |
| TopMargin | `float` | Extra space above the hint during arrangement. Default: `5` |
| BottomMargin | `float` | Extra space below the hint during arrangement. Default: `5` |
| LeftMargin | `float` | Extra space to the left during arrangement. Default: `100` |
| RightMargin | `float` | Extra space to the right during arrangement. Default: `100` |
| Priority | `HintPriority` | Arrangement priority. Values: `Highest`, `High`, `Medium`, `Low`, `Lowest`. Default: `Medium` |
| Strategy | `DynamicHintStrategy` | Behavior when no space is available. Values: `Hide`, `StayInPosition`. Default: `Hide` |

**Usage Example:**

```csharp
var dynamicHint = new DynamicHint
{
    Text = "Hello Dynamic Hint"
};

PlayerDisplay playerDisplay = PlayerDisplay.Get(player);
playerDisplay.AddHint(dynamicHint);
```

---

## Hint Content

### AbstractHintContent

> Namespace: `HintServiceMeow.Core.Models.HintContent`

Base class for hint content providers.

**Events:**

| Event | Type | Description |
|-------|------|-------------|
| ContentUpdated | `UpdateHandler` | Raised when the content changes |

**Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| GetText | — | `string?` | Returns the current text content |
| TryUpdate | `ContentUpdateArg ev` | `void` | Called periodically to allow content to refresh |
| OnUpdated | — | `void` | Raises the `ContentUpdated` event |

---

### StringContent

> Namespace: `HintServiceMeow.Core.Models.HintContent`

A content provider that holds static text. Inherits from [AbstractHintContent](#abstracthintcontent).

**Constructor:** `StringContent(string? content)`

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| Text | `string?` | The static text. Raises `ContentUpdated` when changed |

---

### AutoContent

> Namespace: `HintServiceMeow.Core.Models.HintContent`

A content provider that periodically invokes a delegate to produce dynamic text. Inherits from [AbstractHintContent](#abstracthintcontent).

**Delegate:** `delegate string TextUpdateHandler(AutoContentUpdateArg ev)`

**Constructor:** `AutoContent(TextUpdateHandler? autoText, float defaultUpdateInterval = -1)`

If `defaultUpdateInterval` is negative, defaults to `0.1` seconds.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| AutoText | `TextUpdateHandler?` | The delegate invoked to produce text. Resetting this also resets the next update time |

**Usage Example:**

```csharp
hint.AutoText = (ev) =>
{
    ev.NextUpdateDelay = TimeSpan.FromSeconds(1); // Update every 1 second
    return $"Time: {DateTime.Now:HH:mm:ss}";
};
```

---

## HintCollection

> Namespace: `HintServiceMeow.Core.Models`

A collection that organizes hints by group (typically by assembly name). Implements `INotifyCollectionChanged`.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| AllGroups | `IReadOnlyList<IReadOnlyList<AbstractHint>>` | All hint groups (cached) |
| AllHints | `IReadOnlyList<AbstractHint>` | All hints flattened into a single list (cached) |

**Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| GetHints | `string? assemblyName` | `IReadOnlyList<AbstractHint>` | Returns hints for a group, or all hints if `null` |
| GetHints | `string assemblyName, Func<AbstractHint, bool> predicate` | `IReadOnlyList<AbstractHint>` | Returns filtered hints for a group |

---

## PlayerDisplay

> Namespace: `HintServiceMeow.Core.Utilities`

The central class for managing a player's hint display. Each player has one `PlayerDisplay` instance. Implements [IPlayerDisplay](#iplayerdisplay) and [IDestructible](#idestructible).

**Events:**

| Event | Type | Description |
|-------|------|-------------|
| UpdateAvailable | `UpdateAvailableEventHandler` | Raised each tick when the display is ready to update |

**Delegate:** `delegate void UpdateAvailableEventHandler(UpdateAvailableEventArg ev)`

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| ReferenceHub | `ReferenceHub?` (readonly) | The player this display belongs to |
| HintParser | `IHintParser` | The parser that converts hints to rich text. Replaceable |
| CompatibilityAdaptor | `ICompatibilityAdaptor` | The adaptor for compatibility with other plugins. Replaceable |

**Static Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| Get | `ReferenceHub referenceHub` | `PlayerDisplay` | Gets or creates a PlayerDisplay for the player |
| Get | `LabApi.Features.Wrappers.Player player` | `PlayerDisplay` | Gets or creates a PlayerDisplay (NW/LabApi) |
| Get | `Exiled.API.Features.Player player` | `PlayerDisplay` | Gets or creates a PlayerDisplay (EXILED only) |

**Instance Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| AddHint | `AbstractHint? hint` | `void` | Adds a hint to the display |
| AddHint | `IEnumerable<AbstractHint>? hints` | `void` | Adds multiple hints |
| AddHint | `params AbstractHint[]? hints` | `void` | Adds multiple hints (params) |
| AddHint | `AbstractHint? hint, string groupName` | `void` | Adds a hint to a specific group |
| ShowHint | `AbstractHint hint, float duration = 7f, AfterShowAction afterShow = AfterShowAction.Remove` | `void` | Adds a hint and automatically removes/hides it after `duration` seconds. `AfterShowAction` values: `Remove`, `Hide` |
| ShowHint | `IEnumerable<AbstractHint> hints, float duration = 7f, AfterShowAction afterShow = AfterShowAction.Remove` | `void` | Shows multiple hints with auto-removal |
| RemoveHint | `AbstractHint? hint` | `void` | Removes a hint |
| RemoveHint | `IEnumerable<AbstractHint>? hints` | `void` | Removes multiple hints |
| RemoveHint | `params AbstractHint[]? hints` | `void` | Removes multiple hints (params) |
| RemoveHint | `AbstractHint? hint, string groupName` | `void` | Removes a hint from a specific group |
| RemoveHint | `string id` | `void` | Removes all hints matching the given Id |
| RemoveHint | `Guid id` | `void` | Removes the hint matching the given Guid |
| ClearHint | — | `void` | Removes all hints owned by the calling assembly |
| GetHint | `string? id` | `AbstractHint?` | Returns the first hint matching the Id |
| GetHint | `Guid guid` | `AbstractHint?` | Returns the first hint matching the Guid |
| GetHints | `string id` | `IEnumerable<AbstractHint>` | Returns all hints matching the Id |
| GetHints | — | `IEnumerable<AbstractHint>` | Returns all hints owned by the calling assembly |
| HasHint | `string id` | `bool` | Checks if any hint with the given Id exists |
| HasHint | `Guid guid` | `bool` | Checks if a hint with the given Guid exists |
| TryGetHint | `string id, out AbstractHint hint` | `bool` | Tries to get the first hint matching the Id |
| TryGetHint | `Guid guid, out AbstractHint hint` | `bool` | Tries to get the first hint matching the Guid |
| TryGetHints | `string? id, out IEnumerable<AbstractHint> hints` | `bool` | Tries to get all hints matching the Id |
| ForceUpdate | `bool useFastUpdate = false` | `void` | Forces a display update. Use when working with `HintSyncSpeed.UnSync` |
| SetMinUpdateInterval | `TimeSpan interval` | `void` | Sets the minimum interval between updates |
| AddDisplayOutput | `IDisplayOutput output` | `void` | Adds a custom display output |
| RemoveDisplayOutput | `IDisplayOutput output` | `void` | Removes a display output |
| RemoveDisplayOutput\<T\> | — | `void` | Removes all display outputs of type `T` (where `T : IDisplayOutput`) |

**Usage Example:**

```csharp
PlayerDisplay pd = PlayerDisplay.Get(player);

// Add a hint
var hint = new Hint { Text = "Hello", YCoordinate = 500 };
pd.AddHint(hint);

// Show a temporary hint for 5 seconds
pd.ShowHint(new Hint { Text = "Temporary!" }, duration: 5f);

// Find and modify hints
if (pd.TryGetHint("my-hint-id", out var found))
{
    found.Text = "Updated";
}

// Force update for UnSync hints
pd.ForceUpdate();
```

---

## Extension Methods

### HintExtension

> Namespace: `HintServiceMeow.Core.Extension`

Extension methods for [AbstractHint](#abstracthint).

| Method | Extends | Parameters | Description |
|--------|---------|------------|-------------|
| HideAfter | `AbstractHint` | `float delay` | Sets `Hide = true` after `delay` seconds. Resets any existing hide timer |

**Usage Example:**

```csharp
hint.HideAfter(5f); // Hides the hint after 5 seconds
```

---

### PlayerDisplayExtension

> Namespace: `HintServiceMeow.Core.Extension`

Extension methods for [PlayerDisplay](#playerdisplay).

| Method | Extends | Parameters | Description |
|--------|---------|------------|-------------|
| RemoveAfter | `PlayerDisplay` | `AbstractHint hint, float delay` | Removes the hint from the display after `delay` seconds. Resets any existing removal timer |

**Usage Example:**

```csharp
playerDisplay.RemoveAfter(hint, 10f); // Removes the hint after 10 seconds
```

---

### NWPlayerExtension (Core)

> Namespace: `HintServiceMeow.Core.Extension`

Extension methods for `LabApi.Features.Wrappers.Player`.

| Method | Extends | Parameters | Returns | Description |
|--------|---------|------------|---------|-------------|
| GetPlayerDisplay | `Player` | — | `PlayerDisplay` | Gets the player's PlayerDisplay |
| AddHint | `Player` | `AbstractHint hint` | `void` | Adds a hint to the player's display |
| RemoveHint | `Player` | `AbstractHint hint` | `void` | Removes a hint from the player's display |

---

### ExiledPlayerExtension (Core)

> Namespace: `HintServiceMeow.Core.Extension`

Extension methods for `Exiled.API.Features.Player`. Only available in EXILED builds.

| Method | Extends | Parameters | Returns | Description |
|--------|---------|------------|---------|-------------|
| GetPlayerDisplay | `Player` | — | `PlayerDisplay` | Gets the player's PlayerDisplay |
| AddHint | `Player` | `AbstractHint hint` | `void` | Adds a hint to the player's display |
| RemoveHint | `Player` | `AbstractHint hint` | `void` | Removes a hint from the player's display |

---

## UI Layer

### PlayerUI

> Namespace: `HintServiceMeow.UI.Utilities`

Per-player UI facade that provides access to [CommonHint](#commonhint). Implements [IDestructible](#idestructible).

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| ReferenceHub | `ReferenceHub` (readonly) | The underlying player reference |
| PlayerDisplay | `PlayerDisplay` (readonly) | The player's PlayerDisplay instance |
| CommonHint | `CommonHint` (readonly) | The common hint component |

**Static Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| Get | `ReferenceHub referenceHub` | `PlayerUI` | Gets or creates a PlayerUI for the player |
| Get | `LabApi.Features.Wrappers.Player player` | `PlayerUI` | Gets or creates a PlayerUI (NW/LabApi) |
| Get | `Exiled.API.Features.Player player` | `PlayerUI` | Gets or creates a PlayerUI (EXILED only) |

---

### CommonHint

> Namespace: `HintServiceMeow.UI.Utilities`

Provides pre-configured hint layouts for common use cases: item descriptions, map info, role info, and general messages. Implements [IDestructible](#idestructible).

All display durations are configurable via the plugin config. The `time` parameter in each overload is in seconds.

**Methods — Item Hints:**

| Method | Parameters | Description |
|--------|------------|-------------|
| ShowItemHint | `string itemName` | Shows item name only (short duration) |
| ShowItemHint | `string itemName, float time` | Shows item name only with custom duration |
| ShowItemHint | `string itemName, string description` | Shows item name and one description line |
| ShowItemHint | `string itemName, string description, float time` | Shows item name and one description line with custom duration |
| ShowItemHint | `string itemName, string[] description` | Shows item name and multiple description lines |
| ShowItemHint | `string itemName, string[] description, float time` | Shows item name and multiple description lines with custom duration |

**Methods — Map Hints:**

| Method | Parameters | Description |
|--------|------------|-------------|
| ShowMapHint | `string roomName` | Shows room name only (short duration) |
| ShowMapHint | `string roomName, float time` | Shows room name only with custom duration |
| ShowMapHint | `string roomName, string description` | Shows room name and one description line |
| ShowMapHint | `string roomName, string description, float time` | Shows room name and one description line with custom duration |
| ShowMapHint | `string roomName, string[] description` | Shows room name and multiple description lines |
| ShowMapHint | `string roomName, string[] description, float time` | Shows room name and multiple description lines with custom duration |

**Methods — Role Hints:**

| Method | Parameters | Description |
|--------|------------|-------------|
| ShowRoleHint | `string roleName` | Shows role name only (short duration) |
| ShowRoleHint | `string roleName, float time` | Shows role name only with custom duration |
| ShowRoleHint | `string roleName, string description` | Shows role name and one description line |
| ShowRoleHint | `string roleName, string description, float time` | Shows role name and one description line with custom duration |
| ShowRoleHint | `string roleName, string[] description` | Shows role name and multiple description lines |
| ShowRoleHint | `string roleName, string[] description, float time` | Shows role name and multiple description lines with custom duration |

**Methods — Other Hints:**

| Method | Parameters | Description |
|--------|------------|-------------|
| ShowOtherHint | `string messages` | Shows a single message as a DynamicHint |
| ShowOtherHint | `string messages, float time` | Shows a single message with custom duration |
| ShowOtherHint | `string[] messages` | Shows multiple messages (duration scales with count) |
| ShowOtherHint | `string[] messages, float time` | Shows multiple messages with custom total duration |

**Usage Example:**

```csharp
var ui = PlayerUI.Get(player);
ui.CommonHint.ShowRoleHint("SCP-173", new[] { "Kill all humans", "Use your skills" });
ui.CommonHint.ShowMapHint("Heavy Containment Zone", "The place where most SCPs spawn");
ui.CommonHint.ShowItemHint("Keycard", "Used to open doors");
ui.CommonHint.ShowOtherHint("The server is starting!");
```

---

### NWPlayerExtension (UI)

> Namespace: `HintServiceMeow.UI.Extension`

Extension methods for `LabApi.Features.Wrappers.Player` to access the UI layer.

| Method | Extends | Returns | Description |
|--------|---------|---------|-------------|
| GetPlayerUi | `Player` | `PlayerUI` | Gets the player's PlayerUI instance |

---

### ExiledPlayerExtension (UI)

> Namespace: `HintServiceMeow.UI.Extension`

Extension methods for `Exiled.API.Features.Player` to access the UI layer. Only available in EXILED builds.

| Method | Extends | Returns | Description |
|--------|---------|---------|-------------|
| GetPlayerUi | `Player` | `PlayerUI` | Gets the player's PlayerUI instance |

---

## Interfaces

All interfaces are in the namespace `HintServiceMeow.Core.Interface`.

### IPlayerDisplay

The interface implemented by [PlayerDisplay](#playerdisplay).

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| HintParser | `IHintParser` | The hint parser used to convert hints to display text |
| CompatibilityAdaptor | `ICompatibilityAdaptor` | The compatibility adaptor for other plugins |

**Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| AddDisplayOutput | `IDisplayOutput output` | `void` | Adds a display output |
| RemoveDisplayOutput | `IDisplayOutput output` | `void` | Removes a display output |
| RemoveDisplayOutput\<T\> | — | `void` | Removes all outputs of type `T` where `T : IDisplayOutput` |
| AddHint | `AbstractHint hint` | `void` | Adds a hint |
| RemoveHint | `AbstractHint hint` | `void` | Removes a hint |
| ClearHint | — | `void` | Clears all hints |
| GetHints | `string id` | `IEnumerable<AbstractHint>` | Gets hints by Id |
| GetHints | — | `IEnumerable<AbstractHint>` | Gets all hints |
| ForceUpdate | `bool useFastUpdate = false` | `void` | Forces a display update |

---

### IHintParser

Converts a [HintCollection](#hintcollection) into a formatted message string for display.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| ParseToMessage | `HintCollection collection` | `string` | Parses all hints into a single display message |

---

### IDisplayOutput

Sends the final rendered hint text to the player.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| ShowHint | `DisplayOutputArg ev` | `void` | Outputs the hint content to the player |

---

### ICompatibilityAdaptor

Adapts hint display calls from other (incompatible) plugins.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| ShowHint | `CompatibilityAdaptorArg ev` | `void` | Processes a compatibility hint |

---

### IDestructible

Marks a class as having resources that need cleanup.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| Destruct | — | `void` | Releases resources and performs cleanup |

---

### ILogger

Logging interface used throughout HintServiceMeow.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| Info | `object message` | `void` | Logs an informational message |
| Error | `object message` | `void` | Logs an error message |
| Debug | `object message` | `void` | Logs a debug message |

---

### IUpdateAnalyser

Tracks hint update patterns to predict optimal update timing.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| OnUpdate | — | `void` | Notifies the analyser that an update occurred |
| EstimateNextUpdate | — | `DateTime` | Estimates when the next update will happen |

---

## Argument Classes

All argument classes are in the namespace `HintServiceMeow.Core.Models.Arguments`. Their constructors are internal; they are created by the framework and passed to callbacks.

### AutoContentUpdateArg

Passed to `AutoContent.TextUpdateHandler` delegates.

| Property | Type | Description |
|----------|------|-------------|
| Hint | `AbstractHint` (readonly) | The hint being updated |
| PlayerDisplay | `PlayerDisplay` (readonly) | The owning PlayerDisplay |
| NextUpdateDelay | `TimeSpan` | Set this to control when the next update occurs |
| DefaultUpdateDelay | `TimeSpan` | The default update interval |

### ContentUpdateArg

Passed to `AbstractHintContent.TryUpdate`.

| Property | Type | Description |
|----------|------|-------------|
| Hint | `AbstractHint` (readonly) | The hint being updated |
| PlayerDisplay | `PlayerDisplay` (readonly) | The owning PlayerDisplay |

### DisplayOutputArg

Passed to `IDisplayOutput.ShowHint`.

| Property | Type | Description |
|----------|------|-------------|
| PlayerDisplay | `PlayerDisplay` (readonly) | The owning PlayerDisplay |
| Content | `string` (readonly) | The rendered hint text to display |

### CompatibilityAdaptorArg

Passed to `ICompatibilityAdaptor.ShowHint`.

| Property | Type | Description |
|----------|------|-------------|
| AssemblyName | `string` (readonly) | Name of the calling assembly |
| Content | `string?` (readonly) | The hint content |
| Duration | `float` (readonly) | Display duration in seconds |

### UpdateAvailableEventArg

Passed to the `PlayerDisplay.UpdateAvailable` event.

| Property | Type | Description |
|----------|------|-------------|
| PlayerDisplay | `PlayerDisplay` | The PlayerDisplay that is ready to update |
