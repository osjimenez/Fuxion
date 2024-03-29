﻿#if false
using Fuxion.Json;
using Fuxion.Reflection;
using Fuxion.Threading;

namespace Fuxion.Application.Snapshots;

public class InMemorySnapshotStorage : ISnapshotStorage
{
	public InMemorySnapshotStorage(ITypeKeyResolver typeKeyResolver, string? dumpFilePath = null)
	{
		if (dumpFilePath != null)
		{
			this.dumpFilePath = new(dumpFilePath);
			this.dumpFilePath.Read(path => {
				if (File.Exists(path))
				{
					var dic = File.ReadAllText(path).DeserializeFromJson<Dictionary<TypeKey, List<JsonPod<TypeKey, Snapshot>>>>();
					if (dic == null) throw new FileLoadException($"File '{path}' cannot be deserializer for '{nameof(InMemorySnapshotStorage)}'");
					snapshots.WriteObject(dic.Select(k => (k.Key, Value: k.Value.Select(v => (Snapshot?)v.As(typeKeyResolver[k.Key])).RemoveNulls().ToDictionary(s => s.AggregateId)))
						.ToDictionary(a => a.Key, a => a.Value));
				}
			});
		}
	}
	readonly Locker<string>? dumpFilePath;
	readonly Locker<Dictionary<TypeKey, Dictionary<Guid, Snapshot>>> snapshots = new(new());
	public Task<Snapshot?> GetSnapshotAsync(Type snapshotType, Guid aggregateId) =>
		snapshots.ReadNullableAsync(dic => dic.ContainsKey(snapshotType.GetTypeKey()) && dic[snapshotType.GetTypeKey()].ContainsKey(aggregateId) ? dic[snapshotType.GetTypeKey()][aggregateId] : null);
	public async Task SaveSnapshotAsync(Snapshot snapshot)
	{
		var key = snapshot.GetType().GetTypeKey();
		await snapshots.WriteAsync(dic => {
			if (!dic.ContainsKey(key)) dic.Add(key, new());
			if (dic[key].ContainsKey(snapshot.AggregateId))
				dic[key][snapshot.AggregateId] = snapshot;
			else
				dic[key].Add(snapshot.AggregateId, snapshot);
		});
		if (dumpFilePath != null)
			await dumpFilePath.WriteAsync(path => {
				snapshots.Read(str => File.WriteAllText(path,
					str.ToDictionary(k => k.Value.First().Value.GetType().GetTypeKey(), k => k.Value.Select(e => new JsonPod<TypeKey, Snapshot>(e.Value.GetType().GetTypeKey(), e.Value))).SerializeToJson()));
			});
	}
}
#endif