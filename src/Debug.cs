﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using System.Drawing;

namespace ELS
{
    class Debug
    {
        internal static async Task Spawn()
        {
            if (!Game.PlayerPed.IsInVehicle())
            {
                var polModel = new Model((VehicleHash)Game.GenerateHash("policegt350r"));
                await polModel.Request(-1);
                Vehicle veh = await World.CreateVehicle(polModel, Game.PlayerPed.Position);
                polModel.MarkAsNoLongerNeeded();
                //veh.RegisterAsNetworked();
                Screen.ShowNotification($"network status {Function.Call<bool>(Hash.NETWORK_GET_ENTITY_IS_NETWORKED, veh)}");
                veh.SetExistOnAllMachines(true);
                await CitizenFX.Core.BaseScript.Delay(10000);
                CitizenFX.Core.Debug.WriteLine($"vehtonet{API.VehToNet(veh.Handle)} getnetworkidfromentity{API.NetworkGetNetworkIdFromEntity(veh.Handle)}");
                CitizenFX.Core.Debug.WriteLine($"ModelName {veh.Model}" +
                    $"DisplayName {veh.DisplayName}");
                if (veh == null)
                {
                    CitizenFX.Core.Debug.WriteLine("failure to spawn");
                    return;
                }
            }
            else //if (Game.PlayerPed.CurrentVehicle.IsEls())
            {
                var veh = Game.PlayerPed.CurrentVehicle;
                for(var x =0; x < 24; x++)
                {
                    if (veh.ExtraExists(x))
                    {
                        veh.ToggleExtra(x, !veh.IsExtraOn(x));
                    }
                    }
                veh.ToggleExtra(1, true);
            }
            // Game.Player.Character.SetIntoVehicle(veh, VehicleSeat.Any);
        }
        internal static void DebugText()
        {
#if DEBUG
            if (Game.Player.Character.LastVehicle == null) return;
            var bonePos = Game.Player.Character.LastVehicle.Bones["door_dside_f"].Position;
            var pos = Game.Player.Character.GetPositionOffset(bonePos);
            var text = new Text($"X:{pos.X} Y:{pos.Y} Z:{pos.Z} Lenght:{pos.Length()}", new PointF(Screen.Width / 2.0f, 10f), 0.5f);
            text.Alignment = Alignment.Center;
            if (pos.Length() < 1.5) text.Color = Color.FromArgb(255, 0, 0);
            text.Draw();
#endif
        }
    }
}