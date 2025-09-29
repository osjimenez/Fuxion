using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fuxion.Math.Graph;

[DebuggerDisplay("{" + nameof(Value) + "}")]
public class Vertex<T>()
{
	public Vertex(T value) : this() => Value = value;
	public Vertex(IEnumerable<Vertex<T>> dependencies) : this() => Dependencies = dependencies.ToList();
	public Vertex(T value, IEnumerable<Vertex<T>> dependencies) : this(dependencies) => Value = value;
	internal int Index { get; set; } = -1;
	internal int LowLink { get; set; }
	public T Value { get; set; } = default!;
	public ICollection<Vertex<T>> Dependencies { get; set; } = new List<Vertex<T>>();
}