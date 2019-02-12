using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.Common;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class ModelUpdater
    {
        public MyBall myBall = new MyBall();
        public MyRobot myMe = new MyRobot();
        public MyGame myGame = new MyGame();
        public Dictionary<int, MyRobot> myRobots = new Dictionary<int, MyRobot>();
        public StrategyChooser chooser;
        public Dictionary<int, Control> LastUsedControl = new Dictionary<int, Control>();
        public List<MyRobot> LastTickRobots = new List<MyRobot>();
        public List<MyNitroPack> NitroPacks = new List<MyNitroPack>();

        public List<MyRobot> Teammates
        {
            get { return myRobots.Values.Where(r => r.isTeammate).ToList(); }
        }
        
        public List<MyRobot> NotMeRobots
        {
            get { return myRobots.Values.Where(r => r.id != myMe.id).ToList(); }
        }
        
        public MyRobot Teammate
        {
            get { return Teammates.Where(q => q.id != myMe.id).MinBy(r => (r.position - myMe.position).magnitude); }
        }
        
        public List<MyRobot> Enemies
        {
            get { return myRobots.Values.Where(r => !r.isTeammate).ToList(); }
        }

        public Action Action1
        {
            get { return _action; }
        }


        private Action _action;

        private static void UpdateRobot(MyRobot myRobot, Robot robot, MyRobot oldRobot)
        {
            myRobot.position = new Vector3((double)robot.x,(double)robot.y,(double)robot.z);
            myRobot.velocity = new Vector3((double)robot.velocity_x,(double)robot.velocity_y,(double)robot.velocity_z);
            myRobot.radius = (double)robot.radius;
            myRobot.radiusChangeSpeed = MoveCalculator.JumpSpeedFromRadius(robot.radius);
            myRobot.id = robot.id;
            myRobot.isTeammate = robot.is_teammate;
            myRobot.touch = robot.touch;
            myRobot.nitroAmount = robot.nitro_amount;
            if(robot.touch_normal_x.HasValue && 
               robot.touch_normal_y.HasValue &&
               robot.touch_normal_z.HasValue)
                myRobot.touchNormal = new Vector3(robot.touch_normal_x.Value, robot.touch_normal_y.Value, robot.touch_normal_z.Value);
            if (oldRobot != null)
                myRobot.usedNitroOnLastTick = robot.nitro_amount < oldRobot.nitroAmount;
        }

        public void Update(Robot me, Rules rules, Game game, Action action, StrategyChooser chooser = null)
        {
            this.chooser = chooser;
            _action = action;
            
            myBall.position = new Vector3((double)game.ball.x,(double)game.ball.y,(double)game.ball.z);
            myBall.velocity = new Vector3((double)game.ball.velocity_x,(double)game.ball.velocity_y,(double)game.ball.velocity_z);
            myBall.radius = (double)game.ball.radius;
            myGame.MaxTick = rules.max_tick_count;
            
            NitroPacks.Clear();
            foreach (var nitroPack in game.nitro_packs)
            {
                NitroPacks.Add(new MyNitroPack(nitroPack.id,
                    new Vector3(
                        nitroPack.x, nitroPack.y, nitroPack.z),
                    nitroPack.radius,
                    nitroPack.nitro_amount,
                    nitroPack.respawn_ticks));
            }
            
            LastTickRobots.Clear();
            foreach (var robotsValue in myRobots.Values)
            {
                LastTickRobots.Add((MyRobot)robotsValue.Clone());
            }
            
            myGame.UpdateScore(game.players.First(p => p.me).score, game.players.First(p => !p.me).score, game.current_tick);
            
            
            UpdateRobot(myMe, me, LastTickRobots.FirstOrDefault(r => r.id == me.id));

            foreach (var robot in game.robots)
            {
                if(!myRobots.ContainsKey(robot.id))
                    myRobots[robot.id] = new MyRobot();
                UpdateRobot(myRobots[robot.id], robot, LastTickRobots.FirstOrDefault(r => r.id == robot.id));
            }

        }

        public void Action(Vector3 targetVelocity, double jumpSpeed = 0, bool nitro = false)
        {
            Action(new Control(targetVelocity, jumpSpeed, nitro));
        }

        public void Action(Control control)
        {          
            LastUsedControl[myMe.id] = control;
            
            Action1.target_velocity_x = control.TargetVelocity.x;
            Action1.target_velocity_y = control.TargetVelocity.y;
            Action1.target_velocity_z = control.TargetVelocity.z;
            Action1.jump_speed = control.JumpSpeed;
            Action1.use_nitro = control.Nitro;
        }
    }
}