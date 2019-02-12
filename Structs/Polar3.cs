using System;
using ConsoleApp1.Common;

namespace Com.CodeGame.CodeBall2018.DevKit.CSharpCgdk.Structs
{
    public struct Polar3
    {
        public double r;
        public double tetta;
        public double phi;
        private Vector3 _decart;

        public Polar3(double r, double tetta, double phi)
        {
            this.r = r;
            this.tetta = tetta;
            this.phi = phi;
            _decart = Vector3.zero;
        }

        public Vector3 ToDecart()
        {
            if (_decart == Vector3.zero)
            {
                double sin_tetta = Math.Sin(tetta);
                double cos_tetta = Math.Cos(tetta);
                double sin_phi = Math.Sin(phi);
                double cos_phi = Math.Cos(phi);
                
                _decart = new Vector3(
                    r * sin_tetta * cos_phi,
                    r * cos_tetta,
                    r * sin_tetta * sin_phi
                );                
            }

            return _decart;
            
        }
//        public static Polar3 FromDecart(Vector3 vector)
//        {
//            return new Polar3(
//                r * Math.Sin(tetta) * Math.Cos(phi),
//                r * Math.Cos(tetta),
//                r * Math.Sin(tetta) * Math.Sin(phi)
//            );
//        }
    }

}