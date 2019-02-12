using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Common;
using ConsoleApp1.Structs;
using ConsoleApp1.SubStrategy;

namespace ConsoleApp1.Logic
{
    public class SmartAlgorithm : IAlgorithm
    {
        private bool _useNitro;
        private readonly int _playersCount;

        public SmartAlgorithm(bool useNitro, int playersCount)
        {
            _useNitro = useNitro;
            _playersCount = playersCount;
        }

        public BallHitZoneError BallInHitZone(ref List<BallShootPoint> shootPoints, Point predicted, double nitroAmount, int forTick)
        {
//            return BallHitZoneError.NoError;
//            return BallHitZoneError.SomeError;
            
            var ballPoint = shootPoints.First().BallPoint;

            if (!MoveCalculator.RobotPhysicallyCanRunToBall(predicted.position, ballPoint, forTick))
                return BallHitZoneError.NotSoFastError;

            var fastestPathToCenterOfBall = MovePredictor.PredictPoints(
                new Point(
                    predicted.position.DropY(),
                    predicted.velocity),
                ballPoint.DropY());

            var maxRadius = shootPoints.Max(p => p.GlobalPoint.DistanceTo(p.BallPoint));
            var maxY = shootPoints.Max(p => p.GlobalPoint.y);

            Vector3 speedVector = fastestPathToCenterOfBall.Last().velocity;
            var endpoint1 = ballPoint + speedVector.DropY().normalized * maxRadius;
            var endpoint2 = ballPoint - speedVector.DropY().normalized * maxRadius;
            endpoint1.y = maxY;
            endpoint2.y = maxY;

            PredictPointsResult p1 = this.MakePointsWithJump(
                predicted,
                endpoint1,
                forTick,
                nitroAmount
            );
            PredictPointsResult p2 = this.MakePointsWithJump(
                predicted,
                endpoint2,
                forTick,
                nitroAmount
            );
            
            if (p1.Error == PredictError.NoError 
                || p2.Error == PredictError.NoError
                || (p1.Error == PredictError.NotSoFast && p2.Error == PredictError.NotSoSlow)
                || (p1.Error == PredictError.NotSoSlow && p2.Error == PredictError.NotSoFast))
            {
                var stabilizedPointIndex = MovePredictor.FindStabilizedTick(fastestPathToCenterOfBall, 0.001);
                if (stabilizedPointIndex < fastestPathToCenterOfBall.Count)
                {
                    shootPoints = shootPoints
                        .OrderBy(p => p.GlobalPoint.DropY().DistanceTo(fastestPathToCenterOfBall[stabilizedPointIndex].position.DropY()))
                        .ToList();
                }
                return BallHitZoneError.NoError;
            }

            if (p1.Error == PredictError.NotSoFast && p2.Error == PredictError.NotSoFast)
                return BallHitZoneError.NotSoFastError;
            
            if (p1.Error == PredictError.NotSoSlow && p2.Error == PredictError.NotSoSlow)
                return BallHitZoneError.NotSoSlowError;
            
            return BallHitZoneError.SomeError;
        }

        public AlgorithmSearchSettings Settings()
        {
            return new AlgorithmSearchSettings(_playersCount == 2 ? 10 : 8, 1, 1);
        }

        public PredictPointsResult MakePointsWithJump(Point predicted, Vector3 targetPoint, int forTicks, double nitroAmount)
        {
            return MovePredictor.PredictPointsSmartWithJump(
                predicted,
                targetPoint,
                forTicks,
                nitroAmount,
                new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, Constants.ROBOT_MAX_JUMP_SPEED, _useNitro));
        }

        public List<BallShootPoint> CalcBallShootPoints(Vector3 ballPosition)
        {
//            return new List<BallShootPoint>()
//            {
//                new BallShootPoint(0, ballPosition, new Polar3(2.5, 2, 2))
//            };
            return MoveCalculator.FilterBallShootPoints(
                MoveCalculator.CalcAllBallShootPoints(
                    ballPosition, _playersCount == 2 ? 16 : 12, _playersCount == 2 ? 4 : 3, Constants.ROBOT_MAX_RADIUS + Constants.BALL_RADIUS - 0.5),
                !_useNitro);
        }
    }
}