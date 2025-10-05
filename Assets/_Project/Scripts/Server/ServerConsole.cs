#if UNITY_SERVER
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using PromoCodes;

public class ServerConsole : MonoBehaviour
{
    // для планирования задач в главном Unity-потоке
    private SynchronizationContext _mainThreadContext;
    private Thread _consoleThread;

    void Awake()
    {
        // захватываем контекст главного потока
        _mainThreadContext = SynchronizationContext.Current;

        // запускаем фоновый поток для чтения консоли
        _consoleThread = new Thread(ConsoleLoop)
        {
            IsBackground = true,
            Name = "ServerConsoleThread"
        };
        _consoleThread.Start();
    }

    private void ConsoleLoop()
    {
        while (true)
        {
            string line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // команда должна быть из 5 частей:
            // create promocode ПРИМЕРПРОМОКОД 1 30
            var parts = line.Trim().Split(' ');
            if (parts.Length != 5)
                continue;

            if (!parts[0].Equals("create", StringComparison.OrdinalIgnoreCase) ||
                !parts[1].Equals("promocode", StringComparison.OrdinalIgnoreCase))
                continue;

            string code     = parts[2];
            bool   okType   = int.TryParse(parts[3], out int typeIdx);
            bool   okAmount = int.TryParse(parts[4], out int amount);

            if (!okType || !okAmount)
            {
                Debug.LogWarning("[Console] Invalid type or amount. Usage: create promocode <CODE> <TYPE_INDEX> <AMOUNT>");
                continue;
            }

            if (!Enum.IsDefined(typeof(RewardType), typeIdx))
            {
                Debug.LogWarning($"[Console] RewardType index {typeIdx} is not defined.");
                continue;
            }

            var rewardType = (RewardType)typeIdx;

            // все операции с Unity API и вашими MonoBehaviour-скриптами
            // должны идти через главный поток
            _mainThreadContext.Post(_ =>
            {
                // создаём новую запись
                var entry = new PromoCodeEntry
                {
                    code    = code.ToUpperInvariant(),
                    rewards = new List<RewardOption>
                    {
                        new RewardOption
                        {
                            type   = rewardType,
                            amount = amount
                        }
                    }
                };

                // добавляем в менеджер и сохраняем
                PromoCodeManager.Instance.promoCodes.Add(entry);
                PromoCodeManager.Instance.SavePromoCodes();

                Debug.Log($"[Console] Created promo code «{entry.code}» → {rewardType} x{amount}");
            }, null);
        }
    }

    void OnDestroy()
    {
        // при остановке игры завершаем поток
        _consoleThread?.Interrupt();
    }
}
#endif