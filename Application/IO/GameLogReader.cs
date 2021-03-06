﻿using SharedLibraryCore;
using SharedLibraryCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IW4MAdmin.Application.IO
{
    class GameLogReader : IGameLogReader
    {
        IEventParser Parser;
        readonly string LogFile;

        public long Length => new FileInfo(LogFile).Length;

        public int UpdateInterval => 300;

        public GameLogReader(string logFile, IEventParser parser)
        {
            LogFile = logFile;
            Parser = parser;
        }

        public async Task<ICollection<GameEvent>> ReadEventsFromLog(Server server, long fileSizeDiff, long startPosition)
        {
            // allocate the bytes for the new log lines
            List<string> logLines = new List<string>();

            // open the file as a stream
            using (var rd = new StreamReader(new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Utilities.EncodingType))
            {
                // todo: max async
                // take the old start position and go back the number of new characters
                rd.BaseStream.Seek(-fileSizeDiff, SeekOrigin.End);
                // the difference should be in the range of a int :P
                string newLine;
                while (!String.IsNullOrEmpty(newLine = rd.ReadLine()))
                {
                    logLines.Add(newLine);
                }
            }

            List<GameEvent> events = new List<GameEvent>();

            // parse each line
            foreach (string eventLine in logLines)
            {
                if (eventLine.Length > 0)
                {
                    try
                    {
                        // todo: catch elsewhere
                        events.Add(Parser.GetEvent(server, eventLine));
                    }
                    
                    catch (Exception e)
                    {
                        server.Logger.WriteWarning("Could not properly parse event line");
                        server.Logger.WriteDebug(e.Message);
                        server.Logger.WriteDebug(eventLine);
                    }
                }
            }

            return events;
        }
    }
}
