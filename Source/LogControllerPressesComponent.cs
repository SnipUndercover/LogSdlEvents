using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.LogSdlEvents;

public class LogControllerPressesComponent : GameComponent
{
    private const string LogId = $"{nameof(LogSdlEvents)}/{nameof(LogControllerPressesComponent)}";

    private static readonly FieldInfo _gamePadButtons_buttons
        = typeof(GamePadButtons).GetField("buttons", BindingFlags.NonPublic | BindingFlags.Instance);

    public static LogControllerPressesComponent Instance
        => field ??= new LogControllerPressesComponent();

    private LogControllerPressesComponent() : base(Celeste.Instance)
    {
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        MInput.GamePadData controller = MInput.GamePads[0];

        Buttons currentButtons = (Buttons)_gamePadButtons_buttons.GetValue(controller.CurrentState.Buttons)!;
        Buttons previousButtons = (Buttons)_gamePadButtons_buttons.GetValue(controller.PreviousState.Buttons)!;

        if (currentButtons != previousButtons)
            Logger.Info(LogId, $"Controller state changed: {currentButtons}");
    }
}
