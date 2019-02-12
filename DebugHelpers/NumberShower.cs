using System.Collections.Generic;
using ConsoleApp1.Common;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1.DebugHelpers
{
    public class NumberShower : IDebugHelper
    {
        private readonly int _number;
        private readonly Vector3 _point;
        private readonly double _pointerLength;
        private readonly double _digitHeight;
        private readonly double _digitWidth;
        private readonly double _linesWidth;
        private readonly Color _color;

        public NumberShower(int number, Vector3 point, Color color, double pointerLength = 3, double digitHeight = 2, double digitWidth = 1, double linesWidth = 2.5)
        {
            _number = number;
            _point = point;
            _color = color;
            _pointerLength = pointerLength;
            _digitHeight = digitHeight;
            _digitWidth = digitWidth;
            _linesWidth = linesWidth;
        }

        JObject MakeLine(Vector3 a, Vector3 b)
        {
            return new LineShower(a, b, _color, _linesWidth).MakeJObject();
        }

        List<JObject> MakeDigit(int digit, Vector3 lowBottomPoint)
        {
            var a1 = lowBottomPoint + new Vector3(0, _digitHeight, 0);;
            var b1 = lowBottomPoint + new Vector3(0, _digitHeight / 2, 0);
            var c1 = lowBottomPoint + new Vector3(0, 0, 0);;
            var a2 = lowBottomPoint + new Vector3(_digitWidth, _digitHeight, 0);;;
            var b2 = lowBottomPoint + new Vector3(_digitWidth, _digitHeight / 2, 0);
            var c2 = lowBottomPoint + new Vector3(_digitWidth, 0, 0);

            if (digit == 0)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, b1),
                    MakeLine(b1, c1),
                    MakeLine(c1, c2),
                    MakeLine(a1, a2),
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                };
            }
            if (digit == 1)
            {
                return new List<JObject>()
                {
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                };
            }
            if (digit == 2)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, a2),
                    MakeLine(a2, b2),
                    MakeLine(b2, b1),
                    MakeLine(b1, c1),
                    MakeLine(c1, c2),
                };
            }
            if (digit == 3)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, a2),
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                    MakeLine(c2, c1),
                    MakeLine(b1, b2),
                };
            }
            if (digit == 4)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, b1),
                    MakeLine(b1, b2),
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                };
            }
            if (digit == 5)
            {
                return new List<JObject>()
                {
                    MakeLine(a2, a1),
                    MakeLine(a1, b1),
                    MakeLine(b1, b2),
                    MakeLine(b2, c2),
                    MakeLine(c2, c1),
                };
            }
            if (digit == 6)
            {
                return new List<JObject>()
                {
                    MakeLine(a2, a1),
                    MakeLine(a1, b1),
                    MakeLine(b1, c1),
                    MakeLine(c1, c2),
                    MakeLine(c2, b2),
                    MakeLine(b2, b1),
                };
            }
            if (digit == 7)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, a2),
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                };
            }
            if (digit == 8)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, a2),
                    MakeLine(b1, b2),
                    MakeLine(c1, c2),
                    MakeLine(a1, b1),
                    MakeLine(b1, c1),
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                };
            }
            if (digit == 9)
            {
                return new List<JObject>()
                {
                    MakeLine(a1, a2),
                    MakeLine(b1, b2),
                    MakeLine(c1, c2),
                    MakeLine(a1, b1),
                    MakeLine(a2, b2),
                    MakeLine(b2, c2),
                };
            }
            return new List<JObject>();
        }
        
        public List<JObject> MakeJObjects()
        {
            const double spacing = 0.5;
            List<JObject> result = new List<JObject>();
            
            result.Add(MakeLine(_point, _point + new Vector3(0, _pointerLength, 0)));

            int n = _number;
            var point = _point + new Vector3(-_digitWidth, _pointerLength + spacing, 0);
            if (n == 0)
                result.AddRange(MakeDigit(0, point));
            else
            {
                while (n > 0)
                {
                    result.AddRange(MakeDigit(n % 10, point));
                    n /= 10;
                    point.x -= spacing + _digitWidth;
                }
            }

            return result;
        }

        public JObject MakeJObject()
        {
            return null;
        }
    }
}