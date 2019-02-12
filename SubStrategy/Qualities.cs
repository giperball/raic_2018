using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.Logic;
using ConsoleApp1.SubStrategy;
using Newtonsoft.Json.Linq;


public interface IQualityRating
{
    double Calc(Path path);
}


class HasGoalQuality: IQualityRating
{
    public const int MaxTicksForGoalFound = 150;
    
    public double Calc(Path path)
    {
        if (path.BallAfterHit.Take(MaxTicksForGoalFound).Any(p => MoveCalculator.IsBallInEnemyGates(p.position)))
            return 1;
        return 0;
    }
}

class LengthQuality: IQualityRating
{
    public double Calc(Path path)
    {
        return -path.PredictResults.Count; 
    }
}


class LongestPathWithoutEnemyHitWithGoal : IQualityRating
{
    List<MyRobot> _enemies;
    const int maxTicks = 100;

    public LongestPathWithoutEnemyHitWithGoal(List<MyRobot> enemies)
    {
        _enemies = enemies;
    }

    public double Calc(Path path)
    {
        for (int i = 0; i < Math.Min(maxTicks, path.BallAfterHit.Count); ++i)
        {
            if (MoveCalculator.IsBallInEnemyGates(path.BallAfterHit[i].position))
                return maxTicks + i;
            
            if(path.BallAfterHit[i].position.y > Constants.ROBOT_MAX_JUMP_HEIGHT + Constants.ROBOT_MAX_RADIUS + Constants.BALL_RADIUS)
                continue;
            foreach (var enemy in _enemies)
            {
                int forTick = i + path.PredictResults.Count;
                if (MovePredictor.PredictPointsRoughlyGround(
                        new Point(enemy),
                        path.BallAfterHit[i].position,
                        forTick).Count < forTick)
                {
                    return i;
                }
            }
        }

        return maxTicks;
    }
}

class LongestZOnEnemyHit : IQualityRating
{
    List<MyRobot> _enemies;
    const int maxTicks = 200;

    public LongestZOnEnemyHit(List<MyRobot> enemies)
    {
        _enemies = enemies;
    }

    public double Calc(Path path)
    {
        for (int i = 0; i < Math.Min(maxTicks, path.BallAfterHit.Count); ++i)
        {
            if(path.BallAfterHit[i].position.y > Constants.ROBOT_MAX_JUMP_HEIGHT + Constants.ROBOT_MAX_RADIUS + Constants.BALL_RADIUS)
                continue;
            foreach (var enemy in _enemies)
            {
                int forTick = i + path.PredictResults.Count;
                if (MovePredictor.PredictPointsRoughlyGround(
                        new Point(enemy),
                        path.BallAfterHit[i].position,
                        forTick).Count < forTick)
                {
                    return path.BallAfterHit[i].position.z;
                }
            }
        }

        return 100500;
    }
}