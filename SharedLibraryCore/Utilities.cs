﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

using SharedLibraryCore.Objects;
using static SharedLibraryCore.Server;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace SharedLibraryCore
{
    public static class Utilities
    {
#if DEBUG == true
        public static string OperatingDirectory => $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}{Path.DirectorySeparatorChar}";
#else
        public static string OperatingDirectory => $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}";
#endif
        public static Encoding EncodingType;
        public static Localization.Layout CurrentLocalization = new Localization.Layout(new Dictionary<string, string>());
        public static Player IW4MAdminClient(Server server = null) => new Player()
        {
            ClientId = 1,
            State = Player.ClientState.Connected,
            Level = Player.Permission.Console,
            CurrentServer = server
        };

        public static string HttpRequest(string location, string header, string headerValue)
        {
            using (var RequestClient = new System.Net.Http.HttpClient())
            {
                RequestClient.DefaultRequestHeaders.Add(header, headerValue);
                string response = RequestClient.GetStringAsync(location).Result;
                return response;
            }
        }

        //Get string with specified number of spaces -- really only for visual output
        public static String GetSpaces(int Num)
        {
            String SpaceString = String.Empty;
            while (Num > 0)
            {
                SpaceString += ' ';
                Num--;
            }

            return SpaceString;
        }

        //Remove words from a space delimited string
        public static String RemoveWords(this string str, int num)
        {
            if (str == null || str.Length == 0)
                return "";

            String newStr = String.Empty;
            String[] tmp = str.Split(' ');

            for (int i = 0; i < tmp.Length; i++)
            {
                if (i >= num)
                    newStr += tmp[i] + ' ';
            }

            return newStr;
        }

        /// <summary>
        /// helper method to get the information about an exception and inner exceptions
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string GetExceptionInfo(this Exception ex)
        {
            var sb = new StringBuilder();
            int depth = 0;
            while (ex != null)
            {
                sb.AppendLine($"Exception[{depth}] Name: {ex.GetType().FullName}");
                sb.AppendLine($"Exception[{depth}] Message: {ex.Message}");
                sb.AppendLine($"Exception[{depth}] Call Stack: {ex.StackTrace}");
                sb.AppendLine($"Exception[{depth}] Source: {ex.Source}");
                depth++;
                ex = ex.InnerException;
            }

            return sb.ToString();
        }

        public static Player.Permission MatchPermission(String str)
        {
            String lookingFor = str.ToLower();

            for (Player.Permission Perm = Player.Permission.User; Perm < Player.Permission.Console; Perm++)
                if (lookingFor.Contains(Perm.ToString().ToLower())
                    || lookingFor.Contains(CurrentLocalization.LocalizationIndex[$"GLOBAL_PERMISSION_{Perm.ToString().ToUpper()}"].ToLower()))
                    return Perm;

            return Player.Permission.Banned;
        }

        /// <summary>
        /// Remove all IW Engine color codes
        /// </summary>
        /// <param name="str">String containing color codes</param>
        /// <returns></returns>
        public static String StripColors(this string str)
        {
            if (str == null)
                return "";
            str = Regex.Replace(str, @"(\^+((?![a-z]|[A-Z]).){0,1})+", "");
            string str2 = Regex.Match(str, @"(^\/+.*$)|(^.*\/+$)")
                .Value
                .Replace("/", " /");
            return str2.Length > 0 ? str2 : str;
        }

        /// <summary>
        /// Get the IW Engine color code corresponding to an admin level
        /// </summary>
        /// <param name="level">Specified player level</param>
        /// <returns></returns>
        public static String ConvertLevelToColor(Player.Permission level, string localizedLevel)
        {
            char colorCode = '6';
            // todo: maybe make this game independant?
            switch (level)
            {
                case Player.Permission.Banned:
                    colorCode = '1';
                    break;
                case Player.Permission.Flagged:
                    colorCode = '9';
                    break;
                case Player.Permission.Owner:
                    colorCode = '5';
                    break;
                case Player.Permission.User:
                    colorCode = '2';
                    break;
                case Player.Permission.Trusted:
                    colorCode = '3';
                    break;
                default:
                    break;
            }

            return $"^{colorCode}{localizedLevel ?? level.ToString()}";
        }

        public static string ToLocalizedLevelName(this Player.Permission perm) => CurrentLocalization.LocalizationIndex[$"GLOBAL_PERMISSION_{perm.ToString().ToUpper()}"];

        public static String ProcessMessageToken(this Server server, IList<Helpers.MessageToken> tokens, String str)
        {
            MatchCollection RegexMatches = Regex.Matches(str, @"\{\{[A-Z]+\}\}", RegexOptions.IgnoreCase);
            foreach (Match M in RegexMatches)
            {
                String Match = M.Value;
                String Identifier = M.Value.Substring(2, M.Length - 4);

                var found = tokens.FirstOrDefault(t => t.Name.ToLower() == Identifier.ToLower());

                if (found != null)
                    str = str.Replace(Match, found.Process(server));
            }

            return str;
        }

        public static bool IsBroadcastCommand(this string str)
        {
            return str[0] == '@';
        }

        /// <summary>
        /// Get the full gametype name
        /// </summary>
        /// <param name="input">Shorthand gametype reported from server</param>
        /// <returns></returns>
        public static String GetLocalizedGametype(String input)
        {
            switch (input)
            {
                case "dm":
                    return "Deathmatch";
                case "war":
                    return "Team Deathmatch";
                case "koth":
                    return "Headquarters";
                case "ctf":
                    return "Capture The Flag";
                case "dd":
                    return "Demolition";
                case "dom":
                    return "Domination";
                case "sab":
                    return "Sabotage";
                case "sd":
                    return "Search & Destroy";
                case "vip":
                    return "Very Important Person";
                case "gtnw":
                    return "Global Thermonuclear War";
                case "oitc":
                    return "One In The Chamber";
                case "arena":
                    return "Arena";
                case "dzone":
                    return "Drop Zone";
                case "gg":
                    return "Gun Game";
                case "snipe":
                    return "Sniping";
                case "ss":
                    return "Sharp Shooter";
                case "m40a3":
                    return "M40A3";
                case "fo":
                    return "Face Off";
                case "dmc":
                    return "Deathmatch Classic";
                case "killcon":
                    return "Kill Confirmed";
                case "oneflag":
                    return "One Flag CTF";
                default:
                    return input;
            }
        }

        public static long ConvertLong(this string str)
        {
            str = str.Substring(0, Math.Min(str.Length, 16));
            if (Int64.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long id))
                return id;
            var bot = Regex.Match(str, @"bot[0-9]+").Value;
            if (!string.IsNullOrEmpty(bot))
                // should set their GUID to the negation of their 1 based index  (-1 - -18)
                return -(Convert.ToInt64(bot.Substring(3)) + 1);
            return long.MinValue;
        }

        public static int ConvertToIP(this string str)
        {
            System.Net.IPAddress.TryParse(str, out System.Net.IPAddress ip);

            return ip == null ? int.MaxValue : BitConverter.ToInt32(ip.GetAddressBytes(), 0);
        }

        public static string ConvertIPtoString(this int ip)
        {
            return new System.Net.IPAddress(BitConverter.GetBytes(ip)).ToString();
        }

        public static String GetTimePassed(DateTime start)
        {
            return GetTimePassed(start, true);
        }

        public static String GetTimePassed(DateTime start, bool includeAgo)
        {
            TimeSpan Elapsed = DateTime.UtcNow - start;
            string ago = includeAgo ? $" {CurrentLocalization.LocalizationIndex["WEBFRONT_PENALTY_TEMPLATE_AGO"]}" : "";

            if (Elapsed.TotalSeconds < 30)
            {
                return CurrentLocalization.LocalizationIndex["GLOBAL_TIME_JUSTNOW"];
            }
            if (Elapsed.TotalMinutes < 120)
            {
                if (Elapsed.TotalMinutes < 1.5)
                    return $"1 {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_MINUTES"]}{ago}";
                return Math.Round(Elapsed.TotalMinutes, 0) + $" {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_MINUTES"]}{ago}";
            }
            if (Elapsed.TotalHours <= 24)
            {
                if (Elapsed.TotalHours < 1.5)
                    return $"1 {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_HOURS"]}{ago}";
                return Math.Round(Elapsed.TotalHours, 0) + $" { CurrentLocalization.LocalizationIndex["GLOBAL_TIME_HOURS"]}{ago}";
            }
            if (Elapsed.TotalDays <= 90)
            {
                if (Elapsed.TotalDays < 1.5)
                    return $"1 {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_DAYS"]}{ago}";
                return Math.Round(Elapsed.TotalDays, 0) + $" {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_DAYS"]}{ago}";
            }
            if (Elapsed.TotalDays <= 365)
            {
                return $"{Math.Round(Elapsed.TotalDays / 7)} {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_WEEKS"]}{ago}";
            }
            else
            {
                return $"{Math.Round(Elapsed.TotalDays / 30, 0)} {CurrentLocalization.LocalizationIndex["GLOBAL_TIME_MONTHS"]}{ago}";
            }
        }

        public static Game GetGame(string gameName)
        {
            if (gameName.Contains("IW4"))
                return Game.IW4;
            if (gameName.Contains("CoD4"))
                return Game.IW3;
            if (gameName.Contains("COD_WaW"))
                return Game.T4;
            if (gameName.Contains("COD_T5_S"))
                return Game.T5;
            if (gameName.Contains("T5M"))
                return Game.T5M;
            if (gameName.Contains("IW5"))
                return Game.IW5;
            if (gameName.Contains("COD_T6_S"))
                return Game.T6M;

            return Game.UKN;
        }

        public static string EscapeMarkdown(this string markdownString)
        {
            return markdownString.Replace("<", "\\<").Replace(">", "\\>").Replace("|", "\\|");
        }

        public static TimeSpan ParseTimespan(this string input)
        {
            var expressionMatch = Regex.Match(input, @"([0-9]+)(\w+)");

            if (!expressionMatch.Success) // fallback to default tempban length of 1 hour
                return new TimeSpan(1, 0, 0);

            char lengthDenote = expressionMatch.Groups[2].ToString()[0];
            int length = Int32.Parse(expressionMatch.Groups[1].ToString());

            var loc = CurrentLocalization.LocalizationIndex;

            if (lengthDenote == char.ToLower(loc["GLOBAL_TIME_MINUTES"][0]))
            {
                return new TimeSpan(0, length, 0);
            }

            if (lengthDenote == char.ToLower(loc["GLOBAL_TIME_HOURS"][0]))
            {
                return new TimeSpan(length, 0, 0);
            }

            if (lengthDenote == char.ToLower(loc["GLOBAL_TIME_DAYS"][0]))
            {
                return new TimeSpan(length, 0, 0, 0);
            }

            if (lengthDenote == char.ToLower(loc["GLOBAL_TIME_WEEKS"][0]))
            {
                return new TimeSpan(length * 7, 0, 0, 0);
            }

            if (lengthDenote == char.ToLower(loc["GLOBAL_TIME_YEARS"][0]))
            {
                return new TimeSpan(length * 365, 0, 0, 0);
            }

            return new TimeSpan(1, 0, 0);
        }

        public static string TimeSpanText(this TimeSpan span)
        {
            var loc = CurrentLocalization.LocalizationIndex;

            if (span.TotalMinutes < 60)
                return $"{span.Minutes} {loc["GLOBAL_TIME_MINUTES"]}";
            else if (span.Hours >= 1 && span.TotalHours < 24)
                return $"{span.Hours} {loc["GLOBAL_TIME_HOURS"]}";
            else if (span.TotalDays >= 1 && span.TotalDays < 7)
                return $"{span.Days} {loc["GLOBAL_TIME_DAYS"]}";
            else if (span.TotalDays >= 7 && span.TotalDays < 90)
                return $"{Math.Round(span.Days / 7.0, 0)} {loc["GLOBAL_TIME_WEEKS"]}";
            else if (span.TotalDays >= 90 && span.TotalDays < 365)
                return $"{Math.Round(span.Days / 30.0, 0)} {loc["GLOBAL_TIME_MONTHS"]}";
            else if (span.TotalDays >= 365 && span.TotalDays < 36500)
                return $"{Math.Round(span.Days / 365.0, 0)} {loc["GLOBAL_TIME_YEARS"]}";
            else if (span.TotalDays >= 36500)
                return loc["GLOBAL_TIME_FOREVER"];

            return "unknown";
        }

        public static Player AsPlayer(this Database.Models.EFClient client)
        {
            return client == null ? null : new Player()
            {
                Active = client.Active,
                AliasLink = client.AliasLink,
                AliasLinkId = client.AliasLinkId,
                ClientId = client.ClientId,
                ClientNumber = -1,
                FirstConnection = client.FirstConnection,
                Connections = client.Connections,
                NetworkId = client.NetworkId,
                TotalConnectionTime = client.TotalConnectionTime,
                Masked = client.Masked,
                Name = client.CurrentAlias.Name,
                IPAddress = client.CurrentAlias.IPAddress,
                Level = client.Level,
                LastConnection = client.LastConnection == DateTime.MinValue ? DateTime.UtcNow : client.LastConnection,
                CurrentAlias = client.CurrentAlias,
                CurrentAliasId = client.CurrentAlias.AliasId,
                // todo: make sure this is up to date
                IsBot = client.IPAddress == int.MinValue,
                Password = client.Password,
                PasswordSalt = client.PasswordSalt
            };
        }

        public static bool IsPrivileged(this Player p) => p.Level > Player.Permission.User;

        public static bool PromptBool(string question)
        {
            Console.Write($"{question}? [y/n]: ");
            return (Console.ReadLine().ToLower().FirstOrDefault() as char?) == 'y';
        }

        /// <summary>
        /// prompt user to enter a number
        /// </summary>
        /// <param name="question">question to prompt with</param>
        /// <param name="maxValue">maximum value to allow</param>
        /// <param name="minValue">minimum value to allow</param>
        /// <returns>integer from user's input</returns>
        public static int PromptInt(this string question, int minValue = 0, int maxValue = int.MaxValue)
        {
            Console.Write($"{question}: ");
            int response;

            while (!int.TryParse(Console.ReadLine(), out response) ||
                response < minValue ||
                response > maxValue)
            {
                string range = "";
                if (minValue != 0 || maxValue != int.MaxValue)
                {
                    range = $" [{minValue}-{maxValue}]";
                }
                Console.Write($"Please enter a valid number{range}: ");
            }

            return response;
        }

        public static string PromptString(string question)
        {
            string response;
            do
            {
                Console.Write($"{question}: ");
                response = Console.ReadLine();
            } while (string.IsNullOrWhiteSpace(response));

            return response;
        }

        public static int ClientIdFromString(String[] lineSplit, int cIDPos)
        {
            int pID = -2; // apparently falling = -1 cID so i can't use it now
            int.TryParse(lineSplit[cIDPos].Trim(), out pID);

            if (pID == -1) // special case similar to mod_suicide
                int.TryParse(lineSplit[2], out pID);

            return pID;
        }

        public static Dictionary<string, string> DictionaryFromKeyValue(this string eventLine)
        {
            string[] values = eventLine.Substring(1).Split('\\');

            Dictionary<string, string> dict = null;

            if (values.Length % 2 == 0 && values.Length > 1)
            {
                dict = new Dictionary<string, string>();
                for (int i = 0; i < values.Length; i += 2)
                    dict.Add(values[i], values[i + 1]);
            }

            return dict;
        }

        /* https://loune.net/2017/06/running-shell-bash-commands-in-net-core/ */
        public static string GetCommandLine(int pId)
        {
            var cmdProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c wmic process where processid={pId} get CommandLine",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            cmdProcess.Start();
            cmdProcess.WaitForExit();

            string[] cmdLine = cmdProcess.StandardOutput.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

            return cmdLine.Length > 1 ? cmdLine[1] : cmdLine[0];
        }

        public static string ToBase64UrlSafeString(this string src) => Convert.ToBase64String(src.Select(c => Convert.ToByte(c)).ToArray()).Replace('+', '-').Replace('/', '_');

        public static Task<Dvar<T>> GetDvarAsync<T>(this Server server, string dvarName) => server.RconParser.GetDvarAsync<T>(server.RemoteConnection, dvarName);

        public static Task SetDvarAsync(this Server server, string dvarName, object dvarValue) => server.RconParser.SetDvarAsync(server.RemoteConnection, dvarName, dvarValue);

        public static async Task<string[]> ExecuteCommandAsync(this Server server, string commandName) => await server.RconParser.ExecuteCommandAsync(server.RemoteConnection, commandName);

        public static Task<List<Player>> GetStatusAsync(this Server server) => server.RconParser.GetStatusAsync(server.RemoteConnection);

        public static async Task<Dictionary<string, string>> GetInfoAsync(this Server server)
        {
            string[] response = new string[0];
            for (int i = 0; i < 4; i++)
            {
                response = await server.RemoteConnection.SendQueryAsync(RCon.StaticHelpers.QueryType.GET_INFO);
                if (response.Length == 2)
                    break;
                await Task.Delay(RCon.StaticHelpers.FloodProtectionInterval);
            }
            return response.FirstOrDefault(r => r[0] == '\\')?.DictionaryFromKeyValue();
        }

        public static double GetVersionAsDouble()
        {
            string version = Assembly.GetCallingAssembly().GetName().Version.ToString();
            version = version.Replace(".", "");
            return double.Parse(version) / 1000.0;
        }

        public static string GetVersionAsString() => Assembly.GetCallingAssembly().GetName().Version.ToString();

