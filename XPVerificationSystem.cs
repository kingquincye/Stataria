using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class XPVerificationSystem
    {
        private Queue<PendingXPGain> pendingXPGains = new Queue<PendingXPGain>();

        private RPGPlayer rpgPlayer;

        public bool IsVerificationUIActive { get; private set; } = false;

        public PendingXPGain CurrentVerification { get; private set; }

        private int currentVerificationIndex = 0;
        private int totalVerificationCount = 0;

        public XPVerificationSystem(RPGPlayer player)
        {
            rpgPlayer = player;
        }

        public bool IsSuspiciousXPGain(long xpAmount, string source)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.xpVerification.EnableXPVerification)
                return false;

            if (IsSourceWhitelisted(source))
                return false;

            float effectiveXpAmount = xpAmount;

            if (config.rebirthSystem.EnableRebirthSystem && rpgPlayer.RebirthCount > 0)
            {
                float rebirthBonus = 1f + (rpgPlayer.RebirthCount * config.rebirthSystem.RebirthXPMultiplier);
                effectiveXpAmount = xpAmount / rebirthBonus;
            }

            long absoluteThreshold = CalculateAbsoluteThreshold();
            float relativeThreshold = CalculateRelativeThreshold();

            bool exceedsAbsolute = effectiveXpAmount > absoluteThreshold;
            bool exceedsRelative = rpgPlayer.XPToNext > 0 &&
                                (float)effectiveXpAmount / rpgPlayer.XPToNext > relativeThreshold;

            return exceedsAbsolute || exceedsRelative;
        }

        private long CalculateAbsoluteThreshold()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int baseThresholdFromConfig = config.xpVerification.BaseXPThreshold;
            long baseThreshold = baseThresholdFromConfig;

            double levelScaling = 1.0 + (rpgPlayer.Level * (double)config.xpVerification.LevelScalingFactor);

            double rebirthScaling = 1.0 + (rpgPlayer.RebirthCount * (double)config.xpVerification.RebirthScalingFactor);

            long calculatedThreshold = (long)(baseThreshold * levelScaling * rebirthScaling);

            return calculatedThreshold;
        }

        private float CalculateRelativeThreshold()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float baseRelativeThreshold = config.xpVerification.RelativeXPThreshold;

            float rebirthAdjustment = rpgPlayer.RebirthCount * config.xpVerification.RebirthRelativeThresholdReduction;

            return Math.Max(0.1f, baseRelativeThreshold - rebirthAdjustment);
        }

        private bool IsSourceWhitelisted(string source)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            foreach (string whitelistedSource in config.xpVerification.WhitelistedSources)
            {
                if (source.Contains(whitelistedSource))
                    return true;
            }

            return false;
        }

        public void QueueXPForVerification(long amount, string source)
        {
            pendingXPGains.Enqueue(new PendingXPGain(amount, source));

            totalVerificationCount++;

            if (!IsVerificationUIActive)
            {
                currentVerificationIndex = 0;
                StartVerification();
            }
            else
            {
                UpdateVerificationUI();
            }
        }

        private void StartVerification()
        {
            if (pendingXPGains.Count > 0)
            {
                CurrentVerification = pendingXPGains.Dequeue();
                IsVerificationUIActive = true;
                currentVerificationIndex++;

                UpdateVerificationUI();
            }
        }

        private void UpdateVerificationUI()
        {
            XPVerificationUI.ShowVerification(
                CurrentVerification,
                currentVerificationIndex,
                totalVerificationCount,
                AcceptXP,
                RejectXP,
                AcceptAllXP,
                RejectAllXP
            );
        }

        public void AcceptXP()
        {
            if (CurrentVerification != null)
            {
                rpgPlayer.ApplyXPDirectly(CurrentVerification.Amount, CurrentVerification.Source);
                CurrentVerification = null;

                if (pendingXPGains.Count > 0)
                {
                    StartVerification();
                }
                else
                {
                    IsVerificationUIActive = false;
                    totalVerificationCount = 0;
                    currentVerificationIndex = 0;
                    XPVerificationUI.HideVerification();
                }
            }
        }

        public void RejectXP()
        {
            CurrentVerification = null;

            if (pendingXPGains.Count > 0)
            {
                StartVerification();
            }
            else
            {
                IsVerificationUIActive = false;
                totalVerificationCount = 0;
                currentVerificationIndex = 0;
                XPVerificationUI.HideVerification();
            }
        }

        public void AcceptAllXP()
        {
            if (CurrentVerification != null)
            {
                rpgPlayer.ApplyXPDirectly(CurrentVerification.Amount, CurrentVerification.Source);
            }

            while (pendingXPGains.Count > 0)
            {
                var verification = pendingXPGains.Dequeue();
                rpgPlayer.ApplyXPDirectly(verification.Amount, verification.Source);
            }

            CurrentVerification = null;
            IsVerificationUIActive = false;
            totalVerificationCount = 0;
            currentVerificationIndex = 0;
            XPVerificationUI.HideVerification();
        }

        public void RejectAllXP()
        {
            pendingXPGains.Clear();
            CurrentVerification = null;
            IsVerificationUIActive = false;
            totalVerificationCount = 0;
            currentVerificationIndex = 0;
            XPVerificationUI.HideVerification();
        }
    }

    public class PendingXPGain
    {
        public long Amount { get; }
        public string Source { get; }
        public DateTime TimeReceived { get; }

        public PendingXPGain(long amount, string source)
        {
            Amount = amount;
            Source = source;
            TimeReceived = DateTime.Now;
        }

        public string GetFormattedSource()
        {
            return Source.Replace("From: ", "").Trim();
        }
    }
}