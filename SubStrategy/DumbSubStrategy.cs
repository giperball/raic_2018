using System.Collections.Generic;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using ConsoleApp1.DebugHelpers;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    public class DumbSubStrategy : ISubStrategy
    {
        public static Control MakeControl(Point myMe, Point myBall)
        {
            return new Control(MoveCalculator.TargetToVelocityOnGround(myMe.position, myBall.position), 
                myMe.position.DistanceTo(myBall.position) < Constants.ROBOT_RADIUS + Constants.BALL_RADIUS
                    ? Constants.ROBOT_MAX_JUMP_SPEED : 0);
        }
        
        public void Act(ModelUpdater updater)
        {
            updater.Action(MakeControl(new Point(updater.myMe), new Point(updater.myBall)));
        }

        public List<IDebugHelper> DebugHelpers { get; }
        public string Name => "DumbSubStrategy";
    }
}