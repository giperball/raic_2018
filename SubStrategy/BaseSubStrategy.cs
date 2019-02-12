using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.Common;
using ConsoleApp1.DebugHelpers;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    
    public class BallSoCloseException : Exception
    {
    }
    public class AntiLK91Exception : Exception
    {
        public Vector3 Target;
        public const double JumpSpeed = Constants.ROBOT_MAX_JUMP_SPEED / 2;

        public AntiLK91Exception(Vector3 target)
        {
            Target = target;
        }
    }
    
    public class BaseSubStrategy: ISubStrategy
    {
        public Optimizer _optimizer = new Optimizer();
        private readonly Color _robotColor = Color.cyan;
        private readonly Color _shootPointColor = Color.blue;
        private readonly Color? _ballColor = Color.green;
        private List<IDebugHelper> _debugHelpers = new List<IDebugHelper>();
        private ModelUpdater _updater;
        private readonly IBaseSubstrategyExtension _extension;
        private readonly IAlgorithm _smartAlgorithm;
        private readonly IAlgorithm _smartNitroAlgorithm;
        private readonly IAlgorithm _simplifiedAlgorithm;
        private readonly IAlgorithm _simplifiedNitroAlgorithm;
        private readonly Throttler _throttler = new Throttler();
        private int _lastTickClotherEnemy = 0;
        private int _clotherEnemyTicsCount = 0;

        private bool EnabledTypeDebug()
        {
//            return _extension is AttackerExtension;
            return true;
//            return false;
        }
        
//        const int MaxCheckpoints = 1;

        const double MinTeammateReshootDistance = 15;
        const int MinPathsForSmartStrat = 32;
        const int MaxTicksWithoutRebuild = 30;
        const int MinActionsBeforeJumpForRebuild = 7;
        const int MaxTicksForGoalToOurGates = 150;
        const int AntiLK91TickCount = 5;
        const int AntiLK91MaxPathCountAllowed = 30;
        
        const int TicksAfterHit = 200;
        const int TickForEnemyAttack = 5;
        const int TicksAfterHitForSlowJump = 100;
        
        public BaseSubStrategy(IBaseSubstrategyExtension extension, int playersCount)
        {
            _extension = extension;
            _smartAlgorithm = new SmartAlgorithm(false, playersCount);
            _smartNitroAlgorithm = new SmartAlgorithm(true, playersCount);
            _simplifiedAlgorithm = new SimplifiedAlgorithm(false, playersCount);
            _simplifiedNitroAlgorithm = new SimplifiedAlgorithm(true, playersCount);
        }
        
        
        public void Act(ModelUpdater updater)
        {                      
            _updater = updater;
            _debugHelpers = new List<IDebugHelper>();                    
 
            if (_extension is DefenderExtension)
            {
                var attacker = _updater.Teammate;
                var optimizer = updater.chooser.GetOptimizer(attacker.id);
                var enemiesInFlight = updater.Enemies.Where(e => !e.touch);

                bool haveClotherFlyingEnemies = enemiesInFlight.Any() &&
                                                enemiesInFlight.Min(e =>
                                                    e.position.DropY().DistanceTo(updater.myMe.position.DropY()) < 30);

                if (attacker.position.z < _updater.myMe.position.z
                    && !_optimizer.VerifyAndAlign(updater)
                    && optimizer != null
                    && (!optimizer.VerifyAndAlign(updater) || optimizer.Saved.PredictResults.Count > 40)
                    && !haveClotherFlyingEnemies)
                {
                    StrategyChooser.TryToSwitchStrategies(updater.myMe.id, attacker.id);
                }
            }
                       
            logDebugHelpersBefore(_debugHelpers, updater);

            if (_throttler.Act(updater))
            {
                _debugHelpers.Add(new TextShower(_extension.Name + " THROTTLING"));
                return;
            }
            
            
            if (updater.myMe.touch && updater.Enemies.Any(e => !e.touch))
            {
                var paths = CalcInterceptPaths(updater);
                if (paths != null && paths.Any())
                {
                    var betterPath = SelectAndDebugBetterPath(paths);
                    if (!_optimizer.VerifyAndAlign(updater) ||
                        _optimizer.Saved.PredictResults.Count > betterPath.PredictResults.Count)
                    {
                        Console.WriteLine("INTERCEEPT");
                        _debugHelpers.Add(new TextShower("INTERCEEEEEPT"));
                        _optimizer.SaveValues(betterPath, updater.myMe);
                        updater.Action(betterPath.PredictResults[1].control);
                        return;
                    }
                }
            }

            if (updater.myMe.touch && _optimizer.VerifyAndAlign(updater.myBall, updater.Teammates, updater.Enemies))
            {
                int jumpAt = _optimizer.Saved.PredictResults.FindIndex(p => p.position.y > Constants.ROBOT_RADIUS);
                if (MyGame.CurrentTick - _optimizer.SavedAtTick > MaxTicksWithoutRebuild 
                    && (jumpAt == -1 || jumpAt > MinActionsBeforeJumpForRebuild))
                {
                    _debugHelpers.Add(new TextShower(_extension.Name + " REBUILD ON TICK TIMEOUT"));
                    _optimizer.Clear();
                }
                else
                {
                    bool optimizerCleared = false;
                    foreach (var oldRobot in updater.LastTickRobots)
                    {
                        if (oldRobot.isTeammate)
                            continue;
                        if (!oldRobot.touch && updater.myRobots[oldRobot.id].touch)
                        {
                            _debugHelpers.Add(new TextShower(_extension.Name + " REBUILD ON ROBOT DOWN"));
                            _optimizer.Clear();
                            optimizerCleared = true;
                            break;
                        }
                    }

                    if (!optimizerCleared)
                    {
                        var path = _optimizer.Saved;
                        if (path.PredictResults != null
                            && path.PredictResults.Count >= 2
                            && path.BallAfterHit != null
                            && path.BallAfterHit.Any())
                        {
                            foreach (var notMyOptimizer in notMyOptimizers())
                            {
                                if (!notMyOptimizer.VerifyAndAlign(updater.myBall, updater.Teammates, updater.Enemies))
                                    continue;

                                var teammatePath = notMyOptimizer.Saved;
                                if (teammatePath.BallAfterHit != null && teammatePath.PredictResults.Count <= path.PredictResults.Count)
                                {
                                    _optimizer.Clear();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            if (_optimizer.TryToMove(updater))
            {
                _debugHelpers.AddRange(_optimizer.Saved.ToDebugHelpers(_shootPointColor, _robotColor, _ballColor, _ballColor, 5, 1));
                _debugHelpers.Add(new TextShower("Optimizer on for " + _extension.Name));
                return;
            }
            else
            {
                _debugHelpers.Add(new TextShower("Optimizer cant work for " + _extension.Name));
            }
            
                        
            var dan = CollideLogic.dan_to_arena(updater.myMe.position);
            if (dan.distance > Constants.ROBOT_MIN_RADIUS + 0.0000001)
            {
                _debugHelpers.Add(new TextShower(_extension.Name + " in flight"));
                flyingAction(updater);
                return;
            }
            
            if (Math.Abs(updater.myMe.velocity.y) > 0.0000001)
            {
                _debugHelpers.Add(new TextShower(_extension.Name + " CORNER STRATEGY ON"));
                updater.Action(dan.normal.DropY().normalized * Constants.ROBOT_MAX_GROUND_SPEED);
                return;
            }
            
            if (_updater.myMe.position.DistanceTo(_updater.myBall.position) <
                Constants.ROBOT_MAX_RADIUS + Constants.BALL_RADIUS)
                throw new BallSoCloseException();

            
            int enemyHitTick;

            var pathNullable = CalcPath(updater, out enemyHitTick);
            if (pathNullable.HasValue)
            {
                var path = pathNullable.Value;
                _debugHelpers.Add(new TextShower("FINE PATH FOUND"));
                _optimizer.SaveValues(path, updater.myMe);
                updater.Action(path.PredictResults[1].control);
            }
            
            
            var clothestEnemy = updater.Enemies.MinBy(e =>
                e.position.DistanceTo(updater.myMe.position)); 
            if ((!pathNullable.HasValue || 
                pathNullable.Value.PredictResults.Count > AntiLK91MaxPathCountAllowed) &&
                clothestEnemy.position.DistanceTo(updater.myMe.position) < Constants.ROBOT_RADIUS + MoveCalculator.RadiusFromJumpSpeed(AntiLK91Exception.JumpSpeed))
            {
                if (_lastTickClotherEnemy == MyGame.CurrentTick - 1)
                {
                    ++_clotherEnemyTicsCount;
                    if(_clotherEnemyTicsCount > AntiLK91TickCount)
                    {
                        throw new AntiLK91Exception(clothestEnemy.position);
                    }
                }
                else
                {
                    _clotherEnemyTicsCount = 0;
                }

                _lastTickClotherEnemy = MyGame.CurrentTick;
            }
            else
            {
                _clotherEnemyTicsCount = 0;
            }
            

            if(pathNullable.HasValue || _extension.CanUseThisBall(new Point(updater.myBall)))
                CheckForTeammatePaths(updater, enemyHitTick);    
        }

        List<Optimizer> notMyOptimizers()
        {
            List<Optimizer> notMyOptimizers = new List<Optimizer>();
            foreach (var strat in _updater.chooser.GetStrategies())
            {
                if(!(strat is BaseSubStrategy))
                    continue;
                var baseStrat = (BaseSubStrategy) strat;
                if(baseStrat._optimizer == _optimizer)
                    continue;
                notMyOptimizers.Add(baseStrat._optimizer);
            }

            return notMyOptimizers;
        }

        void flyingAction(ModelUpdater updater)
        {
            var myControl = updater.LastUsedControl[updater.myMe.id];
            if (myControl.JumpSpeed == 0)
            {
                if (_extension is DefenderExtension)
                {
                    updater.Action(new Control(Vector3.down * 100, 0, true));
                }
                else
                {
                    updater.Action(new Control());
                }
                return;
            }
            
            var enemiesInFlight = updater.Enemies.Where(e => !e.touch).Select(r => (MyRobot)r.Clone()).ToList();
            List<Point> ballPredictedList = new List<Point>(){new Point(updater.myBall)};;
            var me = (MyRobot)updater.myMe.Clone();

            
            for (int i = 0; i < 50; ++i)
            {
                var ballPredicted = MovePredictor.PredictBallPointOptimized(ballPredictedList.Last());
                ballPredictedList.Add(ballPredicted);
                double lastYPos = me.position.y;
                CollideLogic.update_for_jump(me, myControl);
                foreach (var enemy in enemiesInFlight)
                {
                    var enemyControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, enemy.radiusChangeSpeed,
                        enemy.usedNitroOnLastTick);

                    CollideLogic.update_for_jump(enemy, enemyControl);
                }
                
                if(lastYPos > me.position.y)
                    break;

                if (ballPredicted.position.DistanceTo(me.position) < Constants.BALL_RADIUS + me.radius
                    || enemiesInFlight.Any(e => e.position.DistanceTo(me.position) < e.radius + me.radius))
                {
                    updater.Action(myControl);
                    return;
                }
            }



            updater.Action(new Control());
        }

        public void CheckForTeammatePaths(ModelUpdater updater, int enemyHitTick)
        {
            var path = _optimizer.Saved;
           
            foreach (var notMyOptimizer in notMyOptimizers())
            {
                if(!notMyOptimizer.VerifyAndAlign(updater.myBall, updater.Teammates, updater.Enemies))
                    continue;

                var teammatePath = notMyOptimizer.Saved;
                if (teammatePath.BallAfterHit != null
                    && (path.PredictResults == null 
                        || path.PredictResults.Count < 2 
                        || teammatePath.PredictResults.Count <= path.PredictResults.Count))
                {
                    var pathAfterTeammate = CalcPathAfterTeammate(teammatePath, new Point(updater.myMe));
                    if (!pathAfterTeammate.HasValue)
                    {
                        if (_extension is AttackerExtension)
                        {
                            Path pathLikePathsNotFound = new Path();
                            
                            pathLikePathsNotFound.BallBeforeHit = new List<Point>(teammatePath.BallBeforeHit);
                            pathLikePathsNotFound.PredictResults = new List<Point>()
                            {
                                new Point(updater.myMe)
                            };
                            
                            var clotherTeammate = _updater.Teammates
                                .Where(q => q.id != updater.myMe.id )
                                .MinBy(t => t.position.DistanceTo(updater.myMe.position));
                            if (clotherTeammate.position.DistanceTo(updater.myMe.position) <
                                Constants.ROBOT_MAX_RADIUS + Constants.ROBOT_RADIUS)
                            {
                                pathLikePathsNotFound.Type = PathType.RunFromTeammate;
                                _debugHelpers.Add(new TextShower("CANT GIVE ROAD, TEAMMATE TOO CLOSE, RUN FROM HIM"));
                                pathLikePathsNotFound.PredictResults.Add(new Point(
                                    Vector3.zero, Vector3.zero,
                                    new Control(-MoveCalculator.TargetToVelocity(updater.myMe.position, clotherTeammate.position))));
                            }
                            else
                            {
                                _debugHelpers.Add(new TextShower("CANT GIVE ROAD, SO JUST ATTACK TO CLOTHER ENEMY"));

                                var clothestEnemy =
                                    _updater.Enemies.MinBy(e => e.position.DistanceTo(_updater.myMe.position));
                                _throttler.Start(clothestEnemy.id, updater);
                                return;
                            }

                            pathAfterTeammate = pathLikePathsNotFound;
                        }
                        else
                        {
                            _debugHelpers.Add(new TextShower("CANT GIVE ROAD, SO DOING LIKE PATH NOT FOUND"));
                            Path pathLikePathsNotFound = new Path();
                            pathLikePathsNotFound.Type = PathType.PathNotFound;
                            pathLikePathsNotFound.BallBeforeHit = new List<Point>(teammatePath.BallBeforeHit);
                            pathLikePathsNotFound.PredictResults = new List<Point>(){new Point(updater.myMe)};
                            for (int i = 0; i < teammatePath.PredictResults.Count; ++i)
                            {
                                var lastPredict = pathLikePathsNotFound.PredictResults.Last();
                                pathLikePathsNotFound.PredictResults.Add(
                                    MovePredictor.PredictNextPoint(lastPredict, _extension.PathsNotFound(lastPredict, teammatePath.BallBeforeHit[i], updater)));
                            }
    
                            pathAfterTeammate = pathLikePathsNotFound;                            
                        }
                    }
                    else
                    {
                        _debugHelpers.Add(new TextShower("ROAD TO THE TEAMMATE"));
                    }

                    var p = pathAfterTeammate.Value;
                    p.EnemyHitTick = enemyHitTick;
                    _optimizer.SaveValues(p, updater.myMe);
                    updater.Action(p.PredictResults[1].control);

                    return;
                }
            }
        }


        void logDebugHelpersBefore(List<IDebugHelper> debugHelpers, ModelUpdater _updater)
        {
            if (_optimizer.Saved.PredictResults != null)
            {
                if (_optimizer.Saved.PredictResults.Count > 1)
                {
                    debugHelpers.Add(new TextShower(String.Format("{3} POS: Actual {0}, expected {1}, diff {2}",
                        _updater.myMe.position,
                        _optimizer.Saved.PredictResults[1].position,
                        _updater.myMe.position - _optimizer.Saved.PredictResults[1].position,
                        this._extension.Name
                    )));
                    debugHelpers.Add(new TextShower(String.Format("{3} POS GROUND: Actual {0}, expected {1}, diff {2}",
                        _updater.myMe.position.DropY(),
                        _optimizer.Saved.PredictResults[1].position.DropY(),
                        _updater.myMe.position.DropY() -_optimizer.Saved.PredictResults[1].position.DropY(),
                        this._extension.Name
                    )));
                    debugHelpers.Add(new TextShower(String.Format("{3} VEL: Actual {0}, expected {1}, diff {2}",
                        _updater.myMe.velocity,
                        _optimizer.Saved.PredictResults[1].velocity,
                        _updater.myMe.velocity -_optimizer.Saved.PredictResults[1].velocity,
                        this._extension.Name
                    )));
//                    debugHelpers.Add(new TextShower(String.Format("{3} BALL POS: Actual {0}, expected {1}, diff {2}",
//                        _updater.myBall.position,
//                        _optimizer.Saved.BallPredict[1].position,
//                        _updater.myBall.position -_optimizer.Saved.BallPredict[1].position,
//                        this._extension.Name
//                    )));
//                    debugHelpers.Add(new TextShower(String.Format("{3} BALL VEL: Actual {0}, expected {1}, diff {2}",
//                        _updater.myBall.velocity,
//                        _optimizer.Saved.BallPredict[1].velocity,
//                        _updater.myBall.velocity -_optimizer.Saved.BallPredict[1].velocity,
//                        this._extension.Name
//                    )));
                    
                    
                    if (Math.Abs((_updater.myMe.velocity - _optimizer.Saved.PredictResults[1].velocity).x) > 1e-4 && 
                        _optimizer.Saved.PredictResults[1].control.JumpSpeed > 0 &&
                        _optimizer.Saved.BallBeforeHit[1].position == _updater.myBall.position &&
                        _updater.myRobots.Values
                            .Where(r => r.id != _updater.myMe.id)
                            .Min(r => r.position.DistanceTo(_updater.myMe.position)) > Constants.ROBOT_RADIUS * 3)
                    {
                        var text = this._extension.Name + " DIFF FOUND " + MyGame.CurrentTick;
                        debugHelpers.Add(new TextShower(text));
                        Console.WriteLine(text);
                    }
                }
            }
        }       
        
        List<Path> CollectPaths(ModelUpdater updater, IAlgorithm algorithm, int hardhoredTickToFuture, bool beliveInAfterHitPredict, out Path stupidPath, out bool goalFound, out int enemyHitTick)
        {            
            List<Point> ballPredictedList = new List<Point>(){new Point(updater.myBall)};;

            var paths = new List<Path>();

            goalFound = false;
            int firstFoundTick = -1;
            int lastFoundTick = -1;
            int checkpoints = 0;
            enemyHitTick = 0;
            
            List<int> foundIndexes = new List<int>();

            var enemiesInFlight = updater.Enemies.Where(e => !e.touch).Select(r => (MyRobot)r.Clone()).ToList();

            bool canCheckNext = !(_extension is DefenderExtension);

            for (int i = 0;;++i)
            {
                if(hardhoredTickToFuture != 0 && i >= hardhoredTickToFuture)
                    break;
                
                if (!canCheckNext)
                {
                    if (i > _extension.TicksToFuture && checkpoints == 0)
                        break;

                    if (firstFoundTick != -1 && i - firstFoundTick > algorithm.Settings().MaxCheckpoints * algorithm.Settings().CheckpointTicksCount)
                        break;

                    if (checkpoints != 0)
                    {
                        if (checkpoints >= algorithm.Settings().MaxCheckpoints)
                            break;
                    }
                }


                var ballPredicted = MovePredictor.PredictBallPointOptimized(ballPredictedList.Last());
                
                if (canCheckNext)
                {
                    if (updater.Enemies.Any(e => MovePredictor.PredictPointsRoughlyGround(new Point(e), ballPredicted.position, i).Count <= i))
                    {
                        canCheckNext = false;
                    }
                }

                if (enemyHitTick == 0)
                {
                    foreach (var enemy in enemiesInFlight)
                    {
                        var enemyControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, enemy.radiusChangeSpeed,
                            enemy.usedNitroOnLastTick);

                        CollideLogic.update_for_jump(enemy, enemyControl);

                        if (enemy.position.DistanceTo(ballPredicted.position) < Constants.BALL_RADIUS + enemy.radius)
                        {
                            var newBall = new MyBall(ballPredictedList.Last().position,
                                ballPredictedList.Last().velocity);
                            MovePredictor.CollideRobotAndBall(enemy, newBall, new Control(
                                Vector3.up * Constants.MAX_ENTITY_SPEED,
                                enemy.radiusChangeSpeed,
                                enemy.usedNitroOnLastTick));
                            ballPredicted = new Point(newBall.position, newBall.velocity);
                            enemiesInFlight.Clear();
                            enemyHitTick = i;
                            break;
                        }
                    }
                }


                ballPredictedList.Add(ballPredicted);
                
                if (MoveCalculator.IsBallInMyGates(ballPredictedList.Last().position))
                {
                    goalFound = true;
                    break;
                }
                
                if(checkpoints > 0 && i - lastFoundTick < algorithm.Settings().CheckpointTicksCount)
                    continue;              


                var shootPoints = algorithm.CalcBallShootPoints(ballPredicted.position);
                if (!shootPoints.Any())
                    continue;
                
                if(!_extension.CanUseThisBall(ballPredicted))
                    continue;
                    
                var hitZoneError = algorithm.BallInHitZone(
                    ref shootPoints, 
                    new Point(updater.myMe),
                    updater.myMe.nitroAmount,
                    i + 2);

                List<CalcResult> calcResults = null;
                if (hitZoneError == BallHitZoneError.NoError)
                    calcResults = CalcActionsForBall(
                        ballPredictedList, 
                        shootPoints.Where(sp => foundIndexes.Count(index => index == sp.Index) < algorithm.Settings().ShootPointsRecheck).ToList(), 
                        new List<Point>(){ new Point(updater.myMe)},
                        i + 2,  
                        algorithm);
              
                if(calcResults == null)
                    continue;

                calcResults = calcResults.Where(r => r.PathError == PredictError.NoError).ToList();
                
                foundIndexes.AddRange(calcResults.Select(p => p.ShootPoint.Index));
                
                if (hitZoneError != BallHitZoneError.NotSoFastError
                    && !_extension.CanUseThisBall(ballPredictedList.First()) 
                    && _extension.CanUseThisBall(ballPredictedList.Last())
                    && algorithm is SmartAlgorithm)
                {
                    var waitingFoundIndexes = new List<int>(foundIndexes);
                    
                    for (int j = 1; j < ballPredictedList.Count - 2; ++j)
                    {
                        var notFoundShootPoints = shootPoints.Where(sp => !waitingFoundIndexes.Contains(sp.Index)).ToList();
                        if(!notFoundShootPoints.Any())
                            break;

                        var stayingActions = new List<Point>()
                        {
                            new Point(
                                updater.myMe.position, 
                                updater.myMe.velocity)
                        };
                        var predictedMyMove = stayingActions.First();
                        for (int k = 0; k < j; k++)
                        {
                            var control = _extension.PathsNotFound(predictedMyMove, ballPredictedList[ballPredictedList.Count - 1 - k], updater);
                            predictedMyMove = MovePredictor.PredictNextPoint(predictedMyMove, control);
                            stayingActions.Add(predictedMyMove);
                        }

                        int forTick = i + 2 - j;

                        hitZoneError = algorithm.BallInHitZone(ref notFoundShootPoints, stayingActions.Last(), updater.myMe.nitroAmount, forTick);
                        if(hitZoneError == BallHitZoneError.NotSoFastError)
                            break;
                        if(hitZoneError == BallHitZoneError.SomeError || hitZoneError == BallHitZoneError.NotSoSlowError)
                            continue;
                        
                        var newActions = CalcActionsForBall(
                            ballPredictedList, 
                            notFoundShootPoints, 
                            stayingActions, 
                            forTick, 
                            algorithm);
                        
                        if(newActions == null || !newActions.Any())
                            continue;
                        
                        waitingFoundIndexes.AddRange(newActions
                            .Where(r => r.PathError != PredictError.NotSoSlow)
                            .Select(p => p.ShootPoint.Index));
                        waitingFoundIndexes = waitingFoundIndexes.Distinct().ToList();
                        calcResults.AddRange(newActions.Where(r => r.PathError == PredictError.NoError));
                    }
                }

                calcResults = calcResults.Where(r => r.PathError == PredictError.NoError).ToList();

                if (enemyHitTick != 0 && !beliveInAfterHitPredict)
                {
                    var enemyHitTickCopy = enemyHitTick;
                    calcResults = calcResults.Where(c =>
                        c.Path.PredictResults.FindIndex(p => p.control.JumpSpeed > 0) > enemyHitTickCopy).ToList();
                }

                if(calcResults.Count == 0)
                    continue;
                
                
                foundIndexes.AddRange(calcResults.Select(p => p.ShootPoint.Index));
                
                checkpoints += 1;
                lastFoundTick = i;
                if (firstFoundTick == -1)
                {
                    firstFoundTick = i;
                }
                
                var disallowed = new List<CalcResult>();
                foreach (var calcResult in calcResults)
                {
                    if (calcResult.Path.BallAfterHit == null)
                    {
                        disallowed.Add(calcResult);
                        continue;
                    }

                    var gatesIndex =
                        calcResult.Path.BallAfterHit.FindIndex(p => MoveCalculator.IsBallInMyGates(p.position));
                    if(gatesIndex == -1)
                        continue;

                    if (gatesIndex < MaxTicksForGoalToOurGates)
                    {
                        disallowed.Add(calcResult);
                        continue;
                    }

                    if (calcResult.Path.BallAfterHit[gatesIndex].position.y > Constants.ROBOT_MAX_JUMP_HEIGHT)
                    {
                        disallowed.Add(calcResult);
                    }
                }
                    
                    
                foreach (var pathInMyGates in disallowed)
                {
                    _debugHelpers.AddRange(pathInMyGates.Path.ToDebugHelpers(null, null, null, Color.black));
                }
                calcResults = calcResults.Except(disallowed).ToList();

                if (!calcResults.Any())
                {
                    DebugHelpers.Add(new TextShower("All roads lead to MY GATES"));
                }

//                    actions = actions.Where(path => MoveCalculator.ValidateOnArenaInner(path.PredictResults)).ToList();


                paths.AddRange(calcResults.Select(r => r.Path));
            }

            _debugHelpers.Add(new TextShower(String.Format(
                _extension.Name + " paths total {0}, ticks after first found {1}, checkpoints {2}",
                paths.Count,
                lastFoundTick - firstFoundTick,
                checkpoints)));
            
            stupidPath = new Path();
            stupidPath.Type = PathType.PathNotFound;
            stupidPath.BallBeforeHit = ballPredictedList;
            stupidPath.PredictResults = new List<Point>(){new Point(updater.myMe)};
            for (int k = 0; k < ballPredictedList.Count; k++)
            {
                var control = _extension.PathsNotFound(stupidPath.PredictResults.Last(), ballPredictedList[k], updater);
                stupidPath.PredictResults.Add(MovePredictor.PredictNextPoint(stupidPath.PredictResults.Last(), control));
            }

            for (int i = 0; i < paths.Count; ++i)
            {
                var t = paths[i];
                t.EnemyHitTick = enemyHitTick;
                paths[i] = t;
            }

            return paths;
        }
        
        
        public List<CalcResult> CalcActionsForBall(List<Point> ballPredictedList, 
            List<BallShootPoint> shootPoints, List<Point> stayingActions, int forTick, IAlgorithm algorithm)
        {
            var ballPredicted = ballPredictedList.Last();

            if (!_extension.CanUseThisBall(ballPredicted))
                return null;
                        
            var result = new List<CalcResult>();

            var predicted = stayingActions.Last();
            
            for(int k = 0; k < shootPoints.Count; ++k)
            {
                var shootPoint = shootPoints[k];
                
                var predictActions = algorithm.MakePointsWithJump(
                    predicted,
                    shootPoint.GlobalPoint,
                    forTick,
                    _updater.myMe.nitroAmount);

                
                if (predictActions.Error != PredictError.NoError)
                {
                    if (predictActions.Error == PredictError.NotSoFast && algorithm is SmartAlgorithm)
                    {
                        for (int q = k; q < shootPoints.Count; ++q)
                        {
                            result.Add(new CalcResult(shootPoints[q], predictActions.Error, new Path()));
                        }
                        break;
                    }
                    else
                    {
                        result.Add(new CalcResult(shootPoint, predictActions.Error, new Path()));
                    }
                    
                    
                    if(predictActions.Error == PredictError.LoopTimeout)
                    {
                        Console.WriteLine(_extension.Name + " ERROR " + predictActions.Error + " AT TICK " + MyGame.CurrentTick);
                    }

                    continue;
                }
                
                if (predictActions.Predicted == null || predictActions.Predicted.Count < 2)
                    continue;
            
                predictActions.Predicted.InsertRange(0, stayingActions.Take(stayingActions.Count - 1));


                foreach (var path in MovePredictor.CalcBallsAfterHit(ballPredictedList, predictActions.Predicted, shootPoint, _updater.myMe.nitroAmount, TicksAfterHit))
                {
                    result.Add(new CalcResult(shootPoint, PredictError.NoError, path));
                }
            }

            return result;
        }

        List<Path> CalcInterceptPaths(ModelUpdater updater)
        {
            var enemiesInFlight = updater.Enemies.Where(e => !e.touch).Select(r => (MyRobot)r.Clone()).ToList();
            List<Point> ballPredictedList = new List<Point>(){new Point(updater.myBall)};;

            int enemyHitTick = 0;
            int enemyTargetId = 0;
            
            for (int i = 0; i < _extension.TicksToFuture && enemyTargetId == 0; ++i)
            {
                var ballPredicted = MovePredictor.PredictBallPointOptimized(ballPredictedList.Last());
                ballPredictedList.Add(ballPredicted);
                
                foreach (var enemy in enemiesInFlight)
                {
                    var enemyControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, enemy.radiusChangeSpeed,
                        enemy.usedNitroOnLastTick);

                    CollideLogic.update_for_jump(enemy, enemyControl);

                    if (enemy.position.DistanceTo(ballPredicted.position) < Constants.BALL_RADIUS + enemy.radius)
                    {
                        enemiesInFlight.Clear();
                        enemyHitTick = i;
                        enemyTargetId = enemy.id;
                        break;
                    }
                }
            }

            if (enemyTargetId == 0)
                return null;

            Path kostyl;
            bool kostyl2;
            int kostyl3;
            var paths = CollectPaths(updater, _simplifiedNitroAlgorithm, enemyHitTick + 1, false, out kostyl, out kostyl2, out kostyl3);
            if (paths.Any())
            {
                return paths;
            }

            var targetEnemy = (MyRobot)updater.Enemies.First(e => e.id == enemyTargetId).Clone();
            var enemyPath = new List<Point>() {new Point(targetEnemy)};
            for (int i = 0; i < enemyHitTick; ++i)
            {
                var enemyControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, targetEnemy.radiusChangeSpeed,
                    targetEnemy.usedNitroOnLastTick);

                CollideLogic.update_for_jump(targetEnemy, enemyControl);
                enemyPath.Add(new Point(targetEnemy));

                PredictPointsResult myPath = _simplifiedNitroAlgorithm.MakePointsWithJump(new Point(updater.myMe), targetEnemy.position, i + 2,
                    updater.myMe.nitroAmount);
                if(myPath.Predicted == null || myPath.Error != PredictError.NoError)
                    continue;

                for (int j = 0; j < Math.Min(enemyPath.Count, myPath.Predicted.Count); ++j)
                {
                    if (enemyPath[j].position.DistanceTo(myPath.Predicted[j].position) < Constants.ROBOT_RADIUS * 2)
                    {
                        Console.WriteLine("INTERCEPT HARD " + MyGame.CurrentTick);
                        
                        var path = new Path();
                        path.Type = PathType.InterceptHard;
                        path.BallBeforeHit = ballPredictedList;
                        path.PredictResults = myPath.Predicted;
                        path.EnemyHitTick = enemyHitTick;
                        path.BallAfterHit = new List<Point>();
                        return new List<Path>(){path};
                    }
                }
            }
            
            return new List<Path>();
        }

        Path? CalcPath(ModelUpdater updater, out int enemyHitTick)
        {
//            var robot = new TmpRobot();
//            robot.position = updater.myMe.position;
//            robot.velocity = updater.myMe.velocity;
//            robot.nitro = updater.myMe.nitroAmount;
//            robot.touch = true;
//
//            var pr = new List<PredictResult>()
//            {
//                new PredictResult(updater.myMe)
//            };
//            
//            List<PredictResult> ballPredictedList = new List<PredictResult>(){new PredictResult(updater.myBall)};;
//            var t = MovePredictor.MakePredictNitro(pr.First(), updater.myMe.position + new Vector3(1, 4, 1), 50);
//            for (int i = 0; i < 10; ++i)
//            {
////                var control = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, Constants.ROBOT_MAX_JUMP_SPEED,
////                    true);
////                CollideLogic.update_for_jump(robot, control);
////                pr.Add(new PredictResult(robot.position, robot.velocity, control));
//                ballPredictedList.Add(MovePredictor.PredictBallMove(ballPredictedList.Last()));
//            }
//            
//
//            Path pp= new Path();
//            pp.BallBeforeHit = ballPredictedList;
//            pp.PredictResults = t;
//            return pp;
            
            List<Path> paths = null;
            Path stupidPath;
            bool goalFound;

            bool useNitro = _extension is DefenderExtension &&
                            updater.Enemies.Min(e => e.position.DistanceTo(updater.myMe.position)) < 30;

            var smartAlg = useNitro ? _smartNitroAlgorithm : _smartAlgorithm;
            var simpleAlg = useNitro ? _simplifiedNitroAlgorithm : _simplifiedAlgorithm;
            
            paths = CollectPaths(updater, smartAlg, 0, false, out stupidPath, out goalFound, out enemyHitTick);
            
            if (paths.Count < MinPathsForSmartStrat)
            {
                if (!_extension.CanUseThisBall(new Point(updater.myBall.position, updater.myBall.velocity)))
                {
                    var control = _extension.PathsNotFound(new Point(updater.myMe), new Point(updater.myBall), updater);
                    _updater.Action(control);
                    _debugHelpers.Add(new TextShower(_extension.Name + " WAITING FOR BETTER MOMENT"));
                    return null;
                }          
                
                var stupid = CollectPaths(updater, simpleAlg, 0, false, out stupidPath, out goalFound, out enemyHitTick);
                if (stupid != null)
                {
                    paths.AddRange(stupid);
                    _debugHelpers.Add(
                        new TextShower(_extension.Name + " STUPID STRAT ADD OWN " + stupid.Count +
                        " PATHS"));
                }
                else
                {
                    _debugHelpers.Add(new TextShower(_extension.Name + " STUPID STRAT CANT FIND PATHS"));
                }
            }

            if (!paths.Any() && goalFound)
            {
                _debugHelpers.Add(new TextShower(_extension.Name + " NITRO SEARCH USING BALL MOVE PREDICT"));
                Path kostyl;
                bool kostyl2;
                int kostyl3;
                paths = CollectPaths(updater, _simplifiedNitroAlgorithm, 0, true, out kostyl, out kostyl2, out kostyl3);
            }
            
            if (!paths.Any())
            {
                _debugHelpers.Add(new TextShower("PATH NOT FOUND, TRYING TO USING DEFAULT"));
                return stupidPath;                    
            }


            return SelectAndDebugBetterPath(paths);
        }

        Path SelectAndDebugBetterPath(List<Path> paths)
        {
            var qualities = CalcQuality(paths);

            var betterPath = qualities.MaxBy(qp => qp.TotalValue);

            if (MyStrategy.DumpAdditionalRenderObjects() && EnabledTypeDebug())
            {
//                Console.WriteLine(_extension.Name + " tick " + MyGame.CurrentTick + "\n");
                
                foreach (var qp in qualities)
                {
                    var pathTopValue = qp.Path.BallAfterHit.Any() ? qp.Path.BallAfterHit.MaxBy(p => p.position.y) : qp.Path.PredictResults.First();
                    
                    

//                    foreach (var qpQualityResult in qp.QualityResults)
//                    {
//                        Console.WriteLine("Index " + qp.Index  +
//                                          qpQualityResult.Quality.GetType().Name + 
//                                          " relative " + qpQualityResult.RelativeValue +
//                                          " absolute " + qpQualityResult.AbsoluteValue);
//                    }

                    if (qp.Path.PredictResults == betterPath.Path.PredictResults)
                    {
                        _debugHelpers.Add(new NumberShower(qp.Index, pathTopValue.position, Color.blue));
                        _debugHelpers.AddRange(qp.Path.ToDebugHelpers(
                            Color.red,
                            _robotColor,
                            _ballColor,
                            Color.blue,
                            3));
                    }
                    else
                    {
                        _debugHelpers.Add(new NumberShower(qp.Index, pathTopValue.position, Color.red));
                        _debugHelpers.AddRange(qp.Path.ToDebugHelpers(
                            _shootPointColor,
                            _robotColor,
                            null,
                            new Color(qp.TotalValue, qp.TotalValue, qp.TotalValue, 0.7)));
                    }
                }
            }
            
            return betterPath.Path;
        }
        

        bool RobotsCollided(List<Point> path1, List<Point> path2)
        {
            for (int i = 0; i < Math.Min(path1.Count, path2.Count); ++i)
            {
                if (path1[i].position.DistanceTo(path2[i].position) < Constants.ROBOT_RADIUS * 2.5)
                    return true;
            }

            return false;
        }

        

        Path? CalcPathAfterTeammate(Path teammatePath, Point predicted, bool simplified = false)
        {
            int ticksToHit = teammatePath.PredictResults.Count;
            for (int i = 0; i < teammatePath.BallAfterHit.Count; ++i)
            {
                int forTick = ticksToHit + i;
                var ballPoint = teammatePath.BallAfterHit[i].position - new Vector3(0, Constants.BALL_RADIUS, 0);
                
                if(ballPoint.y > 4)
                    continue;
                
                if(!MoveCalculator.RobotPhysicallyCanRunToBall(predicted.position, ballPoint, forTick))
                    continue;
                
                if(teammatePath.ShootPoint.BallPoint.DistanceTo(ballPoint) < MinTeammateReshootDistance)
                    continue;
                
                
                if(!_extension.CanUseThisBall(new Point(ballPoint, Vector3.zero)))
                    continue;
                
//                if(MoveCalculator.LineSegmentsIntersection(predicted.position, ballPoint, 
//                    teammatePath.PredictResults.First().position, teammatePath.PredictResults.Last().position))
//                    continue;
                
                var alg = simplified ? _simplifiedAlgorithm : _smartAlgorithm;
                
                var myPredictResults = alg.MakePointsWithJump(
                    predicted, 
                    ballPoint, 
                    forTick,
                    _updater.myMe.nitroAmount); 
                
                if(myPredictResults.Error != PredictError.NoError || myPredictResults.Predicted == null)
                    continue;
                
                if(RobotsCollided(myPredictResults.Predicted, teammatePath.PredictResults))
                    continue;
                
                if(myPredictResults.Predicted.FindIndex(p => p.control.JumpSpeed > 0) < teammatePath.PredictResults.Count + 4)
                    continue;
                
                var path = new Path();
                path.Type = PathType.RecatchBall;
                path.BallBeforeHit = new List<Point>(teammatePath.BallBeforeHit);
                path.PredictResults = myPredictResults.Predicted;
                path.EnemyHitTick = teammatePath.EnemyHitTick;
                return path;
            }

            if (!simplified)
                return CalcPathAfterTeammate(teammatePath, predicted, true);

            return null;
        }

        public List<IDebugHelper> DebugHelpers
        {
            get { return _debugHelpers; }
        }


        public string Name
        {
            get { return _extension.Name; }
            
        }

        struct QualityResult
        {
            public IQualityRating Quality;
            public readonly double AbsoluteValue;
            public double RelativeValue;
            public readonly Path Path; 

            public QualityResult(IQualityRating quality, Path path, double absoluteValue, double relativeValue = 0)
            {
                Quality = quality;
                AbsoluteValue = absoluteValue;
                Path = path;
                RelativeValue = relativeValue;
            }
        }

        struct TotalQualityResult
        {
            public readonly List<QualityResult> QualityResults;
            public readonly Path Path;
            public double TotalValue;
            public int Index;

            public TotalQualityResult(Path path, List<QualityResult> qualityResults, int index)
            {
                Path = path;
                QualityResults = qualityResults;
                Index = index;
                TotalValue = 0;
            }
        }

        
        private List<TotalQualityResult> CalcQuality(List<Path> paths)
        {
//            paths = paths.Where(path =>
//                path.BallAfterHit != null && 
//                !path.BallAfterHit.Any(p => MoveCalculator.IsBallInMyGates(p.position))).ToList();
//            
//            if (!paths.Any())
//                return null;
            
            List<Tuple<double, IQualityRating>> qualities = _extension.GetQualities(paths, _updater);
            
            var totalWeight = qualities.Sum(q => q.Item1);

            Dictionary<Path, TotalQualityResult> result = paths.ToDictionary(
                t => t, 
                t => new TotalQualityResult(t, new List<QualityResult>(), 0));

            foreach (var quality in qualities)
            {   
                var sorted = paths.
                    Select(p => new QualityResult(quality.Item2, p, quality.Item2.Calc(p))).
                    OrderBy(p => p.AbsoluteValue).ToArray();
                
                var distincted = sorted.Select(p => p.AbsoluteValue).Distinct().ToList();
                
                for (int i = 0; i < sorted.Length; ++i)
                {
                    sorted[i].RelativeValue = ((double) distincted.FindIndex(p => p == sorted[i].AbsoluteValue) + 1.0) /
                                              distincted.Count;
                    var t = result[sorted[i].Path];
                    t.TotalValue += sorted[i].RelativeValue * (quality.Item1 / totalWeight);
                    t.QualityResults.Add(sorted[i]);
                    result[sorted[i].Path] = t;
                }
            }

            var list = result.Values.ToList();

            for (int i = 0; i < list.Count; ++i)
            {
                var t=list[i]; 
                t.Index = i;
                list[i] = t;
            }

            return list;
        }
    }
}