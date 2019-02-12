using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.ThirdParty;
using ConsoleApp1.Common;
using ConsoleApp1.Logic;
using ConsoleApp1.Structs;

namespace ConsoleApp1.SubStrategy
{
    public class DefenderExtension : IBaseSubstrategyExtension
    {
        private Vector3 _standbyPoint = new Vector3(0, Constants.ROBOT_RADIUS, -MyArena.half_of_field * 0.8);

        public int TicksToFuture => 100;

        public string Name
        {
            get { return "DefenderSubStrategy"; }
        }


        public List<Tuple<double, IQualityRating>> GetQualities(List<Path> paths, ModelUpdater updater)
        {
            return new List<Tuple<double, IQualityRating>>()
            {
//                new Tuple<double, IQualityRating>(0.1, new EnemyFieldDirectionQuality()),
//                new Tuple<double, IQualityRating>(3, new FarrestFromGatesWithoutEnemyHit(updater.Enemies)),
                new Tuple<double, IQualityRating>(3, new LongestZOnEnemyHit(updater.Enemies)),
//                new Tuple<double, IQualityRating>(3, new FastestShootQuality()),
//                new Tuple<double, IQualityRating>(3, new FarrestOurGatesQuality()),
            }; 
        }

        public bool CanUseThisBall(Point ballPredicted)
        {
            if (ballPredicted.position.z > -MyArena.half_of_field * 0.3)
                return false;
            return true;
        }

        public Control PathsNotFound(Point myMe, Point myBall, ModelUpdater updater)
        {
            if (updater.myMe.nitroAmount < Constants.START_NITRO_AMOUNT)
            {
                var packs = updater.NitroPacks.Where(p => p.Position.z < 0);
                if (packs.Any())
                {
                    var clothestNitroPack = packs.MinBy(q => q.Position.DistanceTo(myMe.position));
                    if (!clothestNitroPack.RespawnTicks.HasValue)
                    {
                        return new Control(MoveCalculator.TargetToVelocity(myMe.position,
                            clothestNitroPack.Position.DropY(Constants.ROBOT_RADIUS)));
                    }
                }
            }
            
            if (_standbyPoint.DistanceTo(myMe.position) > 5)
                return new Control(MoveCalculator.TargetToVelocity(myMe.position, _standbyPoint));
            else
            {
                if(myMe.velocity.magnitude > 1)
                    return new Control(-myMe.velocity);
            }
            return new Control();
        }
    }
}