#if DEBUG == true

        private static readonly TypeInfo QueryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();

        private static readonly FieldInfo QueryCompilerField = typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.First(x => x.Name == "_queryCompiler");

        private static readonly FieldInfo QueryModelGeneratorField = QueryCompilerTypeInfo.DeclaredFields.First(x => x.Name == "_queryModelGenerator");

        private static readonly FieldInfo DataBaseField = QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");

        private static readonly PropertyInfo DatabaseDependenciesField = typeof(Microsoft.EntityFrameworkCore.Storage.Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

        public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            var queryCompiler = (QueryCompiler)QueryCompilerField.GetValue(query.Provider);
            var modelGenerator = (QueryModelGenerator)QueryModelGeneratorField.GetValue(queryCompiler);
            var queryModel = modelGenerator.ParseQuery(query.Expression);
            var database = (IDatabase)DataBaseField.GetValue(queryCompiler);
            var databaseDependencies = (DatabaseDependencies)DatabaseDependenciesField.GetValue(database);
            var queryCompilationContext = databaseDependencies.QueryCompilationContextFactory.Create(false);
            var modelVisitor = (RelationalQueryModelVisitor)queryCompilationContext.CreateQueryModelVisitor();
            modelVisitor.CreateQueryExecutor<TEntity>(queryModel);
            var sql = modelVisitor.Queries.First().ToString().Replace("\"", "`");

            return sql;
        }
#endif
    }
}
