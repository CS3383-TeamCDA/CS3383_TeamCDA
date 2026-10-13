using System;
using System.Threading;
using UnityEngine;
using EscapeThe90s.HUD;

namespace EscapeThe90s.MetaProgression
{
    public class MetaProgressionManager
    {
        public static MetaProgressionManager Instance { get; private set; }

        private HUDController hudController;
        private int currency = 0;
        private CancellationTokenSource _cts;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Instance?.StopTimer();
            Instance = new MetaProgressionManager();
            Instance.StartTimer();
        }

        public void StartTimer()
        {
            StopTimer();
            _cts = new CancellationTokenSource();
            _ = CurrencyTimer(_cts.Token);
        }

        public void StopTimer()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void AddCurrency(int amount)
        {
            currency += amount;

            // Unity's overloaded == treats destroyed objects as null,
            // so this also re-finds the HUD after a scene change.
            if (hudController == null)
            {
                hudController = UnityEngine.Object.FindAnyObjectByType<HUDController>();
            }

            if (hudController != null)
            {
                hudController.UpdateCurrency(currency);
            }
        }

        private async Awaitable CurrencyTimer(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Awaitable.WaitForSecondsAsync(10f, token);
                    AddCurrency(1);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}