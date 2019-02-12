using ConsoleApp1.Common;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Model
{
    public class MyNitroPack
    {
        public int Id;
        public Vector3 Position;
        public double Radius = Constants.NITRO_PACK_RADIUS;
        public double NitroAmount = Constants.NITRO_PACK_AMOUNT;
        public int? RespawnTicks;

        public MyNitroPack(int id, Vector3 position, double radius, double nitroAmount, int? respawnTicks)
        {
            this.Id = id;
            Position = position;
            Radius = radius;
            NitroAmount = nitroAmount;
            RespawnTicks = respawnTicks;
        }
    }
}