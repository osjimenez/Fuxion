using System.Collections.Generic;
using System.Linq;
using Fuxion;
using Test.Dataset.Daos;

namespace Test.Dataset;

public class ActiveDataContextNotFoundException(string? activeDataContextName) : FuxionException($"Active data context named '{activeDataContextName}' doesn't exist");
public class TestDataContextCollection : IDictionary<string, ITestDataContext>, ITestDataContext
{
	// ITestDataContext implementation
	public string Name => "Collection of DataContexts";
	public string? ActiveDataContextName { get; set; }
	ITestDataContext? ActiveDataContext => ActiveDataContextName is not null && ContainsKey(ActiveDataContextName) ? this[ActiveDataContextName!] : null;

	public IQueryable<TDao> Get<TDao>() where TDao : class => ActiveDataContext?.Get<TDao>() ?? throw new ActiveDataContextNotFoundException(ActiveDataContextName);
	
	// Dictionary implementation
	private readonly Dictionary<string, ITestDataContext> dic = new();
	public ITestDataContext this[string key] { get => dic[key]; set => dic[key] = value; }
	public ICollection<string> Keys => dic.Keys;
	public ICollection<ITestDataContext> Values => dic.Values;
	public int Count => dic.Count;
	public bool IsReadOnly => false;
	public void Add(string key, ITestDataContext value) => dic.Add(key, value);
	public void Add(ITestDataContext value) => dic.Add(value.Name, value);
	public void Add(KeyValuePair<string, ITestDataContext> item) => dic.Add(item.Key, item.Value);
	public void Clear() => dic.Clear();
	public bool Contains(KeyValuePair<string, ITestDataContext> item) => dic.Contains(item);
	public bool ContainsKey(string key) => dic.ContainsKey(key);
	public void CopyTo(KeyValuePair<string, ITestDataContext>[] array, int arrayIndex)
	{
		throw new System.NotImplementedException();
	}
	public IEnumerator<KeyValuePair<string, ITestDataContext>> GetEnumerator() => dic.GetEnumerator();
	public bool Remove(string key) => dic.Remove(key);
	public bool Remove(KeyValuePair<string, ITestDataContext> item) => dic.Remove(item.Key);
	public bool TryGetValue(string key, out ITestDataContext value) => dic.TryGetValue(key, out value!);
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => dic.GetEnumerator();
}