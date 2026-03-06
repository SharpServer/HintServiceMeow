Click [here](/Docs/English/README.md) to go back to read me

## Getting Started
### Set up dependencies
1. Create your C# project
2. Include the dll file downloaded from release into your project's dependencies
### Show your first hint
The following code blocks show how to use frequently used features of HSM.

---
Create a Hello World hint on player's screen.
```CSharp
Player player = Player.Get(xxx);

// Hint is a simple hint that can be shown on the player's screen.
Hint hint1 = new Hint
{
    Text = "Hello World" // You can set the properties of the hint in a pair of braces({})
};

// You can set the properties of the hint like this:
hint1.FontSize = 40;
hint1.YCoordinate = 700;
hint1.Alignment = HintAlignment.Left;
// After setting the properties, you don't need to use method to call for an update. All update will be done automatically by the HSM(HintServiceMeow).

// You can show a hint to a player by adding it to the player's PlayerDisplay.
// You can also remove it by removing it from the PlayerDisplay.
PlayerDisplay playerDisplay = PlayerDisplay.Get(player);
playerDisplay.AddHint(hint1);
// playerDisplay.RemoveHint(hint);

```
---
Use `AutoText` to create a hint that update the content automatically.

Use extension to add or remove hint in a simple manner.
```CSharp
Hint hint2 = new Hint
{
    AutoText = ev => DateTime.Now.ToString("HH:mm:ss"), // You can also use a function to set the text of the hint, and the hint will update itself.
    Alignment = HintAlignment.Right, // You can set the properties of the hint in any order you want, and you can also choose to not set some properties, because all properties have a default value.
    YCoordinate = 200
};

// You can also use extension method to make it easier
player.AddHint(hint2); // This is equivalent to playerDisplay.AddHint(hint);
// player.RemoveHint(hint); // This is equivalent to playerDisplay.RemoveHint(hint);

```
---
Use `NextUpdateDelay` to set the custom update rate of your AutoText.

Use `PlayerDisplay::ShowHint(Hint, float)` to show a hint for a certain time.
```CSharp
Hint hint3 = new Hint()
{
    YCoordinate = 300,
    Alignment = HintAlignment.Right,
    AutoText = ev =>
    {
        ev.NextUpdateDelay = TimeSpan.FromSeconds(2f); // You can set the next update delay in the event argument, and the hint will update itself after the delay. This is useful when you want to update the hint after a certain time interval.

        return "TPS: " + Server.Tps.ToString("F2");
    },
};

// If you only want to show a hint temporarily, you can use ShowHint
playerDisplay.ShowHint(hint3, 12f); // This shows a hint for 12 seconds, then hide it.

```
---
Use Dynamic Hint to help avoid conflict.
```CSharp
// DynamicHint is a hint that can automatically arrange itself to avoid overlapping with other hints.
DynamicHint dynamicHint = new DynamicHint
{
    Text = "Hello Dynamic Hint",
    TargetX = 100f,
};

playerDisplay.AddHint(dynamicHint);

```
---
Use CommonHint to quickly develop your UI.
```CSharp
// PlayerUI::CommonHint is a set of preset hints that help you to show hints easily
PlayerUI ui = PlayerUI.Get(player);
ui.CommonHint.ShowRoleHint("SCP173", ["Kill all humans", "Use your skills"]);
ui.CommonHint.ShowMapHint("Heavy Containment Zone", "The place where most SCPs spawn");
ui.CommonHint.ShowItemHint("Keycard", "Used to open doors");
ui.CommonHint.ShowOtherHint("The server is starting!");
```
---
The above code blocks will create an UI like this:
![Hint view](Images/GettingStartedExample.jpg)
Labeled:
![Hint view labeled](Images/GettingStartedExampleLabeled.jpg)

Read [Core Features](CoreFeatures.md) to learn more

Click [here](/Docs/English/README.md) to go back to read me