using System;
using System.Collections.Generic;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    public interface IBaseSubstrategyExtension
    {
        int TicksToFuture {get;}
        string Name { get; }
        List<Tuple<double, IQualityRating>> GetQualities(List<Path> paths, ModelUpdater updater);
        bool CanUseThisBall(Point ballPredicted);

        Control PathsNotFound(Point myMe, Point myBall, ModelUpdater updater);
//        void SaveBetterActions(RobotPathActionsPredict actions);
    }
}