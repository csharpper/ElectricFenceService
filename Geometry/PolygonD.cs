using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Geometry
{
    [Serializable]
    /// <summary>
    /// double的多边形集合,包括回字形
    /// </summary>
    public class PolygonD
    {
        /// <summary>
        /// </summary>
        private RectangleD _bounds = RectangleD.Empty;
        public RectangleD Bounds
        {
            get { return _bounds; }
        }

        /// <summary>
        /// 多边形列表
        /// </summary>
        private List<PointDArray> _points = new List<PointDArray>();

        public ReadOnlyCollection<PointDArray> Points
        {
            get
            {
                return new ReadOnlyCollection<PointDArray>(_points);
            }
        }

        /// <summary>
        /// </summary>
        public void AddPoints(PointDArray points)
        {
            _points.Add(points);
            if (_bounds.IsEmpty)
                _bounds = points.Bounds;
            else
                _bounds.Union(points.Bounds);
        }
    }
}
