using FFXIVClientStructs.FFXIV.Client.Game;

namespace CursorRing;

internal static class GlobalCooldownReader
{
    private const int GlobalCooldownGroup = 57;

#if CURSORRING_BENCHMARK
    internal static GcdObservation LastObservation { get; private set; }
#endif

    internal static unsafe GcdState Read()
    {
        try
        {
            var manager = ActionManager.Instance();
            if (manager is null)
            {
#if CURSORRING_BENCHMARK
                LastObservation = new GcdObservation(GcdReadStatus.ManagerUnavailable, false, 0f, 0f);
#endif
                return GcdState.Inactive;
            }

            var gcd = manager->GetRecastGroupDetail(GlobalCooldownGroup);
            if (gcd is null)
            {
#if CURSORRING_BENCHMARK
                LastObservation = new GcdObservation(GcdReadStatus.DetailUnavailable, false, 0f, 0f);
#endif
                return GcdState.Inactive;
            }

#if CURSORRING_BENCHMARK
            LastObservation = new GcdObservation(GcdReadStatus.Read, gcd->IsActive, gcd->Elapsed, gcd->Total);
#endif
            return GcdState.Create(gcd->IsActive, gcd->Elapsed, gcd->Total);
        }
        catch
        {
#if CURSORRING_BENCHMARK
            LastObservation = new GcdObservation(GcdReadStatus.Failed, false, 0f, 0f);
#endif
            return GcdState.Inactive;
        }
    }
}

#if CURSORRING_BENCHMARK
internal enum GcdReadStatus
{
    None,
    Read,
    ManagerUnavailable,
    DetailUnavailable,
    Failed
}

internal readonly record struct GcdObservation(GcdReadStatus Status, bool NativeActive, float Elapsed, float Total);
#endif
