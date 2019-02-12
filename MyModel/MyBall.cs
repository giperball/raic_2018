using ConsoleApp1.Common;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class MyBall : Unit
    {
        public MyBall(Vector3 position, Vector3 velocity)
        {
            base.position = position;
            base.velocity = velocity;
            radius = Constants.BALL_RADIUS;
            mass = Constants.BALL_MASS;
        }
        
        public MyBall()
        {
            radius = Constants.BALL_RADIUS;
            mass = Constants.BALL_MASS;
        }
    }
}