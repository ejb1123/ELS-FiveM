﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using GHMatti.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;


namespace ELS_Server
{
    public class ELSServer : BaseScript
    {
        Dictionary<int, object> _cachedData = new Dictionary<int, object>();
        long GameTimer;
        string serverId;
        string currentVersion;
        public ELSServer()
        {
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Utils.ReleaseWriteLine($"Welcome to ELS+ {currentVersion} for FiveM");
            GameTimer = API.GetGameTimer();
            Utils.ReleaseWriteLine($"Setting Game time is {GameTimer}");
            serverId = API.LoadResourceFile(API.GetCurrentResourceName(), "ELSId");
            
            
            if (String.IsNullOrEmpty(serverId))
            {
                Guid uuid = Guid.NewGuid();
                API.SaveResourceFile(API.GetCurrentResourceName(), "ELSId", uuid.ToString(), -1);
                serverId = uuid.ToString();
            }
            foreach (string s in Configuration.ElsVehicleGroups)
            {
                API.ExecuteCommand($"add_ace group.{s} command.elscar allow");
            }
            API.RegisterCommand("vcfrefresh", new Action<int, List<object>, string>((source, arguments, raw) =>
            {
                Utils.ReleaseWriteLine($"{Players[source].Name} has activated a VCF Refresh");
                foreach (Player p in Players)
                {
                    TriggerEvent("ELS:VcfSync:Server", p.Handle);
                }
            }), false);

            API.RegisterCommand("elscar", new Action<int, List<object>, string>(async (source, arguments, raw) =>
            {
                if (arguments.Count < 1 || String.IsNullOrEmpty(arguments[0].ToString()))
                {
                    Utils.ReleaseWriteLine("No vehicle specified please try again");
                    return;
                }
                Utils.ReleaseWriteLine($"{Players[source].Name} has attempted to spawn {arguments[0]}");
                TriggerClientEvent(Players[source], "ELS:SpawnCar", arguments[0]);
            }), Configuration.ElsCarAdminOnly);

            EventHandlers["ELS:VcfSync:Server"] += new Action<int>(async (int source) =>
            {
                Utils.DebugWriteLine($"Sending Data to {Players[source].Name}");
                TriggerClientEvent(Players[source], "ELS:VcfSync:Client", VcfSync.VcfData);
                TriggerClientEvent(Players[source], "ELS:PatternSync:Client", CustomPatterns.Patterns);

            });

            EventHandlers["ELS:FullSync:RemoveStale"] += new Action<int>(async (int netid) =>
            {
                _cachedData.Remove(netid);
                Utils.DebugWriteLine($"Stale vehicle {netid} removed from cache");
            });

            EventHandlers["baseevents:enteredVehicle"] += new Action<int, int, string>((veh, seat, name) =>
              {
                  Utils.DebugWriteLine("Vehicle Entered");
                  TriggerClientEvent("ELS:VehicleEntered", veh);
              });
            EventHandlers["baseevents:leftVehicle"] += new Action<int, int, string>((veh, seat, name) =>
            {
                Utils.DebugWriteLine("Vehicle Entered");
                TriggerClientEvent("ELS:VehicleExited", veh);
            });
            EventHandlers["ELS:FullSync:Unicast"] += new Action(() => { });
            EventHandlers["ELS:FullSync:Broadcast"] += new Action<System.Dynamic.ExpandoObject, Int16>((dataDic, playerID) =>
             {
                 var dd = (IDictionary<string, object>)dataDic;
                 Utils.DebugWriteLine($"NetworkID {dd["NetworkID"]}");
                 _cachedData[int.Parse(dd["NetworkID"].ToString())] = dd;
                 BroadcastMessage(dataDic, playerID);
             });
            EventHandlers["ELS:FullSync:Request:All"] += new Action<int>((int source) =>
            {
                Utils.DebugWriteLine($"{source} is requesting Sync Data");
                TriggerClientEvent(Players[source], "ELS:FullSync:NewSpawnWithData", _cachedData);
            });
            API.RegisterCommand("resync", new Action<int, System.Collections.IList, string>((a, b, c) =>
            {
                Utils.DebugWriteLine($"{a}, {b}, {c}");
                TriggerClientEvent(Players[(int.Parse((string)b[0]))], "ELS:FullSync:NewSpawnWithData", _cachedData);
            }), false);
            API.RegisterCommand("clearcache", new Action<int, System.Collections.IList, string>((a, b, c) =>
            {
                _cachedData.Clear();
                Utils.ReleaseWriteLine("ELS Cache cleared");
            }), false);

            Task task = new Task(() => PreloadSyncData());
            task.Start();
            Tick += Server_Tick;
            API.SetConvarReplicated("ELSServerId", serverId);
            Task updateCheck = new Task(() => CheckForUpdates());
            updateCheck.Start();
        }

        async Task PreloadSyncData()
        {
            await VcfSync.CheckResources();
            await CustomPatterns.CheckCustomPatterns();
        }

        async Task CheckForUpdates()
        {
            Utils.ReleaseWriteLine("Checking for ELS+ Updates in 5");
            await ELSServer.Delay(1000);
            Utils.ReleaseWriteLine("Checking for ELS+ Updates in 4");
            await ELSServer.Delay(1000);
            Utils.ReleaseWriteLine("Checking for ELS+ Updates in 3");
            await ELSServer.Delay(1000);
            Utils.ReleaseWriteLine("Checking for ELS+ Updates in 2");
            await ELSServer.Delay(1000);
            Utils.ReleaseWriteLine("Checking for ELS+ Updates in 1");
            await ELSServer.Delay(1000);
#if DEBUG 
            string updateUrl = "http://localhost:3000";
#else
            string updateUrl = "http://els-stats.friendsincode.com:3000";
#endif
            Request request = new Request();
            //JObject data = new JObject();
            //data["serverId"] = serverId;
            //data["serverName"] = API.GetConvar("sv_hostname", $"ELS Plus Server {serverId}");
            RequestResponse result = await request.Http(updateUrl,"POST", 
                $"serverId={serverId}&serverName={API.GetConvar("sv_hostname",$"ELS Plus Server {serverId}")}");
            switch (result.status)
            {
                case HttpStatusCode.NotFound:
                    Utils.ReleaseWriteLine("ELS Update page not found please try again");
                    break;
                case HttpStatusCode.OK:
                    JObject res = JObject.Parse(result.content);
                    if (((string)res["current"]).Equals(currentVersion))
                    {
                        Utils.ReleaseWriteLine("Thank you for using ELS Plus, currently running latest stable version");
                    }
                    else if (((string)res["dev"]).Equals(currentVersion))
                    {
                        Utils.ReleaseWriteLine("Thank you for using ELS Plus, currently running latest development version");
                    }
                    else
                    {
                        Utils.ReleaseWriteLine($"ELS Plus is not up to date please update please update from version {currentVersion} to {(string)res["current"]}");
                    }
                    break;
                case HttpStatusCode.RequestTimeout:
                    Utils.ReleaseWriteLine("ELS Connection timed out please try again");
                    break;
            }

        }

        void BroadcastMessage(System.Dynamic.ExpandoObject dataDic, int SourcePlayerID)
        {
            TriggerClientEvent("ELS:NewFullSyncData", dataDic, SourcePlayerID);
        }

        private async Task Server_Tick()
        {
            if (API.GetGameTimer() >= GameTimer + Configuration.CacheClear)
            {
                _cachedData.Clear();
                GameTimer = API.GetGameTimer();
            }
        }
    }
}