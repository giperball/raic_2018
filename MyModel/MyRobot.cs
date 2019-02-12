using System;
using ConsoleApp1.Common;
using ConsoleApp1.SubStrategy;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class MyRobot : Unit, ICloneable
    {
        public MyRobot()
        {
            mass = Constants.ROBOT_MASS;
            
        }
        
        public MyRobot(Vector3 position, Vector3 velocity, double radius, double radiusChangeSpeed, double nitroAmount)
        {
            base.position = position;
            base.velocity = velocity;
            base.radiusChangeSpeed = radiusChangeSpeed;
            base.radius = radius;
            this.nitroAmount = nitroAmount;
            mass = Constants.ROBOT_MASS;
        }
        
        public int id = 0;
        public bool isTeammate;
        public bool touch = false;
        public bool usedNitroOnLastTick = false;
        public Vector3 touchNormal;
        public double nitroAmount = 0;

        public object Clone()
        {
            var result = new MyRobot();
            result.position = position;
            result.velocity = velocity;
            result.radiusChangeSpeed = radiusChangeSpeed;
            result.radius = radius;
            result.id = id;
            result.isTeammate = isTeammate;
            result.touch = touch;
            result.touchNormal = touchNormal;
            result.nitroAmount = nitroAmount;
            return result;
        }
    }
}