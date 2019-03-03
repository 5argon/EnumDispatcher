# Enum Dispatcher

Action dispatching is an important part in [Flux](https://facebook.github.io/flux/docs/dispatcher.html#content) and [Redux](https://redux.js.org/basics/actions), to ensure any "data store" could change its data without caring about anything else other than user's action. Unit testing paradise! Just dispatch actions and see what the data have become.

I am bringing this workflow to Unity, but erasing the "string action label" pain point of actions in JavaScript by using C#'s `enum` instead. [Redux's designer said that](https://redux.js.org/faq/actions#why-should-type-be-a-string-or-at-least-serializable-why-should-my-action-types-be-constants), he advised using `string` as an action's key rather than JS `Symbol` because it is easily serializable and allows time travel. In Unity I care more about C# tooling provided by `enum`.

## Action principle of Redux / Flux

- Every possible action by user must be represented by action. So in effect, an action cannot cause another action by itself. (So != public methods, which is kinda the "verb" of programming world.)
- Data can only mutate in response to an action. Then that mutation will cause a presentation change.

Google to learn more about benefits of thinking like this.

## Terms

- **Action** : This is one `enum` number inside an `enum` type. `enum` with the same underlying `int` value but from different `enum` type is considered different.
- **Category** : The `enum` type serves as action's category. You can check if an action is in a category or not.
- **Payload** : One action can be attached with `object` payload. It is so that you can vary an action's detail instead of defining many more granular actions.
- **Payload Key** : You can attach multiple payloads to each action. Because C# do not have dynamic dot notation like JavaScript and I don't want to mess with `dynamic`, you instead use payload key to get the correct payload from an action. Payload Key is also an `enum`.
- **Flag** : Each action is strictly in one Category, however it could be added multiple Flags. An action from a different Category can be assigned the same Flag. For example you have the action `BackButtonPressed` in multiple Category describing pressing the top corner back button of each scene. You could assign `Back` flag to all of them, then have an action receiver do something whenever any `Back` was dispatched. (Like unloading things, etc.). Flags are instead based on `string` and not an another `enum`. You can define `const string` for them.

## Coding style 

### Declaration and dispatching

Your action declaration may looks like this, along with some simplified usages : 

```cs
public class MainMenu
{
    public enum Action
    {
        QuitGame,
        ToModeSelect,
        ToCredits,
        TouchedEmptyArea,
        [F(Navigation)] LeftButton, //F attribute is short for flags.
        [F(Navigation)] RightButton,
    }

    public enum PayloadKey
    {
        TouchCoordinate,
    }

    public const string Navigation = nameof(Navigation);

    public void ToModeSelectButtonOnClick()
    {
        //Without payload
        Dispatcher.Dispatch(MainMenu.Action.ToModeSelect);
    }

    public void EmptyAreaOnPointerDown()
    {
        //With payload (as a tuple of the key and `object`)
        Dispatcher.Dispatch(MainMenu.Action.TouchedEmptyArea, 
            (MainMenu.PayloadKey.TouchCoordinate, new Vector2(100,150))
        );
    }
}
```

### Action handling

How to directly check for that exact action with `if` : Use `.Is`.

```cs
private void OnAction(DispatchAction action)
{
    if (action.Is(MusicSelect.Action.SelectSong))
    {
        ...
    }
    else if(action.Is(MusicSelect.Action.SelectDifficulty))
    {
        ...
    }
}
```

How to handle 2 categories at once with `if` and `switch case` : Use `.Category<T>` then use the generic-typed `out` variable with `switch`.

```cs
private void OnAction(DispatchAction action)
{
    if (action.Category<MusicSelect.Action>(out var actMs)) switch (actMs)
    {
        case MusicSelect.Action.SelectSong:
            ...
    }
    else if (action.Category<MusicStart.Action>(out var act)) switch (act)
    {
        case MusicStart.Action.Begin:
            ...
        case MusicStart.Action.BeginEditor:
            ...
        case MusicStart.Action.ToggleRivalView:
            ...
        case MusicStart.Action.ChangeChartDifficulty:
            ...
    }

    ...
}
```


For how to do it in C# Jobs, please see the `Tests` folder.

## Why enum? Not string?

- Strings are brittle and annoying.
- Enums can auto complete.
- Enums are easier to define than `const string`. You don't even have to name the variable.
- Mass-rename by your IDE tooling.
- You can use your IDE to easily find all places that dispatch a certain event by searching enum references.
- Enum can be nested in the class so that dot notation looks nice. It allows you to for example, always name your enum as `Action`, so you don't have to worry about naming conflict. When used, it will looks like `MainMenu.Action.Back`, `ModeSelect.Action.Back` which is quite readable.
- There is an optimization at compiler level that make it fast with `switch case`. It does not require equality comparing case by case but a jump table instead. If these `enum` were just normal `int` it would generate comparison assembly per case, same goes for `string`. This may matter if your action handling code path is hot. (And maybe being Burst compiled for even better assembly.) 

## Pain points in doing so

- Different enum may have an equal underlying `int` value. This makes naive enum-as-label implementation wrong as action in one category replacable by action in an another category. Enum Dispatcher can detect that the same `enum` value are coming from a different `enum` type by also including/caching type information.
- Check for action by `==` is fine, but you can't do `switch case` if the receiving side doesn't contains enum typing information. If the receiving side contains the action type information, then it is not capable of handling action across multiple categories. Enum Dispatcher contains an action wrapper named `DispatchAction` instead of the `enum`. It contains various methods to help to determine the exact action while keeping the receiving side just know about `DispatchAction`.
- Action category via `enum` requires bookkeeping the type. Enum Dispatcher cache `enum` types on-the-go inspired by Unity's Entities package's `TypeManager`.
- Can you all that in C# Jobs? So you could check on action type and act all inside a single job instead of checking on the main thread and having to relay information to the job what to do. Yes, Enum Dispatcher can! With support from `JobDispatchAction` it brings together all its category and flags data to the job. On converting from `DispatchAction` to `JobDispatchAction`, it because all `struct` and `NativeArray` based. This bridges the whole thing to ECS as well. Unfortunately action payload cannot go to the job as it is based on `object` type.

## Dependencies

- C# 7.0, it uses tuples extensively too.
- Entities UPM packages and friends.

## Why it has to do anything with ECS at all? I don't want to depends on ECS package.

I *could* design it as a `static` enum dispatcher where anyone can receive the action. However I decided to bring ECS into play :

- Avoid using `static`, dispatched actions are now `World`-bound. (Though tecnically `World` are `static` beings)
- Allows me to design a `System` which automatically subscribe/unsubscribe to Enum Dispatcher's action because it knows to look for "Dispatching System" in the same world. It works together with `JobDispatchAction` support, so you are not limited to just C# Jobs but use them with `JobComponentSystem`-based action handling.
- (Real reason : Actually I pulled this out from my other hybrid ECS library for dealing with uGUI, so I need it to be compatible with ECS and jobs.)

And so Enum Dispatcher's `asmdef` requires Entity package present. Install them from Package Manager.

Also it is a good bridge from normal world to ECS. For example, Normally you connect the uGUI `Button`'s `On Click` to some public methods. It is not possible to connect with ECS's system since they are not in the scene. With Enum Dispatcher, all uGUI `Button` in the game no longer ever have to contains any logic other than dispatching an enum action. ECS system is now able to respond to button press, also your `MonoBehaviour` things can subscribe as an action receiver as well. Also it is awesome for unit testing now that you can mock user's behaviour by just dispatching actions over and over.

## Architecture

- An ECS system `DispatchingSystem` holds C# `event`. You can subscribe or dispatch by getting this system's reference from your `World` and call its public method. You can declare the callback method anywhere, in `MonoBehaviour`, etc.
- Call `dispatchingSystem.Dispatch` on the system instance will invoke all subscribers with that action immediately. You call it with your `enum`, but action handlers will receive `DispatchAction`. Alternatively, an easy utility `static` method `Dispatcher.Dispatch` will get `DispatchingSystem` in your `World.Active` first then do the same thing. Notice that to this point nothing is related to ECS yet. It didn't create any event entity. Just `.Invoke()`. At this point it is already usable as a general purpose enum-based event system. 
- On each dispatch call, there is one more system which bookkeep enum types of the action. Each `enum` will get its own index. Both `enum` type index and the `enum` integer value will be used together to represent one unique action. `DispatchAction` is an object containing those information. This bookkeeping system does so by using native containers, so this entire "type dictionary" they could be referenced safely from a job, allowing you to check action type on thread.
- Any ECS system inherited from `ActionHandlerSystem` is automatically subscribed/unsubscribe to `DispatchingSystem` of the `World` it is currently in. `ActionHandlerSystem` receives actions immediately like manual subscribers, but you cannot respond to them just yet. They will all be queued, then on its `OnUpdate` you can respond to them with an opportunity to schedule a job since it is a subclass of `JobComponentSystem`. You should `override` the `virtual` method `OnAction` where it will give you actions one by one in order. `JobHandle` is provided in that respond context so all your jobs are hooked up to ECS job pipeline. This is why you can't respond to action immediately. You can check the action first then schedule appropriate jobs, or bring action into the job and check them inside so you could offload main thread. (If the check and respond is complicated)
- `ActionHandlerSystem` is preconfigured to update in an update group called `ActionHandlerSystem.ActionHandlerGroup`. If you want to make sure your system updates after all actions are handled you can use `[UpdateAfter(typeof(ActionHandlerSystem.ActionHandlerGroup))]`, so you can use the result from scheduled jobs that was a response to an action this frame.

## How to use

Please see usage examples from the `Tests` folder, where you will witness an epic fight with monsters.