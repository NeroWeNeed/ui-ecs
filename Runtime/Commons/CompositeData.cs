using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS {
    public interface ICompositeData { }
    public interface ICompositeData2 : ICompositeData { }
    public interface ICompositeData3 : ICompositeData { }
    public interface ICompositeData4 : ICompositeData { }
    public interface ICompositeData2<TValue> : ICompositeData2 where TValue : struct {
        public TValue X { get; set; }
        public TValue Y { get; set; }
    }
    public interface ICompositeData3<TValue> : ICompositeData3 where TValue : struct {
        public TValue X { get; set; }
        public TValue Y { get; set; }
        public TValue Z { get; set; }
    }
    public interface ICompositeData4<TValue> : ICompositeData4 where TValue : struct {
        public TValue X { get; set; }
        public TValue Y { get; set; }
        public TValue Z { get; set; }
        public TValue W { get; set; }
    }

    public struct CompositeData2<TValue> : ICompositeData2<TValue> where TValue : struct {
        private TValue x, y;
        public TValue X { get => x; set => x = value; }
        public TValue Y { get => y; set => y = value; }
        public override bool Equals(object obj) {
            return obj is CompositeData2<TValue> data &&
                   EqualityComparer<TValue>.Default.Equals(x, data.x) &&
                   EqualityComparer<TValue>.Default.Equals(y, data.y);
        }

        public override int GetHashCode() {
            int hashCode = -1965440868;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            return $"CompositeData2<{typeof(TValue).Name}>(X: {x}, Y: {y})";
        }
    }
    public struct CompositeData3<TValue> : ICompositeData3<TValue> where TValue : struct {
        private TValue x, y, z;
        public TValue X { get => x; set => x = value; }
        public TValue Y { get => y; set => y = value; }
        public TValue Z { get => z; set => z = value; }
        public override bool Equals(object obj) {
            return obj is CompositeData3<TValue> data &&
                   EqualityComparer<TValue>.Default.Equals(x, data.x) &&
                   EqualityComparer<TValue>.Default.Equals(y, data.y) &&
                   EqualityComparer<TValue>.Default.Equals(z, data.z);
        }

        public override int GetHashCode() {
            int hashCode = -1965440868;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            return $"CompositeData3<{typeof(TValue).Name}>(X: {x}, Y: {y}, Z: {z})";
        }
    }
    public struct CompositeData4<TValue> : ICompositeData4<TValue> where TValue : struct {
        private TValue x, y, z, w;
        public TValue X { get => x; set => x = value; }
        public TValue Y { get => y; set => y = value; }
        public TValue Z { get => z; set => z = value; }
        public TValue W { get => w; set => w = value; }

        public override bool Equals(object obj) {
            return obj is CompositeData4<TValue> data &&
                   EqualityComparer<TValue>.Default.Equals(x, data.x) &&
                   EqualityComparer<TValue>.Default.Equals(y, data.y) &&
                   EqualityComparer<TValue>.Default.Equals(z, data.z) &&
                   EqualityComparer<TValue>.Default.Equals(w, data.w);
        }

        public override int GetHashCode() {
            int hashCode = -1965440868;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            hashCode = hashCode * -1521134295 + w.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            return $"CompositeData4<{typeof(TValue).Name}>(X: {x}, Y: {y}, Z: {z}, W: {w})";
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CompositeNameAttribute : Attribute {
        public string xName, yName, zName, wName;
        public bool Prefix { get; set; } = true;
        public CompositeNameAttribute(string xName = null, string yName = null, string zName = null, string wName = null) {
            this.xName = xName;
            this.yName = yName;
            this.zName = zName;
            this.wName = wName;
        }
    }
}