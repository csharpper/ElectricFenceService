using System;

namespace Geometry
{

    [Serializable]
    public struct RectangleD
    {
        public static readonly RectangleD Empty;

        public double Left;
        public double Top;
        public double Right;
        public double Bottom;

        public bool IsEmpty
        {
            get
            {
                return Left == 0 && Top == 0 && Right == 0 && Bottom == 0;
            }
        }

        public double Width
        {
            get { return Right - Left; }
        }

        public double Height
        {
            get { return Bottom - Top; }
        }

        public RectangleD(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Right = left + width;
            Bottom = top + height;
        }

        public static RectangleD FromLTRB(double left, double top, double right, double bottom)
        {
            RectangleD rect = new RectangleD();
            rect.Left = left;
            rect.Top = top;
            rect.Right = right;
            rect.Bottom = bottom;
            return rect;
        }

        public static RectangleD Intersect(RectangleD r1, RectangleD r2)
        {
            if (r1.Left > r2.Right || r1.Right < r2.Left || r1.Top > r2.Bottom || r1.Bottom < r2.Top)
                return RectangleD.Empty;
            return RectangleD.FromLTRB(Math.Max(r1.Left, r2.Left), Math.Max(r1.Top, r2.Top), Math.Min(r1.Right, r2.Right), Math.Min(r1.Bottom, r2.Bottom));
        }

        public bool IntersectsWith(RectangleD rect)
        {
            return !RectangleD.Intersect(this, rect).IsEmpty;
        }

        public bool Contains(double x, double y)
        {
            return x >= this.Left && x <= this.Right && y >= this.Top && y <= this.Bottom;
        }

        public bool Contains(PointD pt)
        {
            return Contains(pt.X, pt.Y);
        }

        public bool Contains(RectangleD rect)
        {
            return Contains(rect.Left, rect.Top) && Contains(rect.Right, rect.Top) && Contains(rect.Right, rect.Bottom) && Contains(rect.Left, rect.Bottom);
        }

        public void Union(PointD pd)
        {
            if (Left > pd.X)
                Left = pd.X;
            if (Right < pd.X)
                Right = pd.X;
            if (Top > pd.Y)
                Top = pd.Y;
            if (Bottom < pd.Y)
                Bottom = pd.Y;
        }

        public void Union(RectangleD rect)
        {
            if (Left > rect.Left)
                Left = rect.Left;
            if (Right < rect.Right)
                Right = rect.Right;
            if (Top > rect.Top)
                Top = rect.Top;
            if (Bottom < rect.Bottom)
                Bottom = rect.Bottom;
        }

        public static RectangleD Union(RectangleD rect1, RectangleD rect2)
        {
            RectangleD rc = new RectangleD();
            rc.Left = Math.Min(rect1.Left, rect2.Left);
            rc.Top = Math.Min(rect1.Top, rect2.Top);
            rc.Right = Math.Max(rect1.Right, rect2.Right);
            rc.Bottom = Math.Max(rect1.Bottom, rect2.Bottom);
            return rc;
        }

        public PointDArray ToPointDArray() //wyj, 8.24
        {
            PointD[] pd = new PointD[4];
            pd[0] = new PointD(Left, Top);
            pd[1] = new PointD(Right, Top);
            pd[2] = new PointD(Right, Bottom);
            pd[3] = new PointD(Left, Bottom);
            return new PointDArray(pd);
        }
    }
}