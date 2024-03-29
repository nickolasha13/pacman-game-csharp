using System.Collections.Concurrent;
using CommonLogic.Core;

namespace GameConsole;

public class ConsoleInputProvider : InputProvider
{
    private readonly ConsoleKeybindings keybindings;
    private readonly HashSet<Signal> received = new();
    private readonly ConcurrentQueue<Signal> signals = new();
    private readonly Thread worker;

    public ConsoleInputProvider(ConsoleKeybindings keybindings)
    {
        this.keybindings = keybindings;
        worker = new Thread(Worker);
        worker.Start();
    }

    private void Worker()
    {
        while (true)
        {
            var key = Console.ReadKey(true);
            var awaitingRebind = GetAwaitingRebind();
            if (awaitingRebind != null)
            {
                if (keybindings.IsUsed(key.Key) && keybindings.Get(awaitingRebind.Value) != key.Key)
                    continue;
                keybindings.Rebind(awaitingRebind.Value, key.Key);
                ResetRebinding();
                continue;
            }

            var signal = keybindings.Get(key.Key);
            if (signal != null)
                signals.Enqueue(signal.Value);
        }
    }

    public override void Sync()
    {
        received.Clear();
        while (signals.TryDequeue(out var signal))
            received.Add(signal);
    }

    public override bool IsReceived(Signal signal)
    {
        return received.Contains(signal);
    }

    public override void Dispose()
    {
        worker.Interrupt();
    }

    public override string GetSignalBindingAsString(Signal signal)
    {
        return keybindings.Get(signal).ToString();
    }
}