namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class MyRobot : Unit
    {
        public MyRobot()
        {
            mass = Constants.ROBOT_MASS;
            
        }
        
        public MyRobot(Vector3 position, Vector3 velocity, double radius, double radius_change_speed)
        {
            base.position = position;
            base.velocity = velocity;
            base.radius_change_speed = radius_change_speed;
            base.radius = radius;
            mass = Constants.ROBOT_MASS;
        }
        
        public int number = 0;
        public int id = 0;
        public bool isTeammate;
        public bool touch;
    }
}