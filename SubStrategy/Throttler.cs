using System;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    public class Throttler
    {
        private int _tickStarted;
        private const int _tickCountdown = 4;
        private int _enemyId = 0;
        private int _myId = 0;

        public bool Act(ModelUpdater updater)
        {
            if (_enemyId == 0)
                return false;
            if (MyGame.CurrentTick - _tickStarted > _tickCountdown)
                return false;
            if (_myId != updater.myMe.id)
                return false;

            var enemy = updater.Enemies.First(e => e.id == _enemyId);
            updater.Action(new Control(MoveCalculator.TargetToVelocity(updater.myMe.position, enemy.position)));
            return true;
        }

        public void Start(int enemyId, ModelUpdater updater)
        {
            _tickStarted = MyGame.CurrentTick;
            _enemyId = enemyId;
            _myId = updater.myMe.id;
            Act(updater);
        }
    }
}