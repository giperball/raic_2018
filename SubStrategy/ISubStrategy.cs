using System.Collections.Generic;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using ConsoleApp1.Common;
using ConsoleApp1.DebugHelpers;

namespace ConsoleApp1.SubStrategy
{
    public interface ISubStrategy
    {
        void Act(ModelUpdater updater);
        List<IDebugHelper> DebugHelpers { get; }
        string Name { get; }
    }
}