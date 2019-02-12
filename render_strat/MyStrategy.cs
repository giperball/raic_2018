using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using ConsoleApp1.Common;
using Newtonsoft.Json.Linq;
using Action = Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model.Action;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk
{
    
    public sealed class MyStrategy : IStrategy
    {
        
//        string logFile = Environment.GetEnvironmentVariable("GAME_LOG_FILE");
        string dumpFile = Environment.GetEnvironmentVariable("RENDER_DUMP_FILE");
        private int id = 0;
        private int lastId = 0;
        
//        Dictionary<int, JArray> logInfo = new Dictionary<int, JArray>();
        Dictionary<int, JArray> dumpInfo = new Dictionary<int, JArray>();
        private int lastTick = 0;

        private StreamReader reader;
        public void Act(Robot me, Rules rules, Game game, Action action)
        {
            lastTick = game.current_tick;
            lastId = me.id;
            if (id != 0)
                return;

            id = me.id;
            reader = new StreamReader(File.OpenRead(dumpFile));
            
            
//            var log_lines = System.IO.File.ReadAllLines(logFile);
//            for (int i = 1; i < log_lines.Count(); ++i)
//            {
//                JObject tickInfo = JObject.Parse(log_lines[i]);
//                var tick = tickInfo["current_tick"].ToObject<int>();
//                var tickArray = new JArray();
//                foreach (var robotJson in tickInfo["robots"])
//                {
//                    
//                    var robotSphere = new SphereShower(
//                        new Vector3(
//                            robotJson["position"]["x"].ToObject<double>(),
//                            robotJson["position"]["y"].ToObject<double>(),
//                            robotJson["position"]["z"].ToObject<double>()),
//                        robotJson["player_index"].ToObject<int>() == 1 ? new Color(0, 1, 0) : new Color(1, 0, 0),
//                        Constants.ROBOT_RADIUS
//                        );
//                    tickArray.Add(robotSphere.MakeJObject());
//                }
//
//                logInfo[tick] = tickArray;
//            }
//
//            var renderDump = File.ReadAllText(dumpFile);
//            var renderLines = renderDump.Split("@");
//            foreach (var renderLine in renderLines)
//            {
//                if(renderLine == "")
//                    continue;
//                var splitted = renderLine.Split("|");
//                int tick = Int32.Parse(splitted[0]);
//                JArray data = JArray.Parse(splitted[2]);
//                if (!dumpInfo.ContainsKey(tick))
//                    dumpInfo[tick] = data;
//                else
//                {
//                    dumpInfo[tick].Merge(data);
//                }
//            }
        }

        public string CustomRendering()
        {
            return reader.ReadLine();
            
//            
//            using( Stream stream = File.Open(fileName, FileMode.Open) )
//            {
//                using(  )
//                {
//                    string line = null;
//                    for( int i = 0; i < myLineNumber; ++i )
//                    {
//                        line = reader.ReadLine();
//                    }
//                }
//            }
//            
//            JArray result = new JArray();
//
////            result.Merge(logInfo[lastTick]);
//            result.Merge(dumpInfo[lastTick]);
//            
//            return result.ToString();
        }
    }
}
