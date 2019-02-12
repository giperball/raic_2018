using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.SubStrategy;

namespace ConsoleApp1.Logic
{
    public class SwitchStrategiesException : Exception
    {
        private int _fromId;
        private int _toId;
        
        public SwitchStrategiesException(int fromId, int toId)
            : base()
        {
            _fromId = fromId;
            _toId = toId;
        }

        public int FromId
        {
            get { return _fromId; }
        }

        public int ToId
        {
            get { return _toId; }
        }
    }
    
    public class StrategyChooser
    {
        private Dictionary<int, ISubStrategy> _subStrategies = new Dictionary<int, ISubStrategy>();
        private int _lastNewRoundTick = -1;

        public List<ISubStrategy> GetStrategies()
        {
            return _subStrategies.Values.ToList();
        }

        public Optimizer GetOptimizer(int id)
        {
            var strat = _subStrategies[id];
            if (!(strat is BaseSubStrategy))
                return null;
            return ((BaseSubStrategy) strat)._optimizer;
        }
        
        public ISubStrategy GetStrategy(ModelUpdater updater)
        {
            if (updater.myGame.IsNewRound && _lastNewRoundTick != MyGame.CurrentTick)
            {
                _lastNewRoundTick = MyGame.CurrentTick;
                _subStrategies.Clear();
                var defender = updater.Teammates.MinBy(r => (r.position - MoveCalculator.DefendPoint).magnitude);
                foreach (var robot in updater.Teammates)
                {
//                    if (robot.id == 1)
//                        _subStrategies[robot.id] = new BaseSubStrategy(new AttackerExtension());
//                    else
//                    {
//                        _subStrategies[robot.id] = new EmptySubStrategy();
//                    }
                    
                    if (robot.id == defender.id)
//                        _subStrategies[robot.id] = new EmptySubStrategy();
                        _subStrategies[robot.id] = new BaseSubStrategy(new DefenderExtension(), updater.Teammates.Count);
                    else
//                        _subStrategies[robot.id] = new EmptySubStrategy();
                        _subStrategies[robot.id] = new BaseSubStrategy(new AttackerExtension(), updater.Teammates.Count);
                }
            }

            return _subStrategies[updater.myMe.id];
        }

        private static int LastSwitch = 0;

        public static void TryToSwitchStrategies(int fromId, int toId)
        {
            if(MyGame.CurrentTick - LastSwitch > 200)
                throw new SwitchStrategiesException(fromId, toId);
        }

        public void SwitchSubStrategies(int fromId, int toId)
        {
            LastSwitch = MyGame.CurrentTick;
            var keys = _subStrategies.Keys;
            LastSwitch = MyGame.CurrentTick;
            var t = _subStrategies[fromId];
            _subStrategies[fromId] = _subStrategies[toId];
            _subStrategies[toId] = t;
        }
    }
}