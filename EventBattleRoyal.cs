using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Oxide.Ext.HurtworldSystem;

namespace Oxide.Plugins
{
    [Info("BattleRoyal", "eXotic", "1.4.0")]
    [Description("Automatic battle royal event for Hurtworld Legacy.")]

    class BattleRoyal : HurtworldPlugin
    {
        bool IsEqSaved, isStarted, isQueOpen = false;

        Dictionary<int, int> ItemList = new Dictionary<int, int>();
        List<Location> SpawnPoints = new List<Location>();
        GlobalItemManager GIM = Singleton<GlobalItemManager>.Instance;
        List<uLink.NetworkView> ChestList = new List<uLink.NetworkView>();
        List<BRPlayer> brplayers = new List<BRPlayer>();
        List<BREqSave> EqSavePlayers = new List<BREqSave>();

        public class BREqSave
        {
            public PlayerSession session { get; set; }
            public ulong steamid { get; set; }   
            public Dictionary<int, ItemInstance> inv { get; set; }

            public BREqSave(PlayerSession session, ulong steamid, Dictionary<int, ItemInstance> inv)
            {
                this.session = session;
                this.steamid = steamid;
                this.inv = inv;
            } 

            public BREqSave(PlayerSession session)
            {
                this.session = session;
                this.steamid = (ulong)session.SteamId;
                this.inv = GetPlayerItems(session);
            }
        }

        public class BRPlayer
        {
            public PlayerSession session { get; set; }
            public ulong steamid { get; set; }
            public Vector3 homeposition { get; set; }
            public float infamy { get; set; }

            public BRPlayer(PlayerSession session, ulong steamid, Vector3 homeposition, float infamy)
            {
                this.session = session;
                this.steamid = steamid;
                this.homeposition = homeposition;
                this.infamy = infamy;
            }

            public BRPlayer(PlayerSession session)
            {
                this.session = session;
                this.steamid = (ulong)session.SteamId;
                this.homeposition = session.WorldPlayerEntity.transform.position;
                this.infamy = inf(session);
            }
        }

        class Location
        {
            public float x;
            public float y;
            public float z;

            public Location(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        //Prefix on chat
        string brpref = "<color=#996600>[BATTLEROYAL]</color>";
        string brpref2 = "<color=#996600>»</color>";
        string brprefadm = "<color=red>»</color>";
        protected override void LoadDefaultMessages()
        {
            //English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"BR_unknowm_cmd", "Unknown command."},
                {"BR_no_perm", "You don't have permission to use that command."},
                {"BR_nearly_start", "Queue is now open! Event will start in {time} seconds! (/br join)"},
                {"BR_started", "Event started, gl hf!"},
                {"BR_noplayers", "Event didn't start. Not enought players."},
                {"BR_notstarted", "Queue is not open yet!"},
                {"BR_finish", "Event is over! Winner: <i>{player}</i>"},
                {"BR_finish_no", "Event is over! For unexplained reasons, no one won"},
                {"BR_cmd", "Available commands:"},
                {"BR_cmda", "/br join - join to event queue"},
                {"BR_cmdb", "/br info - info about plugin"},
                {"BR_cmdc", "/br zapisy - turn on/turn off event queue"},
                {"BR_cmdd", "/br start - manual event start (don't start without sign players)"},
                {"BR_cmde", "/br stop - manual event over (no one win)"},
                {"BR_cmdf", "/br kick <nick> - kick player from event"},
                {"BR_cmdg", "/br add <nick> - add and teleport player to event"},
                {"BR_info", "<size=14>Plugin settings: min. {minplayers} players on server to start event, {slots} slots on event, queue is open for {jointime} seconds, event is started every {interval} minutes. Author: eXotic.</size>"},
                {"BR_fail", "Chest with guns spawn error"},
                {"BR_notice_death", "You are dead."},
                {"BR_notice_rm", "You have <color=#ff0000>RaidMode</color> effect."},
                {"BR_notice_cb", "You have <color=#ff6600>Combat</color> effect."},
                {"BR_join", "You are already in queue"},
                {"BR_full", "Queue is full."},
                {"BR_leave", "You are out."},
                {"BR_que_open", "Queue is open now by admin. (/br join)"},
                {"BR_que_lock", "Queue is locked now by admin."},
                {"BR_already_started", "Event started already, you can't join anymore."},
                {"BR_already_in", "You are already in event queue."},
                {"BR_already_event", "Event is already started, first finish previous one."},
                {"BR_no_event", "There is no event going on at the moment."},
                {"BR_count", "{count} players left!"},
                {"BR_unknown_player", "Uknown player."},
                {"BR_kick", "You kicked <i>{player}</i> from event."},
                {"BR_got_kicked", "You got kicked from event."},
                {"BR_player_on_event", "Player is already in event"},
                {"BR_add", "You added <i>{player}</i> to event."},
                {"BR_got_added", "You got added to event."}
            }, this);

