using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Common;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    public class Optimizer
    {        
        private Path _saved; 
        private int _savedId;
        private int _tickAligned;
        const double Eps = 1e-8;
        public int SavedAtTick = 0;

        
        public void Clear()
        {
            _saved = new Path();
            _savedId = 0;
            SavedAtTick = 0;
        }

        public Path Saved => _saved;

        public void SaveValues(Path predicted, MyRobot myMe)
        {
            if(predicted.Type == PathType.Unknown)
                Console.WriteLine("UNKNOWN TYPE SAVED AT " + MyGame.CurrentTick);
            
            if(predicted.BallBeforeHit == null || predicted.PredictResults == null)
                return;
            
            _saved = predicted;
            SavedAtTick = MyGame.CurrentTick;
            _saved.PredictResults = new List<Point>(_saved.PredictResults);
            _saved.BallBeforeHit = new List<Point>(_saved.BallBeforeHit);
            _tickAligned = MyGame.CurrentTick;
            _savedId = myMe.id;
        }

        public bool VerifyAndAlign(ModelUpdater updater)
        {
            return VerifyAndAlign(updater.myBall, updater.Teammates, updater.Enemies);
        }
        public bool VerifyAndAlign(MyBall ball, List<MyRobot> teammates, List<MyRobot> enemies)
        {
            if (_savedId == 0)
                return false;
            
            var ticksElapsed = MyGame.CurrentTick - _tickAligned;
            
            if (_saved.BallBeforeHit == null || _saved.BallBeforeHit.Count <= ticksElapsed)
            {
                return false;
            }
            
            if (!_saved.BallBeforeHit[ticksElapsed].position.AlmostEqual(ball.position, Eps)
                || !_saved.BallBeforeHit[ticksElapsed].velocity.AlmostEqual(ball.velocity, Eps))
            {
                return false;
            }

            if (_saved.PredictResults == null || _saved.PredictResults.Count <= ticksElapsed)
            {
                return false;
            }

            if (_saved.PredictResults.Count < ticksElapsed)
            {
                return false;
            }

            var me = teammates.First(t => t.id == _savedId);
            
            if ( !_saved.PredictResults[ticksElapsed].position.AlmostEqual(me.position, Eps)
                 || !_saved.PredictResults[ticksElapsed].velocity.AlmostEqual(me.velocity, Eps))
            {
                return false;
            }
            
            _saved.BallBeforeHit.RemoveRange(0, ticksElapsed);
            _saved.PredictResults.RemoveRange(0, ticksElapsed);
            _tickAligned = MyGame.CurrentTick;
            _saved.EnemyHitTick -= ticksElapsed;
            
            if (_saved.PredictResults.Count < 2)
            {
                return false;
            }

            if (me.touch)
            {
                var enemiesInFlight = enemies.Where(e => !e.touch).Select(r => (MyRobot)r.Clone()).ToList();
                foreach (var enemy in enemiesInFlight)
                {
                    for (int i = 0; i < _saved.BallBeforeHit.Count; ++i)
                    {
                        if (_saved.EnemyHitTick == i)
                            break;
                        var enemyControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, enemy.radiusChangeSpeed,
                            enemy.usedNitroOnLastTick);
                        CollideLogic.update_for_jump(enemy, enemyControl);
                        var ballPredicted = _saved.BallBeforeHit[i];
                        if (enemy.position.DistanceTo(ballPredicted.position) <
                            Constants.BALL_RADIUS + enemy.radius)
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                var myControl = _saved.PredictResults[1].control;
            
                var enemiesInFlight = enemies.Where(e => !e.touch).Select(r => (MyRobot)r.Clone()).ToList();
                var meCopy = (MyRobot)me.Clone();

            
                for (int i = 0; i < 50; ++i)
                {
                    double lastYPos = meCopy.position.y;
                    CollideLogic.update_for_jump(meCopy, myControl);
                    foreach (var enemy in enemiesInFlight)
                    {
                        var enemyControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, enemy.radiusChangeSpeed,
                            enemy.usedNitroOnLastTick);

                        CollideLogic.update_for_jump(enemy, enemyControl);
                    }
                
                    if(lastYPos > meCopy.position.y)
                        break;

                    if (enemiesInFlight.Any(e => e.position.DistanceTo(meCopy.position) < e.radius + meCopy.radius))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryToMove(ModelUpdater updater)
        {           
            if (updater.myMe.id != _savedId || !VerifyAndAlign(updater.myBall, updater.Teammates, updater.Enemies))
            {
                Clear();
                return false;
            }            
            
            updater.Action(_saved.PredictResults[1].control);
            return true;
        }
    }
}