using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using ConsoleApp1.Common;
using ConsoleApp1.Structs;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs
{
    public struct Point
    {
        public Vector3 velocity;
        public Vector3 position;
        public Control control;

        public Point(Unit unit)
        {
            velocity = unit.velocity;
            position = unit.position;
            control = new Control();
        }

        public Point(Vector3 position, Vector3 velocity)
        {
            this.velocity = velocity;
            this.position = position;
            this.control = new Control();
        }
        
        public Point(Vector3 position, Vector3 velocity, Control control)
        {
            this.velocity = velocity;
            this.position = position;
            this.control = control;
        }
    }
}