using System;

namespace Geometry
{
    [Serializable]
    /// <summary>
    /// double的二维点集合,并封装了最小外接矩形,由于最小外接矩形只在创建实例时计算,因此不应动态修改Points
    /// </summary>
    public class PointDArray
    {
        public PointD[] Points = null;

        /// <summary>
        /// 二维点序列的最小外接矩形
        /// </summary>
        public RectangleD Bounds;

        public PointDArray() { }

        /// <summary></summary>
        public PointDArray(PointD[] points)
        {
            Points = points;

            CalcBounds();
        }

        public PointDArray(PointD[] points, RectangleD bounds)
        {
            Points = points;
            Bounds = bounds;
        }

        public void CalcBounds()
        {
            if (Points == null || Points.Length == 0)
                Bounds = RectangleD.Empty;
            else
            {
                double l = Points[0].X, r = l;
                double t = Points[0].Y, b = t;
                for (int i = 1; i < Points.Length; i++)
                {
                    if (Points[i].X < l)
                        l = Points[i].X;
                    else if (Points[i].X > r)
                        r = Points[i].X;
                    if (Points[i].Y < t)
                        t = Points[i].Y;
                    else if (Points[i].Y > b)
                        b = Points[i].Y;
                }
                Bounds = RectangleD.FromLTRB(l, t, r, b);
            }
        }
    }
}