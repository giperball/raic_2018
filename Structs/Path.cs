using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Common;
using ConsoleApp1.DebugHelpers;
using ConsoleApp1.Logic;

namespace ConsoleApp1.SubStrategy
{
    public enum PathType
    {
        Unknown = 0,
        BallHit,
        RecatchBall,
        InterceptHard,
        RunFromTeammate,
        PathNotFound,
    }
    public struct Path
    {
        public BallShootPoint ShootPoint;
        public int EnemyHitTick;
        public List<Point> BallBeforeHit;
        public List<Point> PredictResults;
        public List<Point>  BallAfterHit;
        public PathType Type;
//    
//        public Path(List<PredictResult> predictResults)
//        {
//            PredictResults = predictResults;
//            BallAfterHit = new List<PredictResult>();
//            BallBeforeHit = new List<PredictResult>();
//            EnemyHitTick = 0;
//            ShootPoint = new BallShootPoint();
//        }

        public List<IDebugHelper> ToDebugHelpers(Color? shootPointColor, Color? robotMoveColor, Color? ballMoveColor, Color? ballAfterHitColor, double width = 2, int step = 4)
        {
            List<IDebugHelper> result = new List<IDebugHelper>();

            if(shootPointColor.HasValue)
                result.Add(new SphereShower(ShootPoint.GlobalPoint, shootPointColor.Value, 0.25));

            if (PredictResults != null && robotMoveColor.HasValue)
            {
                result.AddRange(LineShower.ToLineDebugHelpers(
                    PredictResults.Where((x, i) => i % step == 0).Select(r => r.position),
                    robotMoveColor.Value,
                    width));
            }
            
            if (BallBeforeHit != null && ballMoveColor.HasValue)
            {
                result.AddRange(LineShower.ToLineDebugHelpers(
                    BallBeforeHit.Where((x, i) => i % step * 2 == 0).Select(r => r.position),
                    ballMoveColor.Value,
                    width));
            }

            if (BallAfterHit != null && ballAfterHitColor.HasValue)
            {
                result.AddRange(LineShower.ToLineDebugHelpers(
                    BallAfterHit.Where((x, i) => i % step == 0).Select(r => r.position),
                    ballAfterHitColor.Value,
                    width));
            }

            return result;
        }
    }
}