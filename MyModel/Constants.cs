namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class Constants
    {
        public const double ROBOT_MIN_RADIUS = 1;
        public const double ROBOT_MAX_RADIUS = 1.05D;
        public const double ROBOT_MAX_JUMP_SPEED = 15;
        public const double ROBOT_MAX_JUMP_HEIGHT = 4.79;
        public const double ROBOT_ACCELERATION = 100;
        public const double ROBOT_NITRO_ACCELERATION = 30;
        public const double ROBOT_MAX_GROUND_SPEED = 30;
        public const double ROBOT_MAX_GROUND_SPEED_PER_TICK = ROBOT_MAX_GROUND_SPEED / TICKS_PER_SECOND;
        public const double ROBOT_ARENA_E = 0;
        public const double ROBOT_RADIUS = 1;
        public const double ROBOT_MASS = 2;
        public const double TICKS_PER_SECOND = 60;
        public const double MICROTICKS_PER_TICK = 100;
        public const double MICROTICK_DELTA_TIME = 1.0 / (Constants.TICKS_PER_SECOND * Constants.MICROTICKS_PER_TICK);
        public const double TICK_DELTA_TIME = 1.0 / (Constants.TICKS_PER_SECOND);
        public const double RESET_TICKS = 2 * TICKS_PER_SECOND;
        public const double BALL_ARENA_E = 0.7D;
        public const double BALL_RADIUS = 2;
        public const double BALL_MASS = 1;
        public const double MIN_HIT_E = 0.4D;
        public const double MAX_HIT_E = 0.5D;
        public const double MAX_ENTITY_SPEED = 100;
        public const double MAX_NITRO_AMOUNT = 100;
        public const double START_NITRO_AMOUNT = 50;
        public const double NITRO_POINT_VELOCITY_CHANGE = 0.6D;
        public const double NITRO_PACK_X = 20;
        public const double NITRO_PACK_Y = 1;
        public const double NITRO_PACK_Z = 30;
        public const double NITRO_PACK_RADIUS = 0.5D;
        public const double NITRO_PACK_AMOUNT = 100;
        public const double NITRO_RESPAWN_TICKS = 10 * TICKS_PER_SECOND;
        public const double GRAVITY = 30;

    }
}