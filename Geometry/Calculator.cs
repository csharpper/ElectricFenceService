using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Geometry
{
    public class Calculator
    {
        /// <summary>
        /// 判断多边形是否和一个矩形相交
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="rd"></param>
        /// <returns></returns>
        public static bool CalcIfPolygonIntersectRectangle(PolygonD pd, RectangleD rd)
        {
            if (!pd.Bounds.IntersectsWith(rd))
                return false;
            if (CalcPolygonIntersect(pd.Points[0].Points, rd).Length == 0)
                return false;
            return true;
        }

        /// <summary>
        /// 计算两个多边形是否相交。(未考虑“回”字型)
        /// </summary>
        /// <param name="pd1"></param>
        /// <param name="pd2"></param>
        /// <returns></returns>
        public static bool CalcIfPolygonsIntersect(PolygonD pd1, PolygonD pd2)
        {
            if (!pd1.Bounds.IntersectsWith(pd2.Bounds))
                return false;
            PointD[] plg1 = pd1.Points[0].Points;
            PointD[] plg2 = pd2.Points[0].Points;
            PointD start1, end1, start2, end2;
            for (int i = 0; i < plg1.Length; i++)
            {
                start1 = plg1[i];
                end1 = (i == plg1.Length - 1) ? plg1[0] : plg1[i + 1];
                for (int j = 0; j < plg2.Length; j++)
                {
                    start2 = plg2[j];
                    end2 = (j == plg2.Length - 1) ? plg2[0] : plg2[j + 1];
                    if (CrossLine(start1, end1, start2, end2) != 0)
                        return true;
                }
            }
            return CalcPolygonContainsPolygon(pd1, pd2) || CalcPolygonContainsPolygon(pd2, pd1);
        }

        /// <summary>
        /// 计算多边形和折线（包括线段）是否相交，（未考虑“回”字型）
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool CalcIfPolygonIntersectPolyline(PolygonD pd, PointDArray line)
        {
            if (!pd.Bounds.IntersectsWith(line.Bounds))
                return false;
            PointD[] plg1 = pd.Points[0].Points;
            PointD[] plg2 = line.Points;
            PointD start1, end1, start2, end2;
            for (int i = 0; i < plg1.Length; i++)
            {
                start1 = plg1[i];
                end1 = (i == plg1.Length - 1) ? plg1[0] : plg1[i + 1];
                for (int j = 0; j < plg2.Length - 1; j++)
                {
                    start2 = plg2[j];
                    end2 = plg2[j + 1];
                    if (CrossLine(start1, end1, start2, end2) != 0)
                        return true;
                }
            }
            //补充判断多边形完全包含折线或线段的情况
            if (PtInPolygon(line.Points[0].X, line.Points[0].Y, pd))
                return true;
            return false;
        }

        /// <summary>
        /// 计算点是否在多边形内(包括“回”字型)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="pd"></param>
        /// <returns></returns>
        public static bool PtInPolygon(double x, double y, PolygonD pd)
        {
            if (!pd.Bounds.Contains(x, y))
                return false;
            int count = 0;
            for (int i = 0; i < pd.Points.Count; i++)
            {
                if (ptInRgn(x, y, pd.Points[i].Points))
                    count++;
            }
            return count % 2 == 1;
        }

        /// <summary>
        /// 计算多边形(不含“回”字型)是否包含一个矩形
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="rd"></param>
        /// <returns></returns>
        public static bool CalcPolygonContainsRectangle(PolygonD pd, RectangleD rd)
        {
            if (!pd.Bounds.IntersectsWith(rd))
                return false;
            PointD[] rc = CalcPolygonIntersect(pd.Points[0].Points, rd);
            if (rc.Length > 2)
            {
                byte result = 0x00;
                for (int i = 0; i < rc.Length; i++)
                {
                    if ((rc[i].X != rd.Left && rc[i].X != rd.Right) || (rc[i].Y != rd.Top && rc[i].Y != rd.Bottom))
                        return false;
                    if (rc[i].X == rd.Left && rc[i].Y == rd.Top)
                        result |= 0x01;
                    if (rc[i].X == rd.Right && rc[i].Y == rd.Top)
                        result |= 0x02;
                    if (rc[i].X == rd.Right && rc[i].Y == rd.Bottom)
                        result |= 0x04;
                    if (rc[i].X == rd.Left && rc[i].Y == rd.Bottom)
                        result |= 0x08;
                }
                if (result == 0x0f)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public static bool CalcPolygonContainsRectangle2(PolygonD pd, RectangleD rd)
        {
            bool result = CalcRegionContainsRectangle2(pd.Points[0], rd);
            if (pd.Points.Count > 1 && result)
            {
                for (int i = 1; i < pd.Points.Count; i++)
                {
                    if (CalcRegionContainsRectangle2(pd.Points[i], rd) || calcIfPolygonIntersect(pd.Points[i].Points, rd))//CalcPolygonIntersect(pd.Points[i].Points,rd).Length>0)
                        return false;
                }
                return true;
            }
            else
                return result;
        }

        private static bool calcIfPolygonIntersect(PointD[] pts, RectangleD rect)
        {
            if (calcIfEdgeIntersect(pts, new PointD(rect.Left, rect.Top), new PointD(rect.Right, rect.Top)))
                return true;
            if (calcIfEdgeIntersect(pts, new PointD(rect.Right, rect.Top), new PointD(rect.Right, rect.Bottom)))
                return true;
            if (calcIfEdgeIntersect(pts, new PointD(rect.Right, rect.Bottom), new PointD(rect.Left, rect.Bottom)))
                return true;
            if (calcIfEdgeIntersect(pts, new PointD(rect.Left, rect.Bottom), new PointD(rect.Left, rect.Top)))
                return true;
            return false;
        }

        private static bool calcIfEdgeIntersect(PointD[] src, PointD start, PointD end)
        {
            List<PointD> list = new List<PointD>();

            if (src.Length > 2)
            {
                PointD s = src[src.Length - 1];
                bool bi = inside(s, start, end);
                for (int i = 0; i < src.Length; i++)
                {
                    PointD p = src[i];
                    //if (inside(s, start, end))
                    if (bi)
                    {
                        bi = inside(p, start, end);
                        if (!bi)
                            return true;
                    }
                    else
                    {
                        bi = inside(p, start, end);
                        if (bi)
                            return true;
                    }
                    s = p;
                }
            }
            return false;
        }

        public static bool CalcRegionContainsRectangle2(PointDArray pda, RectangleD rd)
        {
            if (!pda.Bounds.IntersectsWith(rd))
                return false;
            PointD[] rc = Calculator.CalcPolygonIntersect(pda.Points, rd);
            if (rc.Length > 2)
            {
                byte result = 0x00;
                for (int i = 0; i < rc.Length; i++)
                {
                    if ((rc[i].X != rd.Left && rc[i].X != rd.Right) || (rc[i].Y != rd.Top && rc[i].Y != rd.Bottom))
                        return false;
                    if (rc[i].X == rd.Left && rc[i].Y == rd.Top)
                        result |= 0x01;
                    if (rc[i].X == rd.Right && rc[i].Y == rd.Top)
                        result |= 0x02;
                    if (rc[i].X == rd.Right && rc[i].Y == rd.Bottom)
                        result |= 0x04;
                    if (rc[i].X == rd.Left && rc[i].Y == rd.Bottom)
                        result |= 0x08;
                }
                if (result == 0x0f)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 计算多边形(不含“回”字型)是否包含另一个多边形
        /// </summary>
        /// <param name="regionSource"></param>
        /// <param name="regionTarget"></param>
        /// <returns></returns>
        public static bool CalcRegionContainsRegion(PointDArray regionSource, PointDArray regionTarget)
        {
            if (!regionSource.Bounds.IntersectsWith(regionSource.Bounds))
                return false;
            for (int i = 0; i < regionTarget.Points.Length; i++)
            {
                PointD start = regionTarget.Points[i];
                PointD end = (i == regionTarget.Points.Length - 1) ? regionTarget.Points[0] : regionTarget.Points[i + 1];
                if (!CalcRegionContainsLine(regionSource.Points, start, end))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 计算多边形(含“回”字型)是否包含另一个多边形
        /// </summary>
        /// <param name="pdOutter"></param>
        /// <param name="pdInner"></param>
        /// <returns></returns>
        public static bool CalcPolygonContainsPolygon(PolygonD pdOutter, PolygonD pdInner)
        {
            if (!pdOutter.Bounds.IntersectsWith(pdInner.Bounds))
                return false;
            if (!CalcRegionContainsRegion(pdOutter.Points[0], pdInner.Points[0]))
                return false;
            if (pdOutter.Points.Count > 1)
            {
                for (int i = 1; i < pdOutter.Points.Count; i++)
                {
                    if (CalcRegionContainsRegion(pdInner.Points[0], pdOutter.Points[i]))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 计算多边形(不含“回”字型)是否完全包含一条线段
        /// </summary>
        /// <param name="region"></param>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <returns></returns>
        public static bool CalcRegionContainsLine(PointD[] region, PointD pStart, PointD pEnd)
        {
            if (!ptInRgn(pStart.X, pStart.Y, region) || !ptInRgn(pEnd.X, pEnd.Y, region))
            {
                return false;
            }
            PointD[] line = new PointD[] { pStart, pEnd };
            List<PointD> tempPoints = new List<PointD>();
            for (int i = 0; i < region.Length; i++)
            {
                PointD start = region[i];
                PointD end = (i == region.Length - 1) ? region[0] : region[i + 1];
                PointD[] edge = new PointD[] { start, end };
                bool b1 = ptInRgn(pStart.X, pStart.Y, edge);
                bool b2 = ptInRgn(pEnd.X, pEnd.Y, edge);
                if (b1 || b2)
                {
                    if (b1)
                        tempPoints.Add(pStart);
                    if (b2)
                        tempPoints.Add(pEnd);
                }
                else
                {
                    b1 = ptInRgn(start.X, start.Y, line);
                    b2 = ptInRgn(end.X, end.Y, line); //8.23
                    if (b1 || b2)
                    {
                        if (b1)
                            tempPoints.Add(start);
                        if (b2)
                            tempPoints.Add(end);
                    }
                    else if (CrossLine(pStart, pEnd, start, end) != 0)
                        return false;
                }
            }
            tempPoints.Sort(new Comparison<PointD>(sortPointD));
            for (int i = 0; i < tempPoints.Count; i++)
            {
                PointD start = tempPoints[i];
                PointD end = (i == tempPoints.Count - 1) ? tempPoints[0] : tempPoints[i + 1];
                if (!ptInRgn((start.X + end.X) / 2, (start.Y + end.Y) / 2, region))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// PointD排序，从小到大，先按x排序，x相同再按y。
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private static int sortPointD(PointD p1, PointD p2)
        {
            int result = p1.X.CompareTo(p2.X);
            if (result == 0)
                result = p1.Y.CompareTo(p2.Y);
            return result;
        }

        /// <summary>
        /// 判断两条线是否相交
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="e1"></param>
        /// <param name="s2"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static int CrossLine(PointD s1, PointD e1, PointD s2, PointD e2)
        {
            bool b = (Math.Min(s1.X, e1.X) <= Math.Max(s2.X, e2.X) &&
                Math.Min(s2.X, e2.X) <= Math.Max(s1.X, e1.X) &&
                Math.Min(s1.Y, e1.Y) <= Math.Max(s2.Y, e2.Y) &&
                Math.Min(s2.Y, e2.Y) <= Math.Max(s1.Y, e1.Y) &&
                (MutliplyLine(s1.X, s1.Y, e2.X, e2.Y, s2.X, s2.Y) * MutliplyLine(e1.X, e1.Y, e2.X, e2.Y, s2.X, s2.Y) <= 0) &&
                (MutliplyLine(s2.X, s2.Y, e1.X, e1.Y, s1.X, s1.Y) * MutliplyLine(e2.X, e2.Y, e1.X, e1.Y, s1.X, s1.Y) <= 0));
            if (!b)
                return 0;
            if (MutliplyLine(s1.X, s1.Y, e2.X, e2.Y, s2.X, s2.Y) >= 0)
                return 1;
            else
                return -1;
        }

        public static double MutliplyLine(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double xx1 = x1 - x3;
            double yy1 = y1 - y3;
            double xx2 = x2 - x3;
            double yy2 = y2 - y3;
            return xx1 * yy2 - xx2 * yy1;
        }

        public static bool CalcIfPolygonIntersect(PolygonD pd1, PolygonD pd2)
        {
            return pd1.Bounds.IntersectsWith(pd2.Bounds);
        }

        public static PointD[] CalcPolygonIntersect(PointD[] pts, RectangleD rect)
        {
            PointD[] pd = subPolygonByEdge(pts, new PointD(rect.Left, rect.Top), new PointD(rect.Right, rect.Top));
            pd = subPolygonByEdge(pd, new PointD(rect.Right, rect.Top), new PointD(rect.Right, rect.Bottom));
            pd = subPolygonByEdge(pd, new PointD(rect.Right, rect.Bottom), new PointD(rect.Left, rect.Bottom));
            pd = subPolygonByEdge(pd, new PointD(rect.Left, rect.Bottom), new PointD(rect.Left, rect.Top));
            return pd;
        }


        public static List<PointD[]> CalcPolylineIntersect(PointD[] pts, RectangleD rect)
        {
            List<PointD[]> list = new List<PointD[]>();
            list.Add(pts);
            list = subPolylineByEdge(list, new PointD(rect.Left, rect.Top), new PointD(rect.Right, rect.Top));
            list = subPolylineByEdge(list, new PointD(rect.Right, rect.Top), new PointD(rect.Right, rect.Bottom));
            list = subPolylineByEdge(list, new PointD(rect.Right, rect.Bottom), new PointD(rect.Left, rect.Bottom));
            list = subPolylineByEdge(list, new PointD(rect.Left, rect.Bottom), new PointD(rect.Left, rect.Top));
            return list;
        }

        /// <summary>
        /// 计算重心
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static PointD CalcFocus(PointD[] pts)
        {
            //if (pts.Length == 1)
            //    return pts[0];
            //else if (pts.Length == 2)
            //    return new PointD((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2);
            double area = 0;
            PointD center = new PointD(0, 0);

            for (int i = 0; i < pts.Length - 1; i++)
            {
                area += (pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y) / 2;
                center.X += (pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y) * (pts[i].X + pts[i + 1].X);
                center.Y += (pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y) * (pts[i].Y + pts[i + 1].Y);
            }

            int n = pts.Length;
            area += (pts[n - 1].X * pts[0].Y - pts[0].X * pts[n - 1].Y) / 2;
            center.X += (pts[n - 1].X * pts[0].Y - pts[0].X * pts[n - 1].Y) * (pts[n - 1].X + pts[0].X);
            center.Y += (pts[n - 1].X * pts[0].Y - pts[0].X * pts[n - 1].Y) * (pts[n - 1].Y + pts[0].Y);

            if (area == 0)
            {
                double minX = pts[0].X;
                double minY = pts[0].Y;
                double maxX = 0;
                double maxY = 0;
                for (int i = 0; i < pts.Length; i++)
                {
                    if (pts[i].X < minX)
                        minX = pts[i].X;
                    else if (pts[i].X > maxX)
                        maxX = pts[i].X;
                    if (pts[i].Y < minY)
                        minY = pts[i].Y;
                    else if (pts[i].Y > maxY)
                        maxY = pts[i].Y;
                }
                center.X = (minX + maxX) / 2;
                center.Y = (minY + maxY) / 2;
            }
            else
            {

                center.X /= 6 * area;
                center.Y /= 6 * area;
            }
            if (Double.IsInfinity(center.X) || Double.IsInfinity(center.Y))
                return new PointD();
            return center;
        }

        /// <summary>
        /// 判断点是否在一个区域内，此函数为内部调用函数，不再对区域的外接矩形进行判断。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private static bool ptInRgn(double x, double y, PointD[] region)
        {
            if (region.Length == 0)
                return false;
            int count = 0;
            PointD start, end;
            for (int i = 0; i < region.Length; i++)
            {
                start = region[i];
                end = (i == region.Length - 1) ? region[0] : region[i + 1];
                if (start.X == x && start.Y == y) //是顶点
                    return true;
                else
                {
                    if (start.Y == y)  //在X轴上
                    {
                        if (start.X > x)   //在正半轴
                        {
                            if (end.Y == y)    //边终点也在X轴上
                            {
                                if (end.X < x) //点在边上
                                    return true;
                                //否则该边可以当作一个顶点，忽略
                            }
                            else
                            {
                                if (end.Y < y)
                                    count++;
                                //左算又不算
                            }
                        }
                        else//在负半轴上
                        {
                            if (end.Y == y && end.X > x)  //点在边上
                                return true;
                        }
                    }
                    else
                    {
                        if (end.Y == y)    //边终点在X轴上
                        {
                            if (end.X > x && start.Y < y)
                                count++;
                        }
                        else
                        {
                            if (start.Y < y && end.Y < y)
                                continue;
                            if (start.Y > y && end.Y > y)
                                continue;
                            if (start.X == end.X)
                            {
                                if (start.X > x)
                                    count++;
                            }
                            else
                            {
                                double x1 = (end.X * (start.Y - y) + start.X * (y - end.Y)) / (start.Y - end.Y);
                                if (x1 == x)
                                    return true;
                                else if (x1 > x)
                                    count++;
                            }
                        }
                    }
                }
            }
            return (count & 1) != 0;
        }

        public static bool PtInRgn(double x, double y, PointD[] region)
        {
            if (region.Length == 0)
                return false;

            double l = region[0].X;
            double r = l;
            double t = region[0].Y;
            double b = t;
            for (int i = 1; i < region.Length; i++)
            {
                if (region[i].X > r)
                    r = region[i].X;
                else
                {
                    if (region[i].X < l)
                        l = region[i].X;
                }
                if (region[i].Y > b)
                    b = region[i].Y;
                else
                {
                    if (region[i].Y < t)
                        t = region[i].Y;
                }
            }
            //RectangleD rd = new RectangleD(l, t, r - l, b - t);
            RectangleD rd = RectangleD.FromLTRB(l, t, r, b);

            if (!rd.Contains(x, y))
                return false;

            int count = 0;
            PointD start, end;
            for (int i = 0; i < region.Length; i++)
            {
                start = region[i];
                end = (i == region.Length - 1) ? region[0] : region[i + 1];
                if (start.X == x && start.Y == y) //是顶点
                    return true;
                else
                {
                    if (start.Y == y)  //在X轴上
                    {
                        if (start.X > x)   //在正半轴
                        {
                            if (end.Y == y)    //边终点也在X轴上
                            {
                                if (end.X < x) //点在边上
                                    return true;
                                //否则该边可以当作一个顶点，忽略
                            }
                            else
                            {
                                if (end.Y < y)
                                    count++;
                                //左算又不算
                            }
                        }
                        else//在负半轴上
                        {
                            if (end.Y == y && end.X > x)  //点在边上
                                return true;
                        }
                    }
                    else
                    {
                        if (end.Y == y)    //边终点在X轴上
                        {
                            if (end.X > x && start.Y < y)
                                count++;
                        }
                        else
                        {
                            if (start.Y < y && end.Y < y)
                                continue;
                            if (start.Y > y && end.Y > y)
                                continue;
                            if (start.X == end.X)
                            {
                                if (start.X > x)
                                    count++;
                            }
                            else
                            {
                                double x1 = (end.X * (start.Y - y) + start.X * (y - end.Y)) / (start.Y - end.Y);
                                if (x1 == x)
                                    return true;
                                else if (x1 > x)
                                    count++;
                            }
                        }
                    }
                }
            }
            return (count & 1) != 0;
        }

        private static PointD[] subPolygonByEdge(PointD[] src, PointD start, PointD end)
        {
            List<PointD> list = new List<PointD>();

            if (src.Length > 2)
            {
                PointD s = src[src.Length - 1];
                bool bi = inside(s, start, end);
                for (int i = 0; i < src.Length; i++)
                {
                    PointD p = src[i];
                    //if (inside(s, start, end))
                    if (bi)
                    {
                        bi = inside(p, start, end);
                        //if (inside(p, start, end))
                        if (bi)
                            list.Add(p);
                        else
                            list.Add(getEdgePoint(s, p, start, end));
                    }
                    else
                    {
                        bi = inside(p, start, end);
                        //if (inside(p, start, end))
                        if (bi)
                        {
                            list.Add(getEdgePoint(s, p, start, end));
                            list.Add(p);
                        }
                    }
                    s = p;
                }
            }
            return list.ToArray();
        }

        private static List<PointD[]> subPolylineByEdge(List<PointD[]> src, PointD start, PointD end)
        {
            List<PointD[]> list = new List<PointD[]>();
            foreach (PointD[] pd in src)
            {
                if (pd.Length == 0)
                    continue;
                List<PointD> lp = new List<PointD>();
                PointD s = pd[0];
                for (int i = 1; i < pd.Length; i++)
                {
                    PointD p = pd[i];
                    if (inside(s, start, end))
                    {
                        if (lp.Count == 0)
                            lp.Add(s);
                        if (inside(p, start, end))
                            lp.Add(p);
                        else
                        {
                            lp.Add(getEdgePoint(s, p, start, end));
                            list.Add(lp.ToArray());
                            lp.Clear();
                        }
                    }
                    else if (inside(p, start, end))
                    {
                        lp.Add(getEdgePoint(s, p, start, end));
                        lp.Add(p);
                    }
                    s = p;
                }
                if (lp.Count > 0)
                    list.Add(lp.ToArray());
            }
            return list;
        }

        private static bool inside(PointD pt, PointD start, PointD end)
        {
            if (end.X > start.X)    //窗口上边
            {
                if (pt.Y >= start.Y)
                    return true;
            }
            else if (end.X < start.X)   //窗口下边
            {
                if (pt.Y <= start.Y)
                    return true;
            }
            else if (end.Y > start.Y)   //窗口右边
            {
                if (pt.X <= start.X)
                    return true;
            }
            else if (end.Y < start.Y)   //窗口左边
            {
                if (pt.X >= start.X)
                    return true;
            }
            return false;
        }

        private static PointD getEdgePoint(PointD s, PointD p, PointD start, PointD end)
        {
            PointD pt = new PointD();
            if (start.X == end.X)
            {
                pt.X = start.X;
                pt.Y = s.Y + (p.Y - s.Y) * (pt.X - s.X) / (p.X - s.X);
            }
            else
            {
                pt.Y = start.Y;
                pt.X = s.X + (pt.Y - s.Y) * (p.X - s.X) / (p.Y - s.Y);
            }
            return pt;
        }

        #region Format & Parse Longitude/Latitude

        protected static string FormatValue(double value)
        {
            string s = "";
            int du, fen, miao, hm;
            int c = (int)(value * 360000 + 0.5);
            du = c / 360000;
            fen = (c % 360000) / 6000;
            miao = (c % 6000) / 100;
            hm = c % 100;
            return s + du.ToString() + "°" + fen.ToString("D02") + "\'" + miao.ToString("D02") + "." + hm.ToString("D02") + "\"";
        }

        public static string FormatLongitude(double value)
        {
            return FormatValue(Math.Abs(value)) + ((value < 0) ? "W" : "E");
        }

        public static string FormatLatitude(double value)
        {
            return FormatValue(Math.Abs(value)) + ((value < 0) ? "S" : "N");
        }

        public static double ParseLongitude(string value)
        {
            if (value == null || value == string.Empty)
                return Double.NaN;
            double d = 1;
            string s;
            if (value[value.Length - 1] != 'E' && value[value.Length - 1] != 'W')
                s = value;
            else
            {
                s = value.Substring(0, value.Length - 1);
                d = value[value.Length - 1] == 'E' ? 1 : -1; ;
            }
            double v = ParseValue(s);
            if (Double.IsNaN(v) || v > 180)
                return Double.NaN;
            else
                return v * d;
        }

        public static double ParseLatitude(string value)
        {
            if (value == null || value == string.Empty)
                return Double.NaN;
            double d = 1;
            string s;
            if (value[value.Length - 1] != 'N' && value[value.Length - 1] != 'S')
                s = value;
            else
            {
                s = value.Substring(0, value.Length - 1);
                d = value[value.Length - 1] == 'N' ? 1 : -1; ;
            }
            double v = ParseValue(s);
            if (Double.IsNaN(v) || v > 90)
                return Double.NaN;
            else
                return v * d;
        }

        private static double distancePointToPoint(PointD a, PointD b)
        {
            double d;
            double dx, dy;
            dx = a.X - b.X;
            dy = a.Y - b.Y;
            d = Math.Sqrt(dx * dx + dy * dy);
            return (d);
        }


        protected static double ParseValue(string value)
        {
            string[] ps = value.Split(new string[] { "°", "\'", "′", "\"", "″", "-" }, StringSplitOptions.RemoveEmptyEntries);
            if (ps.Length < 1)
                return Double.NaN;
            double du = 0, fen = 0, miao = 0;
            if (!Double.TryParse(ps[0], out du))
                return Double.NaN;
            if (ps.Length > 1 && !Double.TryParse(ps[1], out fen) || fen >= 60)
                return Double.NaN;
            if (ps.Length > 2)
            {
                bool dot = false;
                if (ps[2][0] == '.')
                {
                    ps[2] = ps[2].Substring(1, ps[2].Length - 1);
                    dot = true;
                }
                if (value[value.Length - 1] != '\"' && value[value.Length - 1] != '″')
                    dot = true;
                if (!Double.TryParse(ps[2], out miao))
                    return Double.NaN;
                if (dot)
                    miao *= 0.6;
                if (miao >= 60)
                    return Double.NaN;
            }
            return (du + fen / 60F + miao / 3600);
        }

        #endregion

        /// <summary>
        /// 计算折线中心
        /// </summary>
        /// <param name="ptds"></param>
        /// <returns></returns>
        public static PointD CalcCenterOfPolyline(PointD[] ptds)
        {
            double distance = 0;
            List<double> listDistance = new List<double>();
            for (int i = 1; i < ptds.Length; i++)
            {
                double temp = distancePointToPoint(ptds[i - 1], ptds[i]);
                distance += temp;
                listDistance.Add(temp);
            }
            distance /= 2;
            int listCount = listDistance.Count;
            int index = 0;
            for (; index < listCount; index++)
            {
                distance -= listDistance[index];
                if (distance == 0)
                    return ptds[index + 1];
                else if (distance < 0)
                    break;
            }
            distance = Math.Abs(distance);
            PointD center = new PointD();
            double scale = distance / listDistance[index];//相似三角形
            center.X = ptds[index + 1].X - (ptds[index + 1].X - ptds[index].X) * scale;
            center.Y = ptds[index + 1].Y - (ptds[index + 1].Y - ptds[index].Y) * scale;
            return center;
        }

        public static PointD CalcCenterOfPolyline(List<PointD> ptds)
        {
            double distance = 0;
            List<double> listDistance = new List<double>();
            int ptdsCount = ptds.Count;
            for (int i = 1; i < ptdsCount; i++)
            {
                double temp = distancePointToPoint(ptds[i - 1], ptds[i]);
                distance += temp;
                listDistance.Add(temp);
            }
            distance /= 2;
            int listCount = listDistance.Count;
            int index = 0;
            for (; index < listCount; index++)
            {
                distance -= listDistance[index];
                if (distance == 0)
                    return ptds[index + 1];
                else if (distance < 0)
                    break;
            }
            distance = Math.Abs(distance);
            PointD center = new PointD();
            double scale = distance / listDistance[index];//相似三角形
            center.X = ptds[index + 1].X - (ptds[index + 1].X - ptds[index].X) * scale;
            center.Y = ptds[index + 1].Y - (ptds[index + 1].Y - ptds[index].Y) * scale;
            return center;
        }

        public static double DistancePointToSegment(PointD start, PointD end, PointD pt)
        {
            if (start.Equals(end))
                return CalcDis(start, pt);
            PointD ab = new PointD(end.X - start.X, end.Y - start.Y);
            PointD ac = new PointD(pt.X - start.X, pt.Y - start.Y);
            double f = ab.X * ac.X + ab.Y * ac.Y;
            f /= (start.X - end.X) * (start.X - end.X) + (start.Y - end.Y) * (start.Y - end.Y);

            if (f < 0)
                return CalcDis(start, pt);
            if (f > 1)
                return CalcDis(end, pt);
            PointD d = ab;
            d.X = d.X * f + start.X;
            d.Y = d.Y * f + start.Y;
            return CalcDis(d, pt);
        }

        /// <summary>
        /// 精确，慢
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public static double CalcDis(PointD pt1, PointD pt2)
        {
            double x1 = Math.Cos(pt1.Y * Math.PI / 180) * Math.Sin(pt1.X * Math.PI / 180);
            double y1 = Math.Cos(pt1.Y * Math.PI / 180) * Math.Cos(pt1.X * Math.PI / 180);
            double z1 = Math.Sin(pt1.Y * Math.PI / 180);
            double x2 = Math.Cos(pt2.Y * Math.PI / 180) * Math.Sin(pt2.X * Math.PI / 180);
            double y2 = Math.Cos(pt2.Y * Math.PI / 180) * Math.Cos(pt2.X * Math.PI / 180);
            double z2 = Math.Sin(pt2.Y * Math.PI / 180);
            double d = x1 * x2 + y1 * y2 + z1 * z2;
            if (d > 1)
                d = 1;
            return Math.Acos(d) * 180 / Math.PI * 60;
        }
    }
}
