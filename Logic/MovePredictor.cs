using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Common;
using ConsoleApp1.Structs;
using ConsoleApp1.SubStrategy;

namespace ConsoleApp1.Logic
{      
    public struct JumpPredict
    {
        public JumpPredict(List<double> heights)
        {
            this.heights = heights;
        }
        
        public List<double> heights;
    }
    
    public enum PredictError
    {
        NoError,
        NotSoFast,
        NotStabilized,
        TooFewTicksToJump,
        NotSoSlow,
        LoopTimeout,
        Unknown
    }
    public struct PredictPointsResult
    {
        public PredictError Error;
        public List<Point> Predicted;

        public PredictPointsResult(PredictError error, List<Point> predicted = null)
        {
            Error = error;
            Predicted = predicted;
        }
    }

    public static class MovePredictor
    {
        public const int MaxTicksLoop = 300;
        
        public static int FindJumpTick(List<double> jumpHeights, double height, double eps = 0.1)
        {
            for (int i = 0; i < jumpHeights.Count; ++i)
            {
                if (jumpHeights[i] > height)
                    return Math.Max(1, i);
            }

            return jumpHeights.Count - 1;
        }
        private static Dictionary<Control, JumpPredict> JumpPredicts = new Dictionary<Control, JumpPredict>();
        
        public static JumpPredict MakeJumpPredict(Control control, double nitroAmount)
        {
            if (JumpPredicts.ContainsKey(control))
                return JumpPredicts[control];
            
            Vector3 position = new Vector3(0, Constants.ROBOT_RADIUS);
            Vector3 velocity = new Vector3(30, 0, 0);

            var result = new JumpPredict(new List<double>());            
           
            var robot = new MyRobot();
            robot.position = position;
            robot.velocity = velocity;
            robot.nitroAmount = nitroAmount;
            robot.touch = true;
            robot.touchNormal = Vector3.up;
            
            for (int i = 0;; ++i)
            {
                CollideLogic.update_for_jump(robot, control);

                if (robot.velocity.y < 0)
                {
                    JumpPredicts[control] = result;
                    return result;
                }

                result.heights.Add(robot.position.y);
            }
        }
        
        public static void CollideRobotAndBall(MyRobot robot, MyBall ball, Control control)
        {
            for (int j = 0; j < Constants.MICROTICKS_PER_TICK; ++j)
            {                    
                ball.position += ball.velocity * Constants.MICROTICK_DELTA_TIME;
                ball.position.y -= Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME * Constants.MICROTICK_DELTA_TIME / 2D;
                ball.velocity.y -= Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME;
                CollideLogic.update_microtick_for_jump(robot, control);

                CollideLogic.collide_entities(robot, ball);
            }
        }

        private static List<double> JumpSpeedsForHit = new List<double>()
        {
//            Constants.ROBOT_MAX_JUMP_SPEED / 3 * 2,
//            Constants.ROBOT_MAX_JUMP_SPEED / 3,
            Constants.ROBOT_MAX_JUMP_SPEED,
            Constants.ROBOT_MAX_JUMP_SPEED / 2,
            0,
        };
        
