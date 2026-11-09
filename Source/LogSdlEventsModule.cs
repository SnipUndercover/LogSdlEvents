using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SDL2;

namespace Celeste.Mod.LogSdlEvents;

public class LogSdlEventsModule : EverestModule
{
    public static LogSdlEventsModule Instance { get; private set; }

    public LogSdlEventsModule()
    {
        Instance = this;
    #if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(LogSdlEvents), LogLevel.Verbose);
    #else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(LogSdlEvents), LogLevel.Info);
    #endif
    }

    public override void Load()
    {
        HookKeyboardEvents();
        Celeste.Instance.Components.Add(LogControllerPressesComponent.Instance);
    }

    public override void Unload()
    {
        UnhookKeyboardEvents();
        Celeste.Instance.Components.Remove(LogControllerPressesComponent.Instance);
    }

    private Type Sdl2_FnaPlatform;
    private MethodInfo Sdl2_FnaPlatform_ToXnaKey;
    private ILHook Sdl2FnaPlatform_PollEventsHook;

    private void HookKeyboardEvents()
    {
        // we want to hook into Microsoft.Xna.Framework.SDL2_FNAPlatform.PollEvents
        // but it's an internal class - we need to do some reflection to get it
        Sdl2_FnaPlatform = typeof(Vector2).Assembly.GetType("Microsoft.Xna.Framework.SDL2_FNAPlatform")!;

        // get a useful method to convert SDL2 keys to XNA ones while we're at it
        Sdl2_FnaPlatform_ToXnaKey = Instance.Sdl2_FnaPlatform
            .GetMethod("ToXNAKey", BindingFlags.NonPublic | BindingFlags.Static)!;

        // now, actually hook into the method
        Sdl2FnaPlatform_PollEventsHook = new ILHook(
            Sdl2_FnaPlatform.GetMethod("PollEvents", BindingFlags.Public | BindingFlags.Static)!,
            PollEvents
        );
    }

    private void UnhookKeyboardEvents()
    {
        // clean up after ourselves
        Sdl2FnaPlatform_PollEventsHook?.Dispose();
    }

    private static void PollEvents(ILContext il)
    {
        // add logging right after PollEvents sees that an event was received
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, static instr => instr.MatchLdloc0());
        cursor.EmitDup();
        cursor.EmitDelegate(LogEvent);
    }

    private static void LogEvent(SDL.SDL_Event e)
    {
        const string LogId = $"{nameof(LogSdlEvents)}/{nameof(LogEvent)}";

        // do the logging
        Logger.Info(LogId, $"Received SDL event: {e.type}");
        switch (e.type)
        {
            case SDL.SDL_EventType.SDL_KEYDOWN:
                Keys keyDown = (Keys)Instance.Sdl2_FnaPlatform_ToXnaKey.Invoke(null, [e.key.keysym])!;
                Logger.Info(LogId, $"KeyDown info: {{ key: {keyDown}, repeat: {e.key.repeat} }}");
                break;
            case SDL.SDL_EventType.SDL_KEYUP:
                Keys keyUp = (Keys)Instance.Sdl2_FnaPlatform_ToXnaKey.Invoke(null, [e.key.keysym])!;
                Logger.Info(LogId, $"KeyUp info: {{ key: {keyUp} }}");
                break;
        }
    }
}
