using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.DebugHelpers;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    public class AttackerExtension: IBaseSubstrategyExtension
    {
        public int TicksToFuture => 150;
        
        public List<Tuple<double, IQualityRating>> GetQualities(List<Path> paths, ModelUpdater updater)
        {
            if (updater.myGame.IsNewRound)
            {
                return new List<Tuple<double, IQualityRating>>()
                {
                    new Tuple<double, IQualityRating>(1, new LengthQuality()),
                }; 
            }
            else
            {
                if (paths.Any(p => p.BallAfterHit != null && 
                                   p.BallAfterHit.Take(HasGoalQuality.MaxTicksForGoalFound).
                                       Any(b => MoveCalculator.IsBallInEnemyGates(b.position))))
                {
                    return new List<Tuple<double, IQualityRating>>()
                    {
                        new Tuple<double, IQualityRating>(10000, new HasGoalQuality()),
                        new Tuple<double, IQualityRating>(1000, new LongestPathWithoutEnemyHitWithGoal(updater.Enemies)),
                    };
                }
                else
                {
                    return new List<Tuple<double, IQualityRating>>()
                    {
    //                    new Tuple<double, IQualityRating>(1000, new GoalPointQuality()),
    //                    new Tuple<double, IQualityRating>(8, new JumpQuality()),
    //                    new Tuple<double, IQualityRating>(1000, new HasGoalQuality()),
//                        new Tuple<double, IQualityRating>(3, new FarrestOurGatesQuality()),
                        new Tuple<double, IQualityRating>(3, new LongestZOnEnemyHit(updater.Enemies)),
//                        new Tuple<double, IQualityRating>(3, new LongestPathWithoutEnemyHit(updater.Enemies)),
    //                    new Tuple<double, IQualityRating>(3, new EnemyCanRunToShootPoint(updater.Enemies)),
                    };                    
                }
            }
        }

        public bool CanUseThisBall(Point ballPredicted)
        {
            return true;
        }

        public Control PathsNotFound(Point myMe, Point myBall, ModelUpdater updater)
        {
            return DumbSubStrategy.MakeControl(myMe, myBall);
        }
        
        public string Name => "AttackerSubStrategy";
    }
}