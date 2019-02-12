namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class MyGame
    {
        private int _meScore;
        private int _enemyScore;
        private int _lastGoalTick;
        static public int CurrentTick { get; private set; }
        public int MaxTick;

        public bool IsGoalReset => (LastGoalTick != 0 && CurrentTick - LastGoalTick < (int) Constants.RESET_TICKS - 1);
        
        public bool IsNewRound => CurrentTick == 0 || 
                                  (LastGoalTick != 0 && CurrentTick == LastGoalTick + (int) Constants.RESET_TICKS - 1);

        public int LastGoalTick
        {
            get { return _lastGoalTick; }
        }

        public int MeScore
        {
            get { return _meScore; }
            set { _meScore = value; }
        }

        public int EnemyScore
        {
            get { return _enemyScore; }
            set { _enemyScore = value; }
        }

        public void UpdateScore(int me, int enemy, int tick)
        {
            if (MeScore != me || EnemyScore != enemy)
                _lastGoalTick = tick;
            MeScore = me;
            EnemyScore = enemy;
            CurrentTick = tick;
        }
        
    }
}