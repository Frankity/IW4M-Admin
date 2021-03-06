﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SharedLibraryCore.Helpers;
using SharedLibraryCore.Objects;
using SharedLibraryCore.Dtos;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace SharedLibraryCore
{
    public abstract class Server
    {
        public enum Game
        {
            UKN,
            IW3,
            IW4,
            IW5,
            T4,
            T5,
            T5M,
            T6M,
        }

        public Server(IManager mgr, ServerConfiguration config)
        {
            Password = config.Password;
            IP = config.IPAddress;
            Port = config.Port;
            Manager = mgr;
            Logger = Manager.GetLogger(this.GetHashCode());
            Logger.WriteInfo(this.ToString());
            ServerConfig = config;
            RemoteConnection = new RCon.Connection(IP, Port, Password, Logger);

            Players = new List<Player>(new Player[18]);
            Reports = new List<Report>();
            PlayerHistory = new Queue<PlayerHistory>();
            ChatHistory = new List<ChatInfo>();
            NextMessage = 0;
            CustomSayEnabled = Manager.GetApplicationSettings().Configuration().EnableCustomSayName;
            CustomSayName = Manager.GetApplicationSettings().Configuration().CustomSayName;
            InitializeTokens();
            InitializeAutoMessages();
        }

        //Returns current server IP set by `net_ip` -- *STRING*
        public String GetIP()
        {
            return IP;
        }

        //Returns current server port set by `net_port` -- *INT*
        public int GetPort()
        {
            return Port;
        }

        //Returns list of all current players
        public List<Player> GetPlayersAsList()
        {
            return Players.FindAll(x => x != null);
        }

        /// <summary>
        /// Add a player to the server's player list
        /// </summary>
        /// <param name="P">Player pulled from memory reading</param>
        /// <returns>True if player added sucessfully, false otherwise</returns>
        abstract public Task<bool> AddPlayer(Player P);

        /// <summary>
        /// Remove player by client number
        /// </summary>
        /// <param name="cNum">Client ID of player to be removed</param>
        /// <returns>true if removal succeded, false otherwise</returns>
        abstract public Task RemovePlayer(int cNum);


        /// <summary>
        /// Get a player by name
        /// </summary>
        /// <param name="pName">Player name to search for</param>
        /// <returns>Matching player if found</returns>
        public List<Player> GetClientByName(String pName)
        {
            string[] QuoteSplit = pName.Split('"');
            bool literal = false;
            if (QuoteSplit.Length > 1)
            {
                pName = QuoteSplit[1];
                literal = true;
            }
            if (literal)
                return Players.Where(p => p != null && p.Name.ToLower().Equals(pName.ToLower())).ToList();

            return Players.Where(p => p != null && p.Name.ToLower().Contains(pName.ToLower())).ToList();
        }

        virtual public Task<bool> ProcessUpdatesAsync(CancellationToken cts) => (Task<bool>)Task.CompletedTask;

        /// <summary>
        /// Process any server event
        /// </summary>
        /// <param name="E">Event</param>
        /// <returns>True on sucess</returns>
        abstract protected Task<bool> ProcessEvent(GameEvent E);
        abstract public Task ExecuteEvent(GameEvent E);

        /// <summary>
        /// Send a message to all players
        /// </summary>
        /// <param name="message">Message to be sent to all players</param>
        public GameEvent Broadcast(string message, Player sender = null)
        {
            string formattedMessage = String.Format(RconParser.GetCommandPrefixes().Say, $"{(CustomSayEnabled ? $"{CustomSayName}: " : "")}{message}");

#if DEBUG == true
            Logger.WriteVerbose(message.StripColors());
#endif

            var e = new GameEvent()
            {
                Type = GameEvent.EventType.Broadcast,
                Data = formattedMessage,
                Owner = this,
                Origin = sender,
            };

            Manager.GetEventHandler().AddEvent(e);
            return e;
        }

        /// <summary>
        /// Send a message to a particular players
        /// </summary>
        /// <param name="Message">Message to send</param>
        /// <param name="Target">Player to send message to</param>
        protected async Task Tell(String Message, Player Target)
        {
#if !DEBUG
            string formattedMessage = String.Format(RconParser.GetCommandPrefixes().Tell, Target.ClientNumber, $"{(CustomSayEnabled ? $"{CustomSayName}: " : "")}{Message}");
            if (Target.ClientNumber > -1 && Message.Length > 0 && Target.Level != Player.Permission.Console)
                await this.ExecuteCommandAsync(formattedMessage);
#else
            Logger.WriteVerbose($"{Target.ClientNumber}->{Message.StripColors()}");
            await Task.CompletedTask;
#endif

            if (Target.Level == Player.Permission.Console)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(Message.StripColors());
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // prevent this from queueing up too many command responses
            if (CommandResult.Count > 15)
                CommandResult.RemoveAt(0);

            // it was a remote command so we need to add it to the command result queue
            if (Target.ClientNumber < 0)
            {
                CommandResult.Add(new CommandResponseInfo()
                {
                    Response = Message.StripColors(),
                    ClientId = Target.ClientId
                });
            }
        }

        /// <summary>
        /// Send a message to all admins on the server
        /// </summary>
        /// <param name="message">Message to send out</param>
        public void ToAdmins(String message)
        {
            foreach (var client in GetPlayersAsList().Where(c => c.Level > Player.Permission.Flagged))
            {
                client.Tell(message);
            }
        }

        /// <summary>
        /// Kick a player from the server
        /// </summary>
        /// <param name="Reason">Reason for kicking</param>
        /// <param name="Target">Player to kick</param>
        abstract protected Task Kick(String Reason, Player Target, Player Origin);

        /// <summary>
        /// Temporarily ban a player ( default 1 hour ) from the server
        /// </summary>
        /// <param name="Reason">Reason for banning the player</param>
        /// <param name="Target">The player to ban</param>
        abstract protected Task TempBan(String Reason, TimeSpan length, Player Target, Player Origin);

        /// <summary>
        /// Perm ban a player from the server
        /// </summary>
        /// <param name="Reason">The reason for the ban</param>
        /// <param name="Target">The person to ban</param>
        /// <param name="Origin">The person who banned the target</param>
        abstract protected Task Ban(String Reason, Player Target, Player Origin);

        abstract protected Task Warn(String Reason, Player Target, Player Origin);

        /// <summary>
        /// Unban a player by npID / GUID
        /// </summary>
        /// <param name="npID">npID of the player</param>
        /// <param name="Target">I don't remember what this is for</param>
        /// <returns></returns>
        abstract public Task Unban(string reason, Player Target, Player Origin);

        /// <summary>
        /// Change the current searver map
        /// </summary>
        /// <param name="mapName">Non-localized map name</param>
        public async Task LoadMap(string mapName)
        {
            await this.ExecuteCommandAsync($"map {mapName}");
        }

        public async Task LoadMap(Map newMap)
        {
            await this.ExecuteCommandAsync($"map {newMap.Name}");
        }

        /// <summary>
        /// Initalize the macro variables
        /// </summary>
        abstract public void InitializeTokens();

        /// <summary>
        /// Read the map configuration
        /// </summary>
        protected void InitializeMaps()
        {
            Maps = new List<Map>();
            var gameMaps = Manager.GetApplicationSettings().Configuration().Maps.FirstOrDefault(m => m.Game == GameName);
            if (gameMaps != null)
                Maps.AddRange(gameMaps.Maps);
        }

        /// <summary>
        /// Initialize the messages to be broadcasted
        /// </summary>
        protected void InitializeAutoMessages()
        {
            BroadcastMessages = new List<String>();

            if (ServerConfig.AutoMessages != null)
                BroadcastMessages.AddRange(ServerConfig.AutoMessages);
            BroadcastMessages.AddRange(Manager.GetApplicationSettings().Configuration().AutoMessages);
        }

        public override string ToString()
        {
            return $"{IP}-{Port}";
        }

        protected async Task<bool> ScriptLoaded()
        {
            try
            {
                return (await this.GetDvarAsync<string>("sv_customcallbacks")).Value == "1";
            }

            catch (Exceptions.DvarException)
            {
                return false;
            }
        }

        // Objects
        public IManager Manager { get; protected set; }
        public ILogger Logger { get; private set; }
        public ServerConfiguration ServerConfig { get; private set; }
        public List<Map> Maps { get; protected set; }
        public List<Report> Reports { get; set; }
        public List<ChatInfo> ChatHistory { get; protected set; }
        public Queue<PlayerHistory> PlayerHistory { get; private set; }
        public Game GameName { get; protected set; }

        // Info
        public string Hostname { get; protected set; }
        public string Website { get; protected set; }
        public string Gametype { get; set; }
        public Map CurrentMap { get; set; }
        public int ClientNum
        {
            get
            {
                return Players.Where(p => p != null).Count();
            }
        }
        public int MaxClients { get; protected set; }
        public List<Player> Players { get; protected set; }
        public string Password { get; private set; }
        public bool Throttled { get; protected set; }
        public bool CustomCallback { get; protected set; }
        public string WorkingDirectory { get; protected set; }
        public RCon.Connection RemoteConnection { get; protected set; }
        public IRConParser RconParser { get; protected set; }
        public IEventParser EventParser { get; set; }
        public string LogPath { get; protected set; }

        // Internal
        protected string IP;
        protected int Port;
        protected string FSGame;
        protected int NextMessage;
        protected int ConnectionErrors;
        protected List<string> BroadcastMessages;
        protected TimeSpan LastMessage;
        protected DateTime LastPoll;
        protected ManualResetEventSlim OnRemoteCommandResponse;

        // only here for performance
        private readonly bool CustomSayEnabled;
        private readonly string CustomSayName;

        //Remote
        public IList<CommandResponseInfo> CommandResult = new List<CommandResponseInfo>();
    }
}
