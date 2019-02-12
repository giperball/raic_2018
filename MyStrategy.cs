using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using ConsoleApp1.Common;
using ConsoleApp1.DebugHelpers;
using ConsoleApp1.Logic;
using ConsoleApp1.SubStrategy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Action = Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model.Action;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk
{
    
    public class MultiValueDictionary<Key, Value> : Dictionary<Key, List<Value>> {

        public void Add(Key key, Value value) {
            List<Value> values;
            if (!this.TryGetValue(key, out values)) {
                values = new List<Value>();
                this.Add(key, values);
            }
            values.Add(value);
        }
    }
    
    public sealed class MyStrategy : IStrategy
    {
        static bool saveRenderDump = Environment.GetEnvironmentVariable("SAVE_DUMP_NOW") == "yes";
        static bool tickTimeProfile = Environment.GetEnvironmentVariable("TICK_TIME_PROFILE") == "yes";
        
        
        static List<DateTime> ticksDatetime = new List<DateTime>(){DateTime.Now};
        public static bool DumpAdditionalRenderObjects()
        {
#if !DEBUG
            return saveRenderDump;
#endif
            return true;
        }
        
//        bool sendRenderAnyway = true;
//        bool sendRenderAnyway = Environment.GetEnvironmentVariable("SEND_RENDER_ANYWAY") == "yes";
//        bool saveRenderDump = true;
        private bool firstDumpSave = true;
        ModelUpdater _updater = new ModelUpdater();
        MultiValueDictionary<int, IDebugHelper> _debugHelpers = new MultiValueDictionary<int, IDebugHelper>();
        MultiValueDictionary<int, IDebugHelper> _mainDebugHelpers = new MultiValueDictionary<int, IDebugHelper>();
        StrategyChooser _chooser = new StrategyChooser();
        
        public void Act(Robot me, Rules rules, Game game, Action action)
        {   
            _updater.Update(me, rules, game, action, _chooser);
            
            if (_updater.myGame.IsGoalReset)
            {
                _mainDebugHelpers.Add(0, new TextShower("NEW ROUND"));
                return;
            }

            ISubStrategy subStrategy = _chooser.GetStrategy(_updater);

            _mainDebugHelpers.Add(
                _updater.myMe.id,
                new TextShower(String.Format("Robot {0} ({2}) speed {1}, ground speed {3}, radius {4}, pos {5}", 
                    _updater.myMe.id,
                    _updater.myMe.velocity.magnitude, 
                    subStrategy.Name,
                    _updater.myMe.velocity.DropY().magnitude,
                    _updater.myMe.radius,
                    _updater.myMe.position)));
            
            ActSubStrategy(subStrategy);

            if (action.use_nitro)
            {
                _mainDebugHelpers.Add(_updater.myMe.id, new SphereShower(_updater.myMe.position, Color.cyan, 1.2));
            }
            
        }

        private void ActSubStrategy(ISubStrategy subStrategy)
        {
            try
            {
                subStrategy.Act(_updater);
            }
            catch (SwitchStrategiesException e)
            {
                _chooser.SwitchSubStrategies(e.FromId, e.ToId);
                Console.WriteLine("SWITCH " + MyGame.CurrentTick);
                _mainDebugHelpers.Add(0, new TextShower("SUBSTRATEGIES SWITCH"));
            }
            catch (Exception e)
            {
                if (e is BallSoCloseException)
                {
                    _mainDebugHelpers.Add(_updater.myMe.id, new TextShower("BALL SO CLOSE TO ME, AS NEVER BEFORE"));
                }
                else if (e is AntiLK91Exception)
                {
                    var anti = (AntiLK91Exception) e;
                    _mainDebugHelpers.Add(_updater.myMe.id, new TextShower("SOMEONE WANNA TOUCH ME"));
                    _updater.Action(MoveCalculator.TargetToVelocity(_updater.myMe.position, anti.Target), AntiLK91Exception.JumpSpeed, true);
                    return;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("EXCEPTION (" + e.GetType().Name + ") at tick " + MyGame.CurrentTick);
                    Console.ResetColor();
                    Console.WriteLine(e);

                    _mainDebugHelpers.Add(_updater.myMe.id, new TextShower(String.Format(
                        "Robot {0} ({2}) speed {1}",
                        _updater.myMe.id,
                        e.ToString(),
                        subStrategy.Name)));
                }
                ActSubStrategy(new DumbSubStrategy());
            }
            
            if (subStrategy.DebugHelpers != null)
                _debugHelpers[_updater.myMe.id] = subStrategy.DebugHelpers;               
        }

        void PrintTickProfile()
        {
            const int printLongTicks = 20;
            
            var p = new List<Tuple<int, int>>();
            for(int i = 1; i < ticksDatetime.Count; ++i)
                p.Add(new Tuple<int, int>(i-1, (ticksDatetime[i] - ticksDatetime[i-1]).Milliseconds));
           
            Console.WriteLine("Most longest ticks:");
            foreach (var q in p.OrderByDescending(w => w.Item2).Take(printLongTicks))
            {
                Console.WriteLine("Tick " + q.Item1 + ", msecs " + q.Item2);
            }
        }
        
        public string CustomRendering()
        {
            if (tickTimeProfile)
            {
                ticksDatetime.Add(DateTime.Now);
                Console.WriteLine("Current tick " + MyGame.CurrentTick);
                var printFromTick = int.Parse(Environment.GetEnvironmentVariable("PRINT_FROM_TICK"));
                if(MyGame.CurrentTick >= printFromTick)
                    PrintTickProfile();
            }
            
#if !DEBUG
            if (!saveRenderDump)
                return "";
#endif
            
            
            _mainDebugHelpers.Add(0, new TextShower(String.Format("TICK {0}", MyGame.CurrentTick)));
            
            var array = new JArray();
            foreach (var debugHelpersKeys in _mainDebugHelpers.Keys.OrderBy(v => v))
            {
                foreach (var debugHelper in _mainDebugHelpers[debugHelpersKeys])
                {
                    var t = debugHelper.MakeJObject();
                    if (t == null)
                    {
                        foreach (var jObject in debugHelper.MakeJObjects())
                        {
                            array.Add(jObject);
                        }
                        continue;
                    }
                    
                    array.Add(debugHelper.MakeJObject());
                }
            }
            foreach (var debugHelpersKeys in _debugHelpers.Keys.OrderBy(v => v))
            {
                array.Add(new TextShower("----------------------- " + debugHelpersKeys).MakeJObject());
                foreach (var debugHelper in _debugHelpers[debugHelpersKeys])
                {
                    var t = debugHelper.MakeJObject();
                    if (t == null)
                    {
                        foreach (var jObject in debugHelper.MakeJObjects())
                        {
                            array.Add(jObject);
                        }
                        continue;
                    }
                    
                    array.Add(debugHelper.MakeJObject());
                }
            }
            
            _debugHelpers.Clear();
            _mainDebugHelpers.Clear();
            
            if (saveRenderDump)
            {
                Console.WriteLine("Dumping " + MyGame.CurrentTick);
                array.Add(new TextShower(String.Format("Score {0}:{1}", _updater.myGame.MeScore, _updater.myGame.EnemyScore)).MakeJObject());
                foreach (var robot in _updater.myRobots.Values)
                {
                    array.Add(new SphereShower(
                        robot.position, 
                        robot.isTeammate ? new Color(0, 1, 0) : new Color(1, 0, 0),
                        Constants.ROBOT_RADIUS).MakeJObject());
                }
                
                array.Add(new SphereShower(_updater.myBall.position, new Color(1,1,1), Constants.BALL_RADIUS).MakeJObject());
                
                const string renderDumpFile = "render_dump.txt";
                if (firstDumpSave)
                {
                    firstDumpSave = false;
                    File.Delete(renderDumpFile);
                }
                
                File.AppendAllText(renderDumpFile, array.ToString(Formatting.None) + "\n");
                
                if(_updater.myGame.MaxTick - 1 == MyGame.CurrentTick)
                    Console.WriteLine("THIS IS ENDGAME");
                
                return "";
            }
            
            var str = array.ToString();
            
            return str;
        }
    }
}
