using System.Collections.Generic;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Common;
using ConsoleApp1.SubStrategy;

namespace ConsoleApp1.Logic
{
    public enum BallHitZoneError
    {
        NoError,
        SomeError,
        NotSoSlowError,
        NotSoFastError
    }
    
    public struct CalcResult
    {
        public BallShootPoint ShootPoint;
        public PredictError PathError;
        public Path Path;

        public CalcResult(BallShootPoint shootPoint, PredictError pathError, Path path)
        {
            ShootPoint = shootPoint;
            PathError = pathError;
            Path = path;
        }
    }
    
    public struct AlgorithmSearchSettings
    {
        public int MaxCheckpoints;
        public int CheckpointTicksCount;
        public int ShootPointsRecheck;

        public AlgorithmSearchSettings(int maxCheckpoints, int checkpointTicksCount, int shootPointsRecheck)
        {
            MaxCheckpoints = maxCheckpoints;
            CheckpointTicksCount = checkpointTicksCount;
            ShootPointsRecheck = shootPointsRecheck;
        }
    }
    
    public interface IAlgorithm
    {
        BallHitZoneError BallInHitZone(ref List<BallShootPoint> shootPoints, Point predicted, double nitroAmount,
            int forTick);
        
        List<BallShootPoint> CalcBallShootPoints(Vector3 ballPosition);

        AlgorithmSearchSettings Settings();

        PredictPointsResult MakePointsWithJump(Point predicted,
            Vector3 targetPoint,
            int forTicks,
            double nitroAmount);
    }
}