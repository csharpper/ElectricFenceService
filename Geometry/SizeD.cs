using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Geometry
{
    [Serializable]
    /// <summary>
    /// 存储有序双精度浮点数对，通常为矩形的宽度和高度
    /// </summary>
    public struct SizeD
    {
        public static readonly SizeD Empty;

        private double _width;
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        private double _height;
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        public bool IsEmpty
        {
            get { return this.Equals(SizeD.Empty); }
        }

        public SizeD(double width, double height)
        {
            _width = width;
            _height = height;
        }

        public static SizeD Add(SizeD sz1, SizeD sz2)
        {
            return new SizeD(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        public override bool Equals(object obj)
        {
            if (obj is SizeD)
            {
                SizeD sz = (SizeD)obj;
                return _width == sz.Width && _height == sz.Height;
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static SizeD operator +(SizeD sz1, SizeD sz2)
        {
            return SizeD.Add(sz1, sz2);
        }

        public static bool operator ==(SizeD sz1, SizeD sz2)
        {
            return sz1.Equals(sz2);
        }

        public static bool operator !=(SizeD sz1, SizeD sz2)
        {
            return !sz1.Equals(sz2);
        }

        public static SizeD operator -(SizeD sz1, SizeD sz2)
        {
            return SizeD.Subtract(sz1, sz2);
        }

        public static SizeD Subtract(SizeD sz1, SizeD sz2)
        {
            return new SizeD(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        public override string ToString()
        {
            return "{Width=" + _width.ToString() + ", Height=" + _height.ToString() + "}";
        }
    }
}
