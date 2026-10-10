using System.Collections;
using System.Runtime.InteropServices;

namespace Expat;

public readonly unsafe ref struct XmlAttributesReader(nint atts, int count) : IEnumerable<XmlAttributesReader.XmlAttribute>
{
    readonly nint* _atts = (nint*)atts;
    readonly int _count = count;

    public readonly bool HasAttributes => _count > 0;

    public readonly struct XmlAttribute(nint name, nint value)
    {
        public ReadOnlySpan<byte> Name
            => MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)name);

        public ReadOnlySpan<byte> Value
            => MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)value);
    }

    public struct XmlAttributeEnumerator(nint* atts, int count) : IEnumerator<XmlAttribute>
    {
        XmlAttribute _current;

        int _index = -2;

        public void Reset()
        {
            _index = -2;
        }

        void IDisposable.Dispose()
        {
            _current = default;
        }

        public bool MoveNext()
        {
            if (count == 0) return false;

            _index += 2;

            if (_index >= count) return false;

            _current = new(atts[_index], atts[_index + 1]);

            return true;
        }

        readonly public XmlAttribute Current => _current;

        readonly object IEnumerator.Current => _current;
    }

    public readonly XmlAttributeEnumerator GetEnumerator() => new(_atts, _count);

    readonly IEnumerator<XmlAttribute> IEnumerable<XmlAttribute>.GetEnumerator() => GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