        public static List<Path> CalcBallsAfterHit(List<Point> ballPredictedList, List<Point> robotPredictedList, BallShootPoint shootPoint, double nitroAmount, int ticksToFuture)
        {
               
            var results = new List<Path>();
            for (int i = 1; i < Math.Min(ballPredictedList.Count, robotPredictedList.Count); ++i)
            {
                if(!CollideLogic.has_penetration(
                    ballPredictedList[i].position, robotPredictedList[i].position, Constants.BALL_RADIUS, Constants.ROBOT_MAX_RADIUS))
                    continue;

                var jumpSpeeds = JumpSpeedsForHit;
                if(i == 1)
                    jumpSpeeds = new List<double>(){robotPredictedList[i].control.JumpSpeed};
                
                var ballPredictedListCutted = ballPredictedList.Take(i + 1).ToList();
                
                foreach (var jumpSpeed in jumpSpeeds)
                {
                    double robotRadius = MoveCalculator.RadiusFromJumpSpeed(jumpSpeed);
                    if(!CollideLogic.has_penetration(
                        ballPredictedList[i].position, robotPredictedList[i].position, Constants.BALL_RADIUS, robotRadius))
                        break;

                    var control = robotPredictedList[i].control;
                    control.JumpSpeed = jumpSpeed;
                    
                    var robot = new MyRobot(
                        robotPredictedList[i-1].position,
                        robotPredictedList[i-1].velocity,
                        robotRadius,
                        jumpSpeed,
                        nitroAmount);
                    var ball = new MyBall(
                        ballPredictedListCutted[i-1].position,
                        ballPredictedListCutted[i-1].velocity);
                
                    MovePredictor.CollideRobotAndBall(robot, ball, control);
                    
                    
                    var newPath = new Path();
                    newPath.Type = PathType.BallHit;
                    newPath.PredictResults = robotPredictedList.Take(i).ToList();
                    var collidePoint = robotPredictedList[i];
                    collidePoint.control = control;
                    newPath.PredictResults.Add(collidePoint);
                    newPath.ShootPoint = shootPoint;
                    newPath.BallBeforeHit = ballPredictedListCutted;

                    List<Point> ballAfterHit = new List<Point>(ticksToFuture + 1);
                    ballAfterHit.Add(new Point(ball.position, ball.velocity));
                
                    for (int j = 0; j < ticksToFuture; ++j)
                    {
                        var newPoint = PredictBallPointOptimized(ballAfterHit.Last());
                        ballAfterHit.Add(newPoint);
                        if (MoveCalculator.IsBallInEnemyGates(newPoint.position) ||
                            MoveCalculator.IsBallInMyGates(newPoint.position))
                        {
                            ballAfterHit.Add(PredictBallPointOptimized(ballAfterHit.Last()));
                            break;
                        }
                    }

                    newPath.BallAfterHit = ballAfterHit;
                    results.Add(newPath);
                }
                
                break;
            }
                
            
            return results;
        }
        
        
        public static Point PredictBallPoint(Vector3 position, Vector3 velocity)
        {
            for (int j = 0; j < 100; ++j)
            {
                velocity = Vector3.ClampMagnitude(velocity, Constants.MAX_ENTITY_SPEED);
                position += velocity * Constants.MICROTICK_DELTA_TIME;
                position.y -= Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME * Constants.MICROTICK_DELTA_TIME / 2D;
                velocity.y -= Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME;
                CollideLogic.collide_with_arena(Constants.BALL_RADIUS, 0, Constants.BALL_ARENA_E, ref position, ref velocity);
            }

            return new Point(position, velocity);
        }
        
        public static Point Move(Vector3 position, Vector3 velocity, double radius, double radius_change_speed, double arena_e, bool check_collide = true, int microtics = (int)Constants.MICROTICKS_PER_TICK)
        {
            var delta_time = 1 / (Constants.TICKS_PER_SECOND * Constants.MICROTICKS_PER_TICK);
            for (int j = 0; j < microtics; ++j)
            {
                velocity = Vector3.ClampMagnitude(velocity, Constants.MAX_ENTITY_SPEED);
                position += velocity * delta_time;
                position.y -= Constants.GRAVITY * delta_time * delta_time / 2D;
                velocity.y -= Constants.GRAVITY * delta_time;
                if(check_collide)
                    CollideLogic.collide_with_arena(radius, radius_change_speed, arena_e, ref position, ref velocity);
            }

            return new Point(position, velocity);
        }
       