            //Polish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"BR_unknowm_cmd", "Niepoprawna składnia komendy."},
                {"BR_no_perm", "Nie posiadasz uprawnień do użycia tej komendy."},
                {"BR_nearly_start", "Zapisy otwarte! Event wystartuje za {time} sekund! (/br join - żeby dołączyć)"},
                {"BR_started", "Event wystartował, gl hf!"},
                {"BR_noplayers", "Event nie wystartował z powodu małej ilości zapisów!"},
                {"BR_notstarted", "Zapisy na event nie zostały jeszcze otwarte!"},
                {"BR_finish", "Event zakończony! Zwycięzca: <i>{player}</i>"},
                {"BR_finish_no", "Event zakończony! Z niewyjaśnionych przyczyn, nikt nie wygrał."},
                {"BR_cmd", "Dostępne komendy:"},
                {"BR_cmda", "/br join - dołączenie do eventu"},
                {"BR_cmdb", "/br info - informacje o pluginie"},
                {"BR_cmdc", "/br zapisy - włącza/wyłącza zapisy na event"},
                {"BR_cmdd", "/br start - startuje event manualnie (nie odpalać bez zapisanych graczy)"},
                {"BR_cmde", "/br stop - manualnie kończy event (nikt nie wygrywa)"},
                {"BR_cmdf", "/br kick <nick> - wyrzuca gracza z eventu"},
                {"BR_cmdg", "/br add <nick> - dodaje i teleportuje gracza do eventu"},
                {"BR_info", "<size=14>Ustawienia pluginu: min. {minplayers} wymaganych graczy na serwerze do startu, {slots} miejsc na evencie, zapisy trwają {jointime} sekund, event cykliczny co {interval} minut. Autor pluginu: eXotic.</size>"},
                {"BR_fail", "Błąd respienia skrzynek z broniami"},
                {"BR_notice_death", "Nie żyjesz."},
                {"BR_notice_rm", "Masz efekt <color=#ff0000>RaidMode</color>."},
                {"BR_notice_cb", "Masz efekt <color=#ff6600>Combat</color>."},
                {"BR_join", "Zapisałeś się na event."},
                {"BR_full", "Nie ma już wolnych miejsc."},
                {"BR_leave", "Odpadasz z eventu."},
                {"BR_que_open", "Zapisy na event zostały otwarte przez admina. (/br join - żeby dołączyć)"},
                {"BR_que_lock", "Zapisy na event zostały zamknięte przez admina."},
                {"BR_already_started", "Event już wystartował, nie można się już na niego zapisać."},
                {"BR_already_in", "Jesteś już zapisany na event."},
                {"BR_already_event", "Aktualnie trwa event, najpierw zakończ poprzedni."},
                {"BR_no_event", "Aktualnie nie trwa żaden event."},
                {"BR_count", "Pozostało: {count} graczy!"},
                {"BR_unknown_player", "Nie znaleziono gracza."},
                {"BR_kick", "Wyrzuciłeś gracza <i>{player}</i> z eventu."},
                {"BR_got_kicked", "Zostałeś wyrzucony z eventu."},
                {"BR_player_on_event", "Gracz jest już na evencie."},
                {"BR_add", "Dodałeś gracza <i>{player}</i> do eventu."},
                {"BR_got_added", "Zostałeś dodany do eventu."}
            }, this, "pl");
        }

        protected override void LoadDefaultConfig()
        {
            var Locs = new List<object>() { 
                "-3803, 2653, -3564",
                "-3822, 2652, -3574",
                "-3791, 2652, -3603",
                "-3848, 2646, -3627",
                "-3860, 2642, -3624",
                "-3872, 2646, -3556",
                "-3751, 2646, -3569",
                "-3872, 2646, -3673",
                "-3693, 2646, -3545",
                "-3701, 2646, -3583",
                "-3713, 2646, -3613",
                "-3718, 2646, -3620",
                "-3764, 2646, -3664",
                "-3767, 2646, -3750",
                "-3875, 2646, -3819",
                "-3880, 2646, -3811",
                "-3738, 2646, -3755",
                "-3631, 2646, -3659",
                "-3571, 2646, -3678",
                "-3582, 2646, -3673",
                "-3705, 2650, -3705",
                "-3715, 2647, -3661",
                "-3716, 2660, -3659",
                "-3704, 2660, -3694",
                "-3631, 2659, -3666",
                "-3749, 2646, -3634",
                "-3794, 2646, 2692",
                "-3642, 2646, -3550",
                "-3608, 2646, -3692",
                "-3587, 2651, -3560",
                "-3556, 2646, -3570",
                "-3550, 2646, -3553",
                "-3548, 2651, -3584",
                "-3586, 2651, -3608",
                "-3548, 2646, -3655",
                "-3575, 2664, -3674",
                "-3562.5, 2646, -3705",
                "-3589, 2658, -3730",
                "-3584, 2646, -3740",
                "-3598, 2648, -3760",
                "-3552, 2646, -3794",
                "-3624, 2652, -3819",
                "-3642, 2650, -3794",
                "-3715, 2649, -3820",
                "-3668, 2646, -3776",
                "-3620, 2657, -3772",
                "-3763, 2646, -3778",
                "-3774, 2653, -3795",
                "-3789, 2652, -3819",
                "-3839, 2649, -3773",
                "-3876, 2655, -3767",
                "-3851, 2659, -3709",
                "-3822, 2650, -3718",
                "-3873, 2646, -3735",
                "-3784, 2646, -3770"
            };
            var Ammo = new List<object>(){
                "48, 50", 
                "278, 250",
                "280, 200", 
                "52, 200", 
                "191, 200" 
            };
            PrintWarning("Creating a configuration file for " + this.Title);
            Config.Clear();
            Config["awardID"] = 144;
            Config["awardAmount"] = 1;
            Config["minPlayersToStart"] = 2;
            Config["eventSlots"] = 3;
            Config["eventIntervalMinutes"] = 3;
            Config["startTimeSeconds"] = 5;
            Config["ChestSpawnPoints"] = Locs;
            Config["AmmoAtStartEvent"] = Ammo;
            SaveConfig();
        }
        private string Msg(string msg, object SteamId = null) => lang.GetMessage(msg, this, SteamId?.ToString());
        
        void LoadPermissions()
        {
            if (!permission.PermissionExists("battleroyal.admin"))
                permission.RegisterPermission("battleroyal.admin", this);
        }

        void Init()
        {
            ItemList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<int, int>>("BattleRoyal/ItemList");
            SpawnPoints = Interface.GetMod().DataFileSystem.ReadObject<List<Location>>("BattleRoyal/SpawnPoints");
            LoadDefaultMessages();
            LoadPermissions();
        }

        void Loaded()
        {
            timer.Repeat(Convert.ToSingle(Config["eventIntervalMinutes"])*60, 0, () =>
            {
                var allPlayers = GameManager.Instance.GetSessions().Values.ToList();
                if (allPlayers.Count() >= Convert.ToSingle(Config["minPlayersToStart"]))
                {
                    if(isStarted == false)
                    {
                        isQueOpen = true;
                        StartEvent();
                    }
                }
            });
        }


        [ChatCommand("br")]
        void cmdBr(PlayerSession session, string command, string[] args)
        {
            if (args.Length == 0)
            {
                if(isStarted == true)
                {
                    var c = brplayers.Count;
                    hurt.SendChatMessage(session, brpref, Msg("BR_count", session.SteamId).Replace("{count}",c.ToString()));
                } 
                else
                { 
                    hurt.SendChatMessage(session, brpref, Msg("BR_cmd", session.SteamId));
                    hurt.SendChatMessage(session, brpref2, Msg("BR_cmda", session.SteamId));
                    hurt.SendChatMessage(session, brpref2, Msg("BR_cmdb", session.SteamId));
                    if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin"))hurt.SendChatMessage(session, brprefadm, Msg("BR_cmdc", session.SteamId));
                    if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin"))hurt.SendChatMessage(session, brprefadm, Msg("BR_cmdd", session.SteamId));
                    if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin"))hurt.SendChatMessage(session, brprefadm, Msg("BR_cmde", session.SteamId));
                    if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin"))hurt.SendChatMessage(session, brprefadm, Msg("BR_cmdf", session.SteamId));
                    if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin"))hurt.SendChatMessage(session, brprefadm, Msg("BR_cmdg", session.SteamId));
                }
            }
            else if(args.Length == 1)
            {
                switch (args[0])
                {
                    case "info":
                        hurt.SendChatMessage(session, brpref, Msg("BR_info", session.SteamId).Replace("{minplayers}",Config["minPlayersToStart"].ToString()).Replace("{slots}",Config["eventSlots"].ToString()).Replace("{jointime}",Config["startTimeSeconds"].ToString()).Replace("{interval}",Config["eventIntervalMinutes"].ToString()));
                    break;
                    case "join":
                        if(brplayers.Count >= Convert.ToSingle(Config["eventSlots"]) && isStarted == false)
                        {
                            hurt.SendChatMessage(session, brpref, Msg("BR_full", session.SteamId));
                            return;
                        }
                        else if(isStarted == true)
                        {
                            hurt.SendChatMessage(session, brpref, Msg("BR_already_started", session.SteamId));
                            return;
                        }
                        else if(isQueOpen == false)
                        {
                            hurt.SendChatMessage(session, brpref, Msg("BR_notstarted", session.SteamId));
                            return;
                        }
                        else
                        {
                            var prov = FindGP(session).Count();
                            if (prov == 0)
                            {
                                if (!CheckJoin(session))
                                    return;
                                brplayers.Add(new BRPlayer(session));
                                EqSavePlayers.Add(new BREqSave(session));
                                hurt.SendChatMessage(session, brpref, Msg("BR_join", session.SteamId));
                                if(CheckToStart())
                                {
                                    StartEvent();
                                }
                                return;
                            }
                            else
                            {
                                hurt.SendChatMessage(session, brpref, Msg("BR_already_in", session.SteamId));
                                return;
                            }
                        }
                    break;
                    // admin cmd
                    case "start":
                        if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin")){
                            if(isStarted == false) {StartEvent();}
                            else{hurt.SendChatMessage(session, brpref, Msg("BR_already_event", session.SteamId));}
                            return;
                        }
                        else {
                            hurt.SendChatMessage(session, brpref, Msg("BR_no_perm", session.SteamId));
                            return;
                        }
                    break;
                    case "zapisy":
                        if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin")){
                            if(isQueOpen == false){
                                isQueOpen = true; 
                                //Server.Broadcast(Msg("BR_que_open"));
                                this.BroadcastInLang(brpref, "BR_que_open", null);
                                return; 
                            } 
                            if(isQueOpen == true){
                                isQueOpen = false; 
                                //Server.Broadcast(Msg("BR_que_lock"));
                                this.BroadcastInLang(brpref, "BR_que_lock", null);
                                return;
                            }
                        }
                        else {
                            hurt.SendChatMessage(session, brpref, Msg("BR_no_perm", session.SteamId));
                            return;
                        }
                    break;
                    case "stop":
                        if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin")){
                            if(isStarted == true) {FinishEventNoWinners();}
                            else{hurt.SendChatMessage(session, brpref, Msg("BR_no_event", session.SteamId));}
                        }
                        else {
                            hurt.SendChatMessage(session, brpref, Msg("BR_no_perm", session.SteamId));
                            return;
                        }
                    break;
                    default:
                        hurt.SendChatMessage(session, brpref, Msg("BR_unknowm_cmd", session.SteamId));
                    break;
                }
            }
            else if(args.Length == 2)
            {
                switch (args[0])
                {
                    case "kick":
                        if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin")){
                            var target = getSession(args[1]);
                            if(target == null)
                            {
                                hurt.SendChatMessage(session, brpref, Msg("BR_unknown_player", session.SteamId));
                                return;
                            }
                            BRPlayer par = FindGP(target).FirstOrDefault();
                            if(par == null)                            
                            {
                                hurt.SendChatMessage(session, brpref, Msg("BR_unknown_player", session.SteamId));
                                return;
                            }
                            exittp(par.session);
                            hurt.SendChatMessage(session, brpref, Msg("BR_kick", session.SteamId).Replace("{player}",target.Identity.Name));
                            hurt.SendChatMessage(par.session, brpref, Msg("BR_got_kicked", par.steamid));
                            brplayers.Remove(par);
                            if(brplayers.Count == 0){
                                FinishEventNoWinners();
                            }
                            if(brplayers.Count == 1)
                            {
                                foreach (BRPlayer bp in brplayers)
                                {
                                    if(bp.session != null){
                                        FinishEvent(bp.session);
                                    } else {
                                        FinishEventNoWinners();
                                    }
                                }
                            }
                        }
                        else {
                            hurt.SendChatMessage(session, brpref, Msg("BR_no_perm", session.SteamId));
                            return;
                        }
                    break;
                        case "add":
                        if (permission.UserHasPermission(session.SteamId.ToString(), "battleroyal.admin")){
                            var target = getSession(args[1]);
                            if(target == null)
                            {
                                hurt.SendChatMessage(session, brpref, Msg("BR_unknown_player", session.SteamId));
                                return;
                            }
                            BRPlayer par = FindGP(target).FirstOrDefault();
                            if(par != null)                            
                            {
                                hurt.SendChatMessage(session, brpref, Msg("BR_player_on_event", session.SteamId));
                                return;
                            }
                            if (!CheckJoin(target)) {return;}
                            if(isQueOpen == false && isStarted == true)
                            {
                                brplayers.Add(new BRPlayer(target));
                                EqSavePlayers.Add(new BREqSave(target));
                                BREqSave breq = FindGPP(target).FirstOrDefault();
                                BRPlayer br = FindGP(target).FirstOrDefault();
                                hurt.SendChatMessage(session, brpref, Msg("BR_add", session.SteamId).Replace("{player}",target.Identity.Name));
                                hurt.SendChatMessage(br.session, brpref, Msg("BR_got_added", br.steamid));
                                GetPlayerItems(breq.session);
                                InvClear(breq.session);
                                Heal(br.session);
                                int SpawnID = UnityEngine.Random.Range(0, SpawnPoints.Count);
                                Teleport(br.session, new Vector3(SpawnPoints[SpawnID].x, SpawnPoints[SpawnID].y, SpawnPoints[SpawnID].z));
                                GiveAmmo(br.session);
                            }
                        }
                        else {
                            hurt.SendChatMessage(session, brpref, Msg("BR_no_perm", session.SteamId));
                            return;
                        }
                    break;
                    default:
                        hurt.SendChatMessage(session, brpref, Msg("BR_unknowm_cmd", session.SteamId));
                    break;
                }
            }
        }

        bool CheckToStart(){
            if(brplayers.Count >= Convert.ToSingle(Config["eventSlots"]))
            {
                return true;
            } 
            return false;
        }

        void StartEvent(){
            //Server.Broadcast(Msg("BR_nearly_start").Replace("{time}",Config["startTimeSeconds"].ToString()));
            this.BroadcastInLang(brpref, "BR_nearly_start", new Dictionary<string, string> { { "{time}", Config["startTimeSeconds"].ToString() } });
            timer.Once(Convert.ToSingle(Config["startTimeSeconds"]), () =>
			{
                if(brplayers.Count > 1)
                {
                    isQueOpen = false;
                    isStarted = true;
                    IsEqSaved = true;
                    //Server.Broadcast(Msg("BR_started"));
                    this.BroadcastInLang(brpref, "BR_started", null);
                    GunChestSpawns();
                    foreach (BREqSave breq in EqSavePlayers)
                    {
                        GetPlayerItems(breq.session);
                        InvClear(breq.session);
                    }
                    foreach (BRPlayer br in brplayers)
                    {
                        Heal(br.session);
                        int SpawnID = UnityEngine.Random.Range(0, SpawnPoints.Count);
                        Teleport(br.session, new Vector3(SpawnPoints[SpawnID].x, SpawnPoints[SpawnID].y, SpawnPoints[SpawnID].z));
                        GiveAmmo(br.session);
                    }
                } else {
                    isQueOpen = false;
                    isStarted = false;
                    IsEqSaved = false;
                    //Server.Broadcast(Msg("BR_noplayers"));
                    this.BroadcastInLang(brpref, "BR_noplayers", null);
                    brplayers.Clear();
                    EqSavePlayers.Clear();
                }
			});
        } 

        void FinishEvent(PlayerSession session){
            NextFrame(() => 
            {
                isStarted = false;
                //Server.Broadcast(Msg("BR_finish").Replace("{player}",session.Identity.Name));
                this.BroadcastInLang(brpref, "BR_finish", new Dictionary<string, string> { { "{player}", session.Identity.Name } });
                foreach (BRPlayer bp in brplayers)
                {
                    if(bp.session != null){
                        exittp(bp.session);
                        GiveAward(bp.session);
                    }   
                }
                brplayers.Clear();
                foreach(uLink.NetworkView nwv in ChestList)
                {
                    Singleton<HNetworkManager>.Instance.NetDestroy(nwv);
                }
            });
        }   

        void FinishEventNoWinners(){
            NextFrame(() => 
            {
                isStarted = false;
                //Server.Broadcast(Msg("BR_finish_no"));
                this.BroadcastInLang(brpref, "BR_finish_no", null);
                foreach (BRPlayer bp in brplayers)
                {
                    if(bp.session != null){
                        exittp(bp.session);
                    }   
                }
                brplayers.Clear();
                foreach(uLink.NetworkView nwv in ChestList)
                {
                    Singleton<HNetworkManager>.Instance.NetDestroy(nwv);
                }
            });
        }  

        void exittp(PlayerSession session){
            if (session.WorldPlayerEntity != null)
            {
                InvClear(session);
                BRPlayer par = FindGP(session).FirstOrDefault();
                BREqSave eqpar = FindGPP(session).FirstOrDefault();
                Teleport(session, par.homeposition);
                infset(session);
                RefundItems(session);
                EqSavePlayers.Remove(eqpar);
                if(EqSavePlayers.Count == 0)
                {
                    IsEqSaved = false;
                    EqSavePlayers.Clear();
                }
            }
        }

        void GiveAward(PlayerSession session)
        {
            GIM.GiveItem(session.Player, GIM.GetItem(Convert.ToInt32(Config["awardID"])), Convert.ToInt32(Config["awardAmount"]));
        }
 
        void GunChestSpawns()
		{
            string chest = "LootCache";
            if(ItemList.Count > 0)
			{
                //spawn pointy skrzynek w configu
                var LocList = Config.Get<List<string>>("ChestSpawnPoints");
                foreach(string Loc in LocList)
				{
                    string[] XYZ = Loc.ToString().Split(',');
                    Vector3 position = new Vector3(Convert.ToSingle(XYZ[0]),Convert.ToSingle(XYZ[1]),Convert.ToSingle(XYZ[2]));
                    RaycastHit hitInfo;
                    if (Physics.Raycast(position, Vector3.down, out hitInfo))
                    {
                        Quaternion rotation = Quaternion.Euler(0.0f, (float)UnityEngine.Random.Range(0f, 360f), 0.0f);
                        rotation = Quaternion.FromToRotation(Vector3.down, hitInfo.normal) * rotation;		
                        if(!hitInfo.collider.gameObject.name.Contains("UV") && !hitInfo.collider.gameObject.name.Contains("Cliff") && !hitInfo.collider.gameObject.name.Contains("Zone") && !hitInfo.collider.gameObject.name.Contains("Cube") && !hitInfo.collider.gameObject.name.Contains("Build") && !hitInfo.collider.gameObject.name.Contains("Road") && !hitInfo.collider.gameObject.name.Contains("MeshColliderGroup"))
                        {
                            GameObject Obj = Singleton<HNetworkManager>.Instance.NetInstantiate(chest, hitInfo.point, Quaternion.identity, GameManager.GetSceneTime());
                            if(Obj != null)
                            {
                                Inventory inv = Obj.GetComponent<Inventory>() as Inventory;
                                if(inv.Capacity < 1)
                                    inv.ChangeCapacity(1);
                                uLink.NetworkView nwv = uLink.NetworkView.Get(Obj);
                                ChestList.Add(nwv);
                                inv.DestroyOnEmpty = true;
                                GiveGuns(inv);
                                //Destroy(nwv);
                            }
                            else
                            {
                                //Server.Broadcast(Msg("BR_fail"));
                                this.BroadcastInLang(brpref, "BR_fail", null);
                                return;
                            }
                        }
                    }
                }
            }
        }

        void GiveGuns(Inventory inv)
		{
			if(ItemList.Count > 0)
			{
                int	rand = UnityEngine.Random.Range(0, ItemList.Count-1);
                var item = GIM.GetItem((int)ItemList.ElementAt(rand).Key);
                GIM.GiveItem(item, ItemList.ElementAt(rand).Value, inv);
			}
		}

