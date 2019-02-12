using System.Collections.Generic;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using ConsoleApp1.Common;
using ConsoleApp1.DebugHelpers;

namespace ConsoleApp1.SubStrategy
{
    public class EmptySubStrategy: ISubStrategy
    {
        public List<IDebugHelper> DebugHelpers
        {
            get => null;
        }

        void ISubStrategy.Act(ModelUpdater updater)
        {
        }

        public string Name => "EmptySubStrategy";
    }
}