// Copyright (c) Rayze. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot.Coroutines;
using Styx.Pathing;
using Styx.Plugins;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace MeleeHelper
{
    public class MeleeHelper : HBPlugin
    {
        private static Coroutine s_mainCoroutine;

        public override string Author => "Rayze";

        public static string ProductName => "Melee Helper";

        public override string Name => ProductName;

        public static Version ProductVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        public override Version Version => ProductVersion;

        private static LocalPlayer Me => StyxWoW.Me;

        private static Composite Hook { get; } = new ActionRunCoroutine(cr => Check());

        public override bool WantButton => false;

        private static bool s_dismount;

        private static System.Numerics.Vector3 s_targetLocation => Me.CurrentTarget.Location;

        public override void Pulse()
        {
            if (s_mainCoroutine == null || s_mainCoroutine.IsFinished)
            {
                s_mainCoroutine = new Coroutine(async () => await MainCoroutine());
            }

            try
            {
                s_mainCoroutine.Resume();
            }
            catch (Exception ex) when (!(ex is CoroutineStoppedException))
            {
            }
        }


        public override void OnDisable()
        {
            TreeHooks.Instance.RemoveHook("Combat_OOC", Hook);
        }

        public override void OnEnable()
        {
            Logging.OnLogMessage += Logging_OnLogMessage;
            TreeHooks.Instance.InsertHook("Combat_OOC", 0, Hook);
        }

        private void Logging_OnLogMessage(System.Collections.ObjectModel.ReadOnlyCollection<Logging.LogMessage> messages)
        {
            foreach (Logging.LogMessage _row in messages)
            {
                if (_row.Message.Contains("Dismount to kill bot poi."))
                    s_dismount = true;
            }
        }

        [SuppressMessage("ReSharper", "FunctionNeverReturns", Justification = "Don't care.")]
        private static async Task<bool> MainCoroutine()
        {

            if (!ShouldRun())
                return false;            
            if (!await HandleDismount())
                return false;
            return false;
        }

        private static bool ShouldRun()
        {
            if (Me.IsDead || !Me.IsMelee || !Me.Mounted || Me.CurrentTarget == null || !s_dismount)
                return false;
            return true;
        }

        private static async Task<bool> Check()
        {
            await Coroutine.Yield();
            if (ShouldRun())
                return true;
            return false;
        }  
        
        private static async Task<bool> HandleDismount()
        {
            Navigator.Clear();
            Flightor.Clear();

            while (Me.Mounted)
            {
                if (Me.Location.Distance(s_targetLocation) > 5)
                    await Coroutine.Wait(20000,() => Flightor.MoveTo(s_targetLocation) == MoveResult.ReachedDestination);
                else
                {
                    await CommonCoroutines.LandAndDismount(null, true, s_targetLocation);
                    await CommonCoroutines.StopMoving();

                    var _target = Me.CurrentTarget;
                    if (_target != null)
                        Lua.DoString("StartAttack()");
                }
                //await Coroutine.Yield();
            }            
            return true;
        }
    }
}
