using System;

namespace Geometry
{
    [Serializable]
    /// <summary>
    /// double的二维坐标
    /// </summary>
    public struct PointD
    {
        public static readonly PointD Empty;

        public double X;
        public double Y;

        public PointD(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj is PointD)
            {
                PointD pd = (PointD)obj;
                return (this.X == pd.X && this.Y == pd.Y);
            }
            else
            {
                return false;
            }
        }

        public bool IsEmpty()
        {
            return this.Equals(PointD.Empty);
        }

        public static PointD Add(PointD pt, SizeD size)
        {
            return new PointD(pt.X + size.Width, pt.Y + size.Height);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static PointD operator +(PointD pt, SizeD size)
        {
            return PointD.Add(pt, size);
        }

        public static bool operator ==(PointD pd1, PointD pd2)
        {
            return pd1.Equals(pd2);
        }

        public static bool operator !=(PointD pd1, PointD pd2)
        {
            return !pd1.Equals(pd2);
        }

        public static PointD Subtract(PointD pt, SizeD size)
        {
            return new PointD(pt.X - size.Width, pt.Y - size.Height);
        }

        public static PointD operator -(PointD pt, SizeD size)
        {
            return PointD.Subtract(pt, size);
        }

        public override string ToString()
        {
            return "{X=" + this.X.ToString() + ", Y=" + this.Y.ToString() + "}";
        }
    }
}