        public static Point PredictBallPointOptimized(Point predict)
        {
            Vector3 resultPosition = predict.position;
            Vector3 resultVelocity = predict.velocity;

            const double pos_y_diff = Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME * Constants.MICROTICK_DELTA_TIME /
                                      2D;
            const double vel_y_diff = Constants.GRAVITY * Constants.MICROTICK_DELTA_TIME;


            double pos_x = predict.position.x;
            double pos_y = predict.position.y;
            double pos_z = predict.position.z;

            double vel_x = predict.velocity.x;
            
            double vel_z = predict.velocity.z;
            
            
            double vel_y_first = predict.velocity.y;
            double vel_y_last = predict.velocity.y - vel_y_diff * 99;    
            
            double vel_sum = (vel_y_first + vel_y_last) / 2 * 100;

            pos_y += vel_sum * Constants.MICROTICK_DELTA_TIME;
            pos_x += vel_x * Constants.MICROTICK_DELTA_TIME * 100;
            pos_z += vel_z * Constants.MICROTICK_DELTA_TIME * 100;
            pos_y -= pos_y_diff * 100;

            resultPosition = new Vector3(pos_x, pos_y, pos_z);
            resultVelocity = new Vector3(vel_x, vel_y_last - vel_y_diff, vel_z);
            
            var dan = CollideLogic.dan_to_arena(resultPosition);
            if (dan.distance < Constants.BALL_RADIUS)
                return PredictBallPoint(predict.position, predict.velocity);
            
            return new Point(resultPosition, resultVelocity);
        }

        public static Point PredictNextPointRoughly(
            Point predict,
            Vector3 targetVelocity)
        {
            var velocityChange = targetVelocity - predict.velocity;                   
            predict.velocity += Vector3.ClampMagnitude(
                velocityChange.normalized * Constants.ROBOT_ACCELERATION * Constants.TICK_DELTA_TIME, 
                velocityChange.magnitude);
            predict.position += predict.velocity * Constants.TICK_DELTA_TIME;
            predict.control = new Control(targetVelocity);
            return predict;
        }
        
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Point PredictNextPoint(Point predicted, Control control)
//        {
//            return PredictNextPoint(predicted.position, predicted.velocity, control.TargetVelocity);
//        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point PredictNextPoint(Point predicted, Control control)
        {
//            if(control.TargetVelocity.y != 0)
//                Console.WriteLine("TARGET ON Y");

//            
//            
//            var velocity = predicted.velocity;
//            var position = predicted.position;
//            var velocityChange = control.TargetVelocity - velocity;
//            var tickVelocityChange = velocityChange.normalized * Constants.ROBOT_ACCELERATION *
//                                     Constants.MICROTICK_DELTA_TIME;
//
//            double velocityChangeX = tickVelocityChange.x;
//            double velocityChangeZ = tickVelocityChange.z;
//            
//            for (int i = 0; i < 100; ++i)
//            {
//                velocity += Vector3.ClampMagnitude(
//                    velocityChange.normalized * Constants.ROBOT_ACCELERATION * Constants.MICROTICK_DELTA_TIME, 
//                    velocityChange.magnitude);
//                position += velocity * Constants.MICROTICK_DELTA_TIME;
//            }
//
//            return new Point(position, velocity, control);
//            
//
//            var velocity = predicted.velocity;
//            var position = predicted.position;
//            for (int i = 0; i < 100; ++i)
//            {
//                var velocityChange = control.TargetVelocity - velocity;                   
//                velocity += Vector3.ClampMagnitude(
//                    velocityChange.normalized * Constants.ROBOT_ACCELERATION * Constants.MICROTICK_DELTA_TIME, 
//                    velocityChange.magnitude);
//                position += velocity * Constants.MICROTICK_DELTA_TIME;
//            }
//
//            return new Point(position, velocity, control);
//            
//            
            double position_x = predicted.position.x;
            double position_z = predicted.position.z;
            double velocity_x = predicted.velocity.x;
            double velocity_z = predicted.velocity.z;

            int i = 0;
            
            double velocityChange_x;
            double velocityChange_z;

            double velocityChangeMagnitudeSqr;

            double velocityChangeMagnitude;
            
            double newVelocityChange_x;
            double newVelocityChange_z;

            double newVelocityChangeMagnitude;

            double newVelocityChangeMagnitudeSqr;

            for (i = 0; i < 100; i+=1)
            {
                velocityChange_x = control.TargetVelocity.x - velocity_x;
                velocityChange_z = control.TargetVelocity.z - velocity_z;

                velocityChangeMagnitudeSqr = velocityChange_x * velocityChange_x +
                                             velocityChange_z * velocityChange_z;

                // Если мы пропускаем 1e-12 то начинаются расхождения, лр где-то режет эту скорость
                if (velocityChangeMagnitudeSqr < 1e-10)
                {
                    position_x += velocity_x * Constants.MICROTICK_DELTA_TIME * (100 - i);
                    position_z += velocity_z * Constants.MICROTICK_DELTA_TIME * (100 - i);
                    break;
                }

                velocityChangeMagnitude = Math.Sqrt(velocityChangeMagnitudeSqr);
                
                if (velocityChangeMagnitude > 1E-80)
                {
                    newVelocityChange_x = velocityChange_x * Constants.ROBOT_ACCELERATION * Constants.MICROTICK_DELTA_TIME / velocityChangeMagnitude;
                    newVelocityChange_z = velocityChange_z * Constants.ROBOT_ACCELERATION * Constants.MICROTICK_DELTA_TIME / velocityChangeMagnitude;
                }
                else
                {
                    newVelocityChange_x = 0;
                    newVelocityChange_z = 0;
                }

                newVelocityChangeMagnitudeSqr = newVelocityChange_x * newVelocityChange_x +
                                                newVelocityChange_z * newVelocityChange_z; 

                if (newVelocityChangeMagnitudeSqr > velocityChangeMagnitudeSqr)
                {
                    newVelocityChangeMagnitude = Math.Sqrt(newVelocityChangeMagnitudeSqr);
                    if (newVelocityChangeMagnitude > 1E-80)
                    {
                        newVelocityChange_x = newVelocityChange_x / newVelocityChangeMagnitude * velocityChangeMagnitude;
                        newVelocityChange_z = newVelocityChange_z / newVelocityChangeMagnitude * velocityChangeMagnitude;
                    }
                }

                velocity_x += newVelocityChange_x;
                velocity_z += newVelocityChange_z;

                position_x += velocity_x * Constants.MICROTICK_DELTA_TIME;
                position_z += velocity_z * Constants.MICROTICK_DELTA_TIME;
            }
            
            return new Point(
                new Vector3(position_x, predicted.position.y, position_z), 
                new Vector3(velocity_x, predicted.velocity.y, velocity_z),
                control
            );
        }
        
        public struct ClampSpeedInfo
        {
            public double Speed;
            public int StartTick;
            public int StopTick;

            public ClampSpeedInfo(double speed, int startTick = 0, int stopTick = MaxTicksLoop)
            {
                Speed = speed;
                StartTick = startTick;
                StopTick = stopTick;
            }
        }

        public static List<Point> PredictPointsRoughlyGround(
            Point predicted,
            Vector3 targetPoint,
            int maxTicks = MaxTicksLoop
        )
        {
            predicted.position = predicted.position.DropY();
            targetPoint = targetPoint.DropY();
            return PredictPointsRoughly(predicted, targetPoint, maxTicks);
        }
        
        public static List<Point> PredictPointsRoughly(
            Point predicted, 
            Vector3 targetPoint, 
            int maxTicks = MaxTicksLoop
        )
        {            
            List<Point> result = new List<Point>(){predicted};
            
            bool calcPerpend = false;
            for (int i = 0; i < maxTicks; ++i)
            {
                Vector3 targetVelocity = MoveCalculator.TargetToVelocity(predicted.position, targetPoint);
                
                predicted = PredictNextPointRoughly(predicted, targetVelocity);

                if (!calcPerpend && predicted.position.DistanceTo(targetPoint) < Constants.ROBOT_MAX_GROUND_SPEED_PER_TICK)
                    calcPerpend = true;

                result.Add(predicted);
                
                if (!calcPerpend)
                {
                    continue;
                }
                
                if(targetPoint.IsPointBetween(result[result.Count - 1].position, result[result.Count - 2].position))                
                    break;
            }

            return result;
        }

        private static int _lastTickPredictPointsCalled = 0;
        private static int _predictPointsCount = 0;
        
        public static List<Point> PredictPoints(
            Point predicted, 
            Vector3 targetPoint, 
            int maxTicks = MaxTicksLoop,
            ClampSpeedInfo? clamp1 = null,
            ClampSpeedInfo? clamp2 = null
        )
        {            
#if DEBUG
            if (MyGame.CurrentTick != _lastTickPredictPointsCalled)
            {
                Console.WriteLine("TICK " + _lastTickPredictPointsCalled + " TOTAL CALLED " + _predictPointsCount);
                _predictPointsCount = 0;
                _lastTickPredictPointsCalled = MyGame.CurrentTick;
            }
            _predictPointsCount++;
#endif

            List<Point> result = new List<Point>(maxTicks+1){predicted};
            
            bool calcPerpend = false;
            for (int i = 0; i < maxTicks; ++i)
            {
                Vector3 targetVelocity = MoveCalculator.TargetToVelocity(predicted.position, targetPoint);
                if(clamp1.HasValue && i >= clamp1.Value.StartTick && i < clamp1.Value.StopTick)
                    targetVelocity = Vector3.ClampMagnitude(targetVelocity, clamp1.Value.Speed);
                if(clamp2.HasValue && i >= clamp2.Value.StartTick && i < clamp2.Value.StopTick)
                    targetVelocity = Vector3.ClampMagnitude(targetVelocity, clamp2.Value.Speed);
                
                predicted = PredictNextPoint(predicted, new Control(targetVelocity));

                if (!calcPerpend && predicted.position.DistanceTo(targetPoint) < Constants.ROBOT_MAX_GROUND_SPEED_PER_TICK)
                    calcPerpend = true;

                result.Add(predicted);
                
                if (!calcPerpend)
                {
                    continue;
                }
                
               
                if(targetPoint.IsPointBetween(result[result.Count - 1].position, result[result.Count - 2].position))                
                    break;
            }

            return result;
        }


        static List<Point> FlatThisListWithJump(List<Point> list, int fastestJumpTicks, double nitroAmount, Control flyingControl)
        {            
            List<Point> result = new List<Point>();
            
            var tmp_robot = new MyRobot();
            tmp_robot.touch = true;
            tmp_robot.touchNormal = Vector3.up;
            tmp_robot.nitroAmount = nitroAmount;
            tmp_robot.radius = MoveCalculator.RadiusFromJumpSpeed(flyingControl.JumpSpeed);
            tmp_robot.radiusChangeSpeed = flyingControl.JumpSpeed;
            
            for (int i = 0; i < list.Count; ++i)
            {
                var t = list[i];
                
                if (i < list.Count - fastestJumpTicks || i == 0)
                {
                    result.Add(t);
                    continue;
                }
                
                var predictedPosition = result[i - 1].position;
                var predictedVelocity = result[i - 1].velocity;

                tmp_robot.position = predictedPosition;
                tmp_robot.velocity = predictedVelocity;
                CollideLogic.update_for_jump(tmp_robot, flyingControl);
                t.position = tmp_robot.position;
                t.velocity = tmp_robot.velocity;
                t.control = flyingControl;

                result.Add(t);
            }

            return result;
        }


        static double CalcSlowdownSpeed(double curSpeed, int tickCount)
        {
            return curSpeed - Constants.ROBOT_ACCELERATION * (1.0 / Constants.TICKS_PER_SECOND) * tickCount;
        }
        
        static double CalcBreakdownDistance(double curSpeed, int ticks)
        {   
            double startSpeed = curSpeed - Constants.ROBOT_ACCELERATION * (1.0 / (Constants.TICKS_PER_SECOND * Constants.MICROTICKS_PER_TICK));
            double endSpeed = curSpeed - Constants.ROBOT_ACCELERATION * (1.0 / (Constants.TICKS_PER_SECOND)) * ticks;

            return (startSpeed + endSpeed) / 2 * 100 * ticks * Constants.MICROTICK_DELTA_TIME;
//
//            double distance = 0;
//            for (int k = 0; k < ticks; ++k)
//            {
//                for (int i = 0; i < 100; ++i)
//                {
//                    curSpeed -= Constants.ROBOT_ACCELERATION * (1.0 / (Constants.TICKS_PER_SECOND * Constants.MICROTICKS_PER_TICK));
//                    distance += curSpeed * Constants.MICROTICK_DELTA_TIME;
//                }
//            }
//
//            return distance;
        }

        static public int FindStabilizedTick(List<Point> path, double eps)
        {
            // может вернуть на прошлую версию?
            var lastNotmTargetVelocity = path.Last().velocity.normalized;
            for (int i = 0; i < path.Count - 1; ++i)
            {
                if (path[i].velocity.normalized
                    .AlmostEqual(lastNotmTargetVelocity, eps))
                {
                    return i;
                }
            }
//            for (int i = 2; i < path.Count; ++i)
//            {
//                for (int j = i + 1; j < path.Count; ++j)
//                {
//                    if (path[i].velocity.normalized
//                        .AlmostEqual(path[j].velocity.normalized, eps))
//                    {
//                        return i;
//                    }
//                }
//            }

            return MaxTicksLoop;
        }

        private static int _predictPointsSmartCount = 0;
        private static int _predictPointsSmartLastTick = 0;
        
        public static PredictPointsResult PredictPointsSmart(
            Point predicted,
            Vector3 targetPoint,
            int forTicks,
            int ticksOnGround,
            double maxSpeed,
            double minMiddleSpeed)
        {
#if DEBUG
            if (MyGame.CurrentTick != _predictPointsSmartLastTick)
            {
                Console.WriteLine("TICK " + _predictPointsSmartLastTick + " SMART PREDICT CALLED " + _predictPointsSmartCount);
                _predictPointsSmartCount = 0;
                _predictPointsSmartLastTick = MyGame.CurrentTick;
            }
            _predictPointsSmartCount++;
#endif

            targetPoint = targetPoint.DropY(Constants.ROBOT_RADIUS);

            if (forTicks >= MaxTicksLoop)
            {
                return new PredictPointsResult(PredictError.NotSoSlow);
            }
            
            const double eps = 1e-11; 
            var fastestPath = PredictPoints(
                predicted,
                targetPoint,
                forTicks + 1,
                new ClampSpeedInfo(maxSpeed));

            if (!fastestPath[fastestPath.Count - 1].velocity.magnitude.AlmostEqualTo(maxSpeed)
                || !fastestPath[fastestPath.Count - 2].velocity.magnitude.AlmostEqualTo(maxSpeed))
            {
                return new PredictPointsResult(PredictError.NotStabilized);
            }

            if (fastestPath.Count > forTicks)
            {
                return new PredictPointsResult(PredictError.NotSoFast);
            }

            var stabilizedTick = FindStabilizedTick(fastestPath, eps);
            if (ticksOnGround < stabilizedTick)
            {
                return new PredictPointsResult(PredictError.NotStabilized);
            }

//            var flattedTick = FindFlattedTick(fastestPath, eps);
//            var minSlowdownTick = Math.Max(flattedTick, stabilizedTick);

            if (stabilizedTick > ticksOnGround)
            {
                return new PredictPointsResult(PredictError.TooFewTicksToJump);
            }

            if (fastestPath.Count == forTicks)
            {
                return new PredictPointsResult(PredictError.NoError, fastestPath);
            }

            var fullSpeed = maxSpeed;

            for (int breakdownStep = 1;; ++breakdownStep)
            {
                var slowdownSpeed = CalcSlowdownSpeed(fullSpeed, breakdownStep);
                
                if (slowdownSpeed < minMiddleSpeed)
                {
                    return new PredictPointsResult(PredictError.NotSoSlow);
                }
                
                int breakdownTick = stabilizedTick;
                while (slowdownSpeed > fastestPath[breakdownTick].velocity.magnitude)
                    ++breakdownTick;
                
                double rightBreakdownDistance = CalcBreakdownDistance(fullSpeed, breakdownStep);
                double leftBreakdownDistance = 0;
                int rightBreakdownStep = breakdownStep;
                int leftBreakdownStep = 0;
                double breakdownSpeed = fastestPath[breakdownTick].velocity.magnitude;
                while (breakdownSpeed > slowdownSpeed)
                {
                    leftBreakdownStep++;
                    leftBreakdownDistance += CalcBreakdownDistance(breakdownSpeed, 1);
                    breakdownSpeed = CalcSlowdownSpeed(breakdownSpeed, 1);
                }
                    
                var breakdownTicks = leftBreakdownStep + rightBreakdownStep;
                var breakdownDistance = leftBreakdownDistance + rightBreakdownDistance;


                for (int slowdownTicks = 0;; ++slowdownTicks)
                {
                    double slowdownDistance = slowdownTicks * (slowdownSpeed / Constants.TICKS_PER_SECOND);
                    double totalDistance = breakdownDistance + slowdownDistance;
                    int fullPathTicks = breakdownTick + breakdownTicks + slowdownTicks;
                    
                    if(fullPathTicks >= ticksOnGround)
                        break;

                    var startpoint = fastestPath[breakdownTick];
                    var velNorm = fastestPath[breakdownTick].velocity.normalized;
                    
                    var endPos = startpoint.position + velNorm * totalDistance;
                    if (targetPoint.IsPointBetween(startpoint.position, endPos))
                        break;

                    fullPathTicks += (int) Math.Ceiling(endPos.DistanceTo(targetPoint) / (fullSpeed / Constants.TICKS_PER_SECOND));

                    if (fullPathTicks < forTicks)
                    {
                        continue;
                    }

                    if (fullPathTicks == forTicks)
                    {
                        var result = PredictPoints(
                            predicted,
                            targetPoint,
                            MaxTicksLoop,
                            new ClampSpeedInfo(
                                slowdownSpeed,
                                breakdownTick,
                                breakdownTick + leftBreakdownStep + slowdownTicks - 1),
                            new ClampSpeedInfo(maxSpeed));
                   
                        if(result.Count != fullPathTicks)
                            Console.WriteLine("DIFF PREDICT TICK " + MyGame.CurrentTick);
                        
                        return new PredictPointsResult(PredictError.NoError, result);
                    }
                    

                    var newClampedSpeed = slowdownSpeed + (CalcSlowdownSpeed(fullSpeed, breakdownStep - 1) - slowdownSpeed) / 2.0;
                    if (newClampedSpeed.AlmostEqualTo(slowdownSpeed, 1e-9))
                    {
                        Console.WriteLine("DOUBLE SPLIT FOUND AT "  + MyGame.CurrentTick + " GOING DOWN");
                        break;
                    }
                    else
                    {
                        slowdownSpeed = newClampedSpeed;
                    }
                    slowdownTicks = 0;
                }
            }
        }

        private static int makeSimplifiedPredictLastTick = 0;
        private static int makeSimplifiedPredictCount = 0;

        public static PredictPointsResult PredictPointsSimplifiedWithJump(
            Point predicted,
            Vector3 target,
            int forTicks,
            double nitroAmount,
            Control flyingControl)
        {
            var jumpPredict = MakeJumpPredict(flyingControl, nitroAmount);
            
            int fastestJumpTicks = FindJumpTick(jumpPredict.heights, target.y);
            if (fastestJumpTicks >= forTicks)
            {
                return new PredictPointsResult(PredictError.TooFewTicksToJump);
            }

            var points = PredictPointsSimplified(predicted, target, forTicks);
            if (points.Predicted == null || points.Error != PredictError.NoError)
                return points;

            points.Predicted = FlatThisListWithJump(points.Predicted, fastestJumpTicks, nitroAmount, flyingControl);

            return points;
        }
        public const double MinMiddleSpeedOnFullSpeed = 15;
        
        public static PredictPointsResult PredictPointsSmartWithJump(
            Point predicted,
            Vector3 target,
            int forTicks,
            double nitroAmount,
            Control flyingControl)
        {
            var jumpPredict = MakeJumpPredict(flyingControl, nitroAmount);
            
            int fastestJumpTicks = FindJumpTick(jumpPredict.heights, target.y);
            if (fastestJumpTicks >= forTicks)
            {
                return new PredictPointsResult(PredictError.TooFewTicksToJump);
            }

            var points = PredictPointsSmart(
                predicted, 
                target, 
                forTicks, 
                forTicks - fastestJumpTicks, 
                Constants.ROBOT_MAX_GROUND_SPEED, 
                MinMiddleSpeedOnFullSpeed);
            if (points.Predicted == null || points.Error != PredictError.NoError)
                return points;

            points.Predicted = FlatThisListWithJump(points.Predicted, fastestJumpTicks, nitroAmount, flyingControl);

            return points;
        }
        
        public static PredictPointsResult PredictPointsSimplified(
            Point predicted,
            Vector3 targetPoint,
            int forTicks)
        {
            
#if DEBUG
            if (MyGame.CurrentTick != makeSimplifiedPredictLastTick)
            {
                Console.WriteLine("TICK " + makeSimplifiedPredictLastTick + " SIMPLIFIED PREDICT CALLED " + makeSimplifiedPredictCount);
                makeSimplifiedPredictCount = 0;
                makeSimplifiedPredictLastTick = MyGame.CurrentTick;
            }
            makeSimplifiedPredictCount++;
#endif
            
            double eps = 0.05;

            var targetOnGround = targetPoint.DropY(Constants.ROBOT_RADIUS); 
            
            var fastestPath = PredictPoints(
                predicted,
                targetOnGround,
                forTicks + 1);

            if (fastestPath.Count == forTicks)
            {
                return new PredictPointsResult(PredictError.NoError, fastestPath);
            }

            if (fastestPath.Count > MaxTicksLoop)
            {
                return new PredictPointsResult(PredictError.LoopTimeout);
            }

            if (fastestPath.Count > forTicks)
            {
                return new PredictPointsResult(PredictError.NotSoFast);
            }

            double fastestSpeed = Constants.ROBOT_MAX_GROUND_SPEED;
            double slowestSpeed = fastestSpeed / 2;
            var slowestPath = PredictPoints(predicted, targetOnGround, forTicks + 1, new ClampSpeedInfo(slowestSpeed));
            if (slowestPath.Count == forTicks)
            {
                return new PredictPointsResult(PredictError.NoError, slowestPath);
            }

            while (slowestPath.Count < forTicks)
            {
                fastestSpeed = slowestSpeed;
                slowestSpeed /= 2;
                if (slowestSpeed < 0.5)
                {
                    return new PredictPointsResult(PredictError.NotSoSlow);
                }
                slowestPath = PredictPoints(predicted, targetOnGround, forTicks + 1, new ClampSpeedInfo(slowestSpeed));
            }
            
            if (slowestPath.Count == forTicks)
            {
                return new PredictPointsResult(PredictError.NoError, slowestPath);
            }
            for (;;)
            {
                if (Math.Abs(fastestSpeed - slowestSpeed) < eps)
                {
                    return new PredictPointsResult(PredictError.Unknown);
                }

                double speedDiff = fastestSpeed - slowestSpeed;
//                double tickDiff = slowestPath.Count - fastestPath.Count;
//                double proportion = (slowestPath.Count - forTicks) / tickDiff;
//                double fixedProportion = proportion > 0.5 ? 0.75 : 0.25;
                double fixedProportion = 0.5;
//                double fixedProportion = proportion;
                
//                double centerProportion = Math.Abs(0.5 - proportion);
//                double edgeProportion = 0.5 - centerProportion;
//                double doubleProportion = edgeProportion + edgeProportion * (centerProportion * 2);
//                if (proportion > 0.5)
//                    doubleProportion = 1 - doubleProportion;
                var speed = slowestSpeed + speedDiff * fixedProportion;
                
                var path = PredictPoints(predicted, targetOnGround, forTicks, new ClampSpeedInfo(speed));
                if (path.Count == forTicks)
                {
                    return new PredictPointsResult(PredictError.NoError, path);
                }

                if (path.Count > forTicks)
                {
                    slowestSpeed = speed;
                }
                else
                {
                    fastestSpeed = speed;
                }
            }
        }
    }
}