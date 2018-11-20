﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.Linq;

namespace Fuxion.Json
{
	public static class JsonPodExtensions
	{
		public static JsonPod<TPayload, TKey> ToJsonPod<TPayload, TKey>(this TPayload me, TKey key) where TPayload : class => new JsonPod<TPayload, TKey>(me, key);
		public static JsonPod<TPayload, TKey> FromJsonPod<TPayload, TKey>(this string me) where TPayload : class => me.FromJson<JsonPod<TPayload, TKey>>();
	}
	public class JsonPod<TPayload, TKey>
	{
		[JsonConstructor]
		protected JsonPod() { }
		public JsonPod(TPayload payload, TKey key)
		{
			PayloadKey = key;
			Payload = payload;
			PayloadJRaw = new JRaw(payload.ToJson());
		}
		[JsonProperty]
		public TKey PayloadKey { get; private set; }
		JRaw _PayloadJRaw;
		[JsonProperty(PropertyName = nameof(Payload))]
		internal JRaw PayloadJRaw
		{
			get => _PayloadJRaw;
			set
			{
				_PayloadJRaw = value;
				if (Payload == null)
				{
					bool wasFailed = false;
					var res  = PayloadJRaw.Value.ToString().FromJson<TPayload>(new JsonSerializerSettings
					{
						Error = delegate (object sender, ErrorEventArgs args)
						{
							wasFailed = true;
							args.ErrorContext.Handled = true;
						}
					});
					if (!wasFailed)
						Payload = res;
				}
			}
		}
		[JsonIgnore]
		public bool PayloadHasValue { get; private set; }
		TPayload _Payload;
		[JsonIgnore]
		public TPayload Payload {
			get => _Payload;
			private set
			{
				_Payload = value;
				PayloadHasValue = true;
			}
		}

		public static implicit operator TPayload(JsonPod<TPayload, TKey> payload)
		{
			return payload.Payload;
		}

		public JsonPod<T, TKey> CastWithPayload<T>() => new JsonPod<T, TKey>(PayloadJRaw.Value.ToString().FromJson<T>(), PayloadKey);

		public T As<T>() => PayloadJRaw.Value.ToString().FromJson<T>();
		public object As(Type type) => PayloadJRaw.Value.ToString().FromJson(type);

		public bool Is<T>() => Is(typeof(T));
		public bool Is(Type type)
		{
			try
			{
				As(type);
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("" + ex.Message);
				return false;
			}
		}
	}
}