/*      funkcja do usuwania skrzynek z broniami po pewnym czasie

        void Destroy(uLink.NetworkView nwv)
		{
			timer.Once(Convert.ToSingle(Config["GunsChestSecondsTillDestroy"]), () =>
			{
				if(nwv != null)
				{
					ChestList.Remove(nwv);
					Singleton<HNetworkManager>.Instance.NetDestroy(nwv);
				}
			});
		}
*/        
        void Unload()
        {
            if(isStarted == true || isQueOpen == true)
            {  
                isQueOpen = false;
                FinishEventNoWinners();
            }
            //czyszczenie zespawnowanych skrzynek z broniami
            foreach(uLink.NetworkView nwv in ChestList)
			{
				Singleton<HNetworkManager>.Instance.NetDestroy(nwv);
			}
        }

        public static float inf(PlayerSession session)
        {
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            float a = stats.GetFluidEffect(EEntityFluidEffectType.Infamy).GetValue();
            return a;
        }

        void Heal(PlayerSession session)
        {
            EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            stats.GetFluidEffect(EEntityFluidEffectType.Infamy).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.ColdBar).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.Radiation).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.HeatBar).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.Dampness).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.Hungerbar).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.Nutrition).SetValue(100f);
            stats.GetFluidEffect(EEntityFluidEffectType.InternalTemperature).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.ExternalTemperature).Reset(true);
            stats.GetFluidEffect(EEntityFluidEffectType.Toxin).SetValue(0f);
            stats.GetFluidEffect(EEntityFluidEffectType.Health).SetValue(100f);
            stats.RemoveBinaryEffect(EEntityBinaryEffectType.BrokenLeg);
            stats.RemoveBinaryEffect(EEntityBinaryEffectType.Wet);
        }
        
        void Teleport(PlayerSession session, Vector3 location)
        {
            session.WorldPlayerEntity.transform.position = location;
        }

        void RefreshInventory(PlayerSession target)
        {
            GIM.GiveItem(target.Player, GIM.GetItem(22), 0);
        }

        IEnumerable<BRPlayer> FindGP(PlayerSession session)
        {
            return (from x in brplayers where x.steamid == (ulong)session.SteamId select x);
        }

        IEnumerable<BREqSave> FindGPP(PlayerSession session)
        {
            return (from x in EqSavePlayers where x.steamid == (ulong)session.SteamId select x);
        }

        bool CheckJoin(PlayerSession session)
        {
            var statsa = session.WorldPlayerEntity.GetComponent<EntityStats>();
            float a = statsa.GetFluidEffect(EEntityFluidEffectType.Health).GetValue();
            if (a == 0)
            {
                hurt.SendChatMessage(session, brpref, Msg("BR_notice_death", session.SteamId));
                return false;
            }
            bool rb = statsa.HasBinaryEffect(EEntityBinaryEffectType.RaidMode);
            if(rb)
            {
                hurt.SendChatMessage(session, brpref, Msg("BR_notice_rm", session.SteamId));
                return false;
            }
            rb = statsa.HasBinaryEffect(EEntityBinaryEffectType.Combat);
            if(rb)
            {
                hurt.SendChatMessage(session, brpref, Msg("BR_notice_cb", session.SteamId));
                return false;
            }
            return true;
        }

        void OnPlayerDisconnected(PlayerSession session)
        {
            if(isStarted == true)
            {
                var prov = FindGP(session).Count();
                if (prov == 0) return;
                InvClear(session);
                BRPlayer par = FindGP(session).FirstOrDefault();
                BREqSave eqpar = FindGPP(session).FirstOrDefault();
                Teleport(session, par.homeposition);
                infset(session);
                RefundItems(session);
                brplayers.Remove(par);
                EqSavePlayers.Remove(eqpar);
                if(EqSavePlayers.Count == 0)
                {
                    IsEqSaved = false;
                    EqSavePlayers.Clear();
                }
                if(brplayers.Count == 0){
                    FinishEventNoWinners();
                }
            }
        }

        void OnPlayerSuicide(PlayerSession session){
            if(isStarted == true)
            {
                var prov = FindGP(session).Count();
                if (prov == 0) return;
                InvClear(session);
                BRPlayer par = FindGP(session).FirstOrDefault();
                Teleport(session, par.homeposition);
                infset(session);
                RefundItems(session);
                brplayers.Remove(par);
                if(brplayers.Count == 0){
                    FinishEventNoWinners();
                }
                if(brplayers.Count == 1)
                {
                    foreach (BRPlayer bp in brplayers)
                    {
                        if(bp.session != null){
                            FinishEvent(bp.session);
                        } else {
                            FinishEventNoWinners();
                        }
                    }
                }
            }
        }

        void OnPlayerDeath(PlayerSession session, EntityEffectSourceData dataSource)
        {
            if(isStarted == true)
            {
                string KillerName = GetNameOfObject(dataSource.EntitySource);
                if (string.IsNullOrEmpty(KillerName))
                    return;
                
                KillerName = KillerName.Remove(KillerName.Length - 3);
                if (getSession(KillerName) == null)
                    return;
                PlayerSession killersession = getSession(KillerName);
                var prov = FindGP(killersession).Count();
                if (prov == 0) return;
                hurt.SendChatMessage(session, brpref, Msg("BR_leave", session.SteamId));
                InvClear(session);
                BRPlayer par = FindGP(session).FirstOrDefault();
                Teleport(session, par.homeposition);
                infset(session);
                //RefundItems(session);
                brplayers.Remove(par);
                if (brplayers.Count == 1) {
                    FinishEvent(killersession);
                    return;
                } 
                else if (brplayers.Count == 0) {
                    FinishEventNoWinners();
                    return;
                }
            }
        }
        
        void OnPlayerRespawn(PlayerSession session)
        {
            if(IsEqSaved == true)
            {
                var prov = FindGPP(session).FirstOrDefault();
                if(prov == null) return;
                RefundItems(session);
                EqSavePlayers.Remove(prov);
                if(EqSavePlayers.Count == 0){
                    IsEqSaved = false;
                    EqSavePlayers.Clear();
                }
            }
        }

        void OnPlayerSpawn(PlayerSession session)
        {
            if(IsEqSaved == true)
            {
                var prov = FindGPP(session).FirstOrDefault();
                if(prov == null) return;
                RefundItems(session);
                EqSavePlayers.Remove(prov);
                if(EqSavePlayers.Count == 0){
                    IsEqSaved = false;
                    EqSavePlayers.Clear();
                }
            }
        }
               
        void infset(PlayerSession session)
        {
            BRPlayer par = FindGP(session).FirstOrDefault();
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            stats.GetFluidEffect(EEntityFluidEffectType.Infamy).SetValue(par.infamy);
            
        }
        
        void GiveAmmo(PlayerSession session)
        {
            var AmmoList = Config.Get<List<string>>("AmmoAtStartEvent");
            foreach(string am in AmmoList)
			{
                string[] amm = am.ToString().Split(',');
                GIM.GiveItem(session.Player, GIM.GetItem(Convert.ToInt32(amm[0])), Convert.ToInt32(amm[1]));
            }
        }

        private PlayerSession getSession(string identifier)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;

            foreach (var i in sessions)
            {
                //if (i.Value.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                if (i.Value.Identity.Name.ToLower().Contains(identifier.ToLower()) || identifier.Equals(i.Value.SteamId.ToString()))
                {
                    session = i.Value;
                    break;
                }
            }
            return session;
        }
        
        string GetNameOfObject(UnityEngine.GameObject obj)
        {
            var ManagerInstance = GameManager.Instance;
            return ManagerInstance.GetDescriptionKey(obj);
        }

        void InvClear(PlayerSession session)
        {
            var pInventory = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            for (var i = 0; i < pInventory.Capacity; i++) pInventory.Items[i] = null;

            GIM.GiveItem(session.Player, GIM.GetItem(20), 0);
        }
        

        public void RefundItems(PlayerSession session)
        {
            var inv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            BREqSave par = FindGPP(session).FirstOrDefault();

            if (par.inv.Count() > 0)
            {
                foreach (var x in par.inv)
                {
                    if (!inv.GiveItemServer(x.Value, x.Key)) GlobalItemManager.SpawnWorldItem(x.Value, inv);
                }
                inv.Invalidate();
            }
        }

        public static Dictionary<int, ItemInstance> GetPlayerItems(PlayerSession session)
        {
            var playerinv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
            Dictionary<int, ItemInstance> listitems = new Dictionary<int, ItemInstance>();

            for (var i = 0; i < playerinv.Capacity; i++)
            {
                var item = playerinv.GetSlot(i);
                if (item?.Item == null || listitems.ContainsValue(item)) continue;
                listitems.Add(i, item);
            }

            return listitems;
        }
    }
}