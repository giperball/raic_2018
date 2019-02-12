using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.Common;
using ConsoleApp1.SubStrategy;

namespace ConsoleApp1.Logic
{    
    public static class MoveCalculator
    {       
        public static Vector3 TargetToVelocityOnGround(Vector3 currentPoint, Vector3 target,
            double speed = Constants.ROBOT_MAX_GROUND_SPEED)
        {
            return TargetToVelocity(currentPoint.DropY(Constants.ROBOT_RADIUS), target.DropY(Constants.ROBOT_RADIUS),
                speed);
        }
        
        public static Vector3 TargetToVelocity(Vector3 currentPoint, Vector3 target,
            double speed = Constants.ROBOT_MAX_GROUND_SPEED)
        {
            return (target - currentPoint).normalized * speed;
        }


        public static bool RobotPhysicallyCanRunToBall(Vector3 robotPoint, Vector3 ballPoint, int forTick)
        {
            return (ballPoint.DropY().DistanceTo(robotPoint.DropY()) - Constants.ROBOT_MAX_RADIUS - Constants.BALL_RADIUS) / forTick
                < Constants.ROBOT_MAX_GROUND_SPEED_PER_TICK;
        }
        
        public static Vector3 GoalPoint = new Vector3(0, MyArena.goal_height / 2, (MyArena.depth + MyArena.goal_depth) / 2);
        public static Vector3 DefendPoint = new Vector3(0, MyArena.goal_height / 2, -(MyArena.depth + MyArena.goal_depth) / 2);


        public static bool ValidateOnArenaInner(List<Point> predicted)
        {
            foreach (var predictResult in predicted)
            {
                if (Math.Abs(predictResult.position.x) > MyArena.width / 2 - MyArena.bottom_radius
                    || Math.Abs(predictResult.position.z) > MyArena.depth / 2 - MyArena.bottom_radius
                    || (Math.Abs(predictResult.position.x) > MyArena.width / 2 - MyArena.corner_radius
                        && Math.Abs(predictResult.position.z) > MyArena.depth / 2 - MyArena.corner_radius))
                {
                    if (CollideLogic.dan_to_arena(predictResult.position).distance < Constants.ROBOT_RADIUS)
                        return false;
                }
            }

            return true;
        }
        
        public static List<BallShootPoint> CalcAllBallShootPoints(Vector3 ballPosition, int totalCircleCount, int totalVerticalCount, double radius)
        {
            List<BallShootPoint> result = new List<BallShootPoint>(totalCircleCount * totalVerticalCount + 1);
            
//            List<double> parts = new List<double>(){0, 0.33, 0.75, 0.8};

            for (int i = 0; i < totalVerticalCount; ++i)
            {
                var tetta = Math.PI / 2 + Math.PI / 2  / (totalVerticalCount) * i;
                
                for (int j = 0; j < totalCircleCount; ++j)
                {
                    var phi = (Math.PI * 2) / totalCircleCount * j;
                    result.Add(new BallShootPoint(
                        i * totalCircleCount + j,
                        ballPosition,
                        new Polar3(radius, tetta, phi)
                    ));
                }
            }
            
            result.Add(new BallShootPoint(totalCircleCount * totalVerticalCount + 1, ballPosition, new Polar3(radius, Math.PI, 0)));

            return result;
        }

        public static List<BallShootPoint> FilterBallShootPoints(List<BallShootPoint> allPoints, bool filterOnHeight)
        {
            return allPoints.Where(p => 
                (!filterOnHeight || p.GlobalPoint.y < Constants.ROBOT_MAX_JUMP_HEIGHT) 
                && p.GlobalPoint.y > Constants.ROBOT_RADIUS
//                && CollideLogic.dan_to_arena(p.GlobalPoint).distance > Constants.ROBOT_RADIUS * 1.5
                && Math.Abs(p.GlobalPoint.x) < MyArena.width / 2 - Constants.ROBOT_RADIUS * 1.5
                ).ToList();
        }
               
        public static bool IsBallInEnemyGates(Vector3 point)
        {
            return point.z > MyArena.depth / 2.0 + Constants.BALL_RADIUS;
        }
        public static bool IsBallInMyGates(Vector3 point)
        {
            return point.z < -(MyArena.depth / 2.0 + Constants.BALL_RADIUS);
        }
        
        public static bool LineSegmentsIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
//            intersection = Vector3.zero;

            var d = (p2.x - p1.x) * (p4.z - p3.z) - (p2.z - p1.z) * (p4.x - p3.x);

            if (d == 0.0f)
            {
                return false;
            }

            var u = ((p3.x - p1.x) * (p4.z - p3.z) - (p3.z - p1.z) * (p4.x - p3.x)) / d;
            var v = ((p3.x - p1.x) * (p2.z - p1.z) - (p3.z - p1.z) * (p2.x - p1.x)) / d;

            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
            {
                return false;
            }
//
//            intersection.x = p1.x + u * (p2.x - p1.x);
//            intersection.z = p1.z + u * (p2.z - p1.z);

            return true;
        }

        public static double RadiusFromJumpSpeed(double jumpSpeed)
        {
            return Constants.ROBOT_MIN_RADIUS + (Constants.ROBOT_MAX_RADIUS - Constants.ROBOT_MIN_RADIUS)
                * jumpSpeed / Constants.ROBOT_MAX_JUMP_SPEED;
        }
        
        public static double JumpSpeedFromRadius(double radius)
        {
            //! проверить
            return (Constants.ROBOT_MAX_JUMP_SPEED * (radius - Constants.ROBOT_MIN_RADIUS)) /
                   (Constants.ROBOT_MAX_RADIUS - Constants.ROBOT_MIN_RADIUS);
        }

    }
}