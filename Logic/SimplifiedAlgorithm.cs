using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.Common;
using ConsoleApp1.Structs;

namespace ConsoleApp1.Logic
{
    public class SimplifiedAlgorithm : IAlgorithm
    {   
        private bool _useNitro;
        private readonly int _playersCount;
        private Control _flyingControl;

        public SimplifiedAlgorithm(bool useNitro, int playersCount)
        {
            _useNitro = useNitro;
            _playersCount = playersCount;
            _flyingControl = new Control(Vector3.up * Constants.MAX_ENTITY_SPEED, Constants.ROBOT_MAX_JUMP_SPEED,
                _useNitro);
        }

        public BallHitZoneError BallInHitZone(ref List<BallShootPoint> shootPoints, Point predicted, double nitroAmount, int forTick)
        {
//            return BallHitZoneError.SomeError;
            
            var ballPoint = shootPoints.First().BallPoint;
            
            if (!MoveCalculator.RobotPhysicallyCanRunToBall(predicted.position, ballPoint, forTick))
                return BallHitZoneError.NotSoFastError;

            var lowestPoint = shootPoints.MinBy(p => p.GlobalPoint.y);

            var jumpPredict = MovePredictor.MakeJumpPredict(_flyingControl, nitroAmount);
            int jumpTick = MovePredictor.FindJumpTick(jumpPredict.heights, lowestPoint.GlobalPoint.y);
            if (jumpTick > forTick)
                return BallHitZoneError.NotSoFastError;
            if (jumpPredict.heights.Last() + jumpPredict.heights[jumpPredict.heights.Count - 2] -
                jumpPredict.heights.Last() < lowestPoint.GlobalPoint.y)
                return BallHitZoneError.NotSoFastError;

            var fastestPathToCenterOfBall = MovePredictor.PredictPoints(
                new Point(predicted.position.DropY(),predicted.velocity),
                ballPoint.DropY());
            var eps = 1e-11;

            if (MovePredictor.FindStabilizedTick(fastestPathToCenterOfBall, eps) > forTick)
                return BallHitZoneError.NoError;

            if (fastestPathToCenterOfBall.Count - 20 > forTick)
                return BallHitZoneError.NotSoFastError;
            return BallHitZoneError.NoError;
        }

        public AlgorithmSearchSettings Settings()
        {
            return new AlgorithmSearchSettings(_playersCount == 2 ? 10 : 8, 1, 1);
        }

        public PredictPointsResult MakePointsWithJump(Point predicted, Vector3 targetPoint, int forTicks, double nitroAmount)
        {
            return MovePredictor.PredictPointsSimplifiedWithJump(
                predicted,
                targetPoint,
                forTicks,
                nitroAmount,
                _flyingControl);
        }

        public List<BallShootPoint> CalcBallShootPoints(Vector3 ballPosition)
        {
//            return new List<BallShootPoint>()
//            {
//                new BallShootPoint(0, ballPosition, new Polar3(0, 3, 3))
//            };
            return MoveCalculator.FilterBallShootPoints(
                MoveCalculator.CalcAllBallShootPoints(ballPosition, _playersCount == 2 ? 16 : 12, _playersCount == 2 ? 4 : 3, Constants.ROBOT_MAX_RADIUS + Constants.BALL_RADIUS * 0.8),
                !_useNitro);
        }
        
        
    }
}