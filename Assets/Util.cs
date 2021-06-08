using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;


namespace NinjaSchool
{
	public static class Util
	{
		public static async UniTask Move_deprecated(this Transform transform, Vector3 dest, float speed, CancellationToken token = default, int delay = 0)
		{
			throw new NotSupportedException();
			//token.ThrowIfCancellationRequested();
			//while (transform.position != dest)
			//{
			//	transform.position = Vector3.MoveTowards(transform.position, dest, speed);
			//	await UniTask.Delay(delay, cancellationToken: token);
			//}
			//transform.position = dest;
		}


		public static async UniTask Move(this Transform transform, Vector3 velocity, int delay)
		{
			transform.position += velocity;
			await UniTask.Delay(delay);
		}


		/// <summary>
		/// Kiểm tra tất cả trường hợp kể cả point nằm trên cạnh của <see cref="Rect"/>
		/// </summary>
		public static bool TrueContains(this in Rect rect, in Vector2 point) =>
			point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;


		/// <summary>
		/// Đưa <paramref name="point"/> vào trong <paramref name="rect"/>
		/// </summary>
		public static void Clamp(this in Rect rect, ref Vector2 point) =>
			point.Set(Mathf.Clamp(point.x, rect.xMin, rect.xMax),
				Mathf.Clamp(point.y, rect.yMin, rect.yMax));


		/// <summary>
		/// <c> <paramref name="v"/>.z = 0 </c>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Set(this ref Vector3 v, float newX, float newY) => v.Set(newX, newY, 0);


		#region UniTask
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool isRunning(this in UniTask task) => task.Status == UniTaskStatus.Pending;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool isRunning<T>(this in UniTask<T> task) => task.Status == UniTaskStatus.Pending;


		/// <summary>
		/// Bắt <see cref="Exception"/> ngoại trừ <see cref="OperationCanceledException"/><br/>
		/// Không <see langword="await"/> <paramref name="task"/>, đảm bảo luôn kiểm tra được status của <paramref name="task"/>
		/// </summary>
		public static void Forget(this in UniTask task)
		{
			tasks.Add(task);
			if (tasks.Count == 1) Forget();
		}


		private static readonly List<UniTask> tasks = new List<UniTask>(), tmp = new List<UniTask>();
		private static async void Forget()
		{
			while (true)
			{
				tmp.Clear();
				foreach (var task in tasks)
					if (!task.isRunning())
						if (task.Status == UniTaskStatus.Faulted) await task;
						else tmp.Add(task);

				foreach (var task in tmp) tasks.Remove(task);
				if (tasks.Count == 0) break;
				await UniTask.Yield();
			}
		}


		/// <summary>
		/// Bắt <see cref="Exception"/> ngoại trừ <see cref="OperationCanceledException"/><br/>
		/// Không <see langword="await"/> <paramref name="task"/>, đảm bảo luôn kiểm tra được status của <paramref name="task"/>
		/// </summary>
		public static void Forget<T>(this in UniTask<T> task)
		{
			GenericTasks<T>.tasks.Add(task);
			if (GenericTasks<T>.tasks.Count == 1) GenericTasks<T>.Forget();
		}


		private static class GenericTasks<T>
		{
			public static readonly List<UniTask<T>> tasks = new List<UniTask<T>>();
			private static readonly List<UniTask<T>> tmp = new List<UniTask<T>>();


			public static async void Forget()
			{
				while (true)
				{
					tmp.Clear();
					foreach (var task in tasks)
						if (!task.isRunning())
							if (task.Status == UniTaskStatus.Faulted) await task;
							else tmp.Add(task);

					foreach (var task in tmp) tasks.Remove(task);
					if (tasks.Count == 0) break;
					await UniTask.Yield();
				}
			}
		}
		#endregion


		#region Global Dict
		private static readonly Dictionary<string, object> dict = new Dictionary<string, object>();

		public static bool TryGetValue<TValue>(this string key, out TValue value)
		{
			bool result = dict.TryGetValue(key, out object v);
			value = (TValue)v;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TValue GetValue<TValue>(this string key) => (TValue)dict[key];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ContainsKey(this string key) => dict.ContainsKey(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Remove(this string key) => dict.Remove(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this string key, object value) => dict[key] = value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearAllKeys() => dict.Clear();
		#endregion


		#region Converts
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3Int ToVector3Int(this in Vector2Int value) => new Vector3Int(value.x, value.y, 0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2Int ToVector2Int(this in Vector3Int value) => new Vector2Int(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 ToVector2(this in Vector3Int value) => new Vector2(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ToVector3(this in Vector2Int value) => new Vector3(value.x, value.y);

#if !DEBUG
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static Vector3Int ToVector3Int(this in Vector3 value) =>
#if DEBUG
				value.x < 0 || value.y < 0 || value.z < 0 ? throw new IndexOutOfRangeException($"value= {value} phải là tọa độ không âm !") :
#endif
			new Vector3Int((int)value.x, (int)value.y, (int)value.z);
		#endregion


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json);
	}



	public readonly struct ReadOnlyArray<T> : IEnumerable<T>
	{
		private readonly T[] array;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlyArray(T[] array) => this.array = array;


		public T this[int index] => array[index];

		public int Length => array.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => (array as IEnumerable<T>).GetEnumerator();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => array.GetEnumerator();
	}



	[Serializable]
	public sealed class ObjectPool<T> : IEnumerable<T> where T : Component
	{
		[SerializeField] private T prefab;
		[SerializeField] private Transform usingAnchor, freeAnchor;
		[SerializeField] private List<T> free = new List<T>();
		private readonly List<T> @using = new List<T>();


		private ObjectPool() { }


		public ObjectPool(T prefab, Transform freeAnchor = null, Transform usingAnchor = null)
		{
			this.prefab = prefab;
			this.freeAnchor = freeAnchor;
			this.usingAnchor = usingAnchor;
		}


		/// <param name="active">Active gameObject ngay lập tức ?</param>
		public T Get(Vector3 position = default, bool active = true)
		{
			T item;
			if (free.Count != 0)
			{
				item = free[0];
				free.RemoveAt(0);
			}
			else item = UnityEngine.Object.Instantiate(prefab);

			item.transform.parent = usingAnchor;
			@using.Add(item);
			item.transform.position = position;
			item.gameObject.SetActive(active);
			return item;
		}


		public void Recycle(T item)
		{
			item.gameObject.SetActive(false);
			item.transform.parent = freeAnchor;
			@using.Remove(item);
			free.Add(item);
		}


		public void Recycle()
		{
			for (int i = 0; i < @using.Count; ++i)
			{
				var item = @using[i];
				item.gameObject.SetActive(false);
				item.transform.parent = freeAnchor;
				free.Add(item);
			}
			@using.Clear();
		}


		public void DestroyGameObject(T item)
		{
			@using.Remove(item);
			UnityEngine.Object.Destroy(item.gameObject);
		}


		public void DestroyGameObject()
		{
			foreach (var item in @using) UnityEngine.Object.Destroy(item.gameObject);
			@using.Clear();
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => @using.GetEnumerator();


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerator<T> GetEnumerator() => (@using as IEnumerable<T>).GetEnumerator();
	}



	/// <summary>
	/// Giống như vector nhưng không có độ dài (magnitude)<br/>
	/// Ứng dụng: lưu giá trị rời rạc 8 hướng
	/// </summary>
	[Serializable]
	[DataContract]
	public struct Direction : IEquatable<Direction>
#if UNITY_EDITOR
			, ISerializationCallbackReceiver
#endif
	{
		#region Fields
		public enum Name
		{
			_0 = 0, Up, RightUp, Right, RightDown, Down, LeftDown, Left, LeftUp
		}
		[SerializeField] private Name m_Name;

		public enum Value
		{
			_0 = 0, Positive = 1, Negative = -1
		}
		[SerializeField] private Value m_X, m_Y;


#if UNITY_EDITOR
		private Name lastName;
		private Value lastX, lastY;

		public void OnBeforeSerialize()
		{
			lastName = name;
			lastX = x;
			lastY = y;
		}


		public void OnAfterDeserialize()
		{
			if (name != lastName) name = name;
			if (x != lastX) x = x;
			if (y != lastY) y = y;
		}
#endif
		#endregion


		private static readonly IReadOnlyDictionary<Name, (Value, Value)> NAME_TO_XY = new Dictionary<Name, (Value, Value)>
		{
			[0] = (0, 0),
			[Name.Up] = (0, upValue),
			[Name.RightUp] = (rightValue, upValue),
			[Name.Right] = (rightValue, 0),
			[Name.RightDown] = (rightValue, downValue),
			[Name.Down] = (0, downValue),
			[Name.LeftDown] = (leftValue, downValue),
			[Name.Left] = (leftValue, 0),
			[Name.LeftUp] = (leftValue, upValue)
		};
		private static readonly IReadOnlyDictionary<Value, IReadOnlyDictionary<Value, Name>> XY_TO_NAME = new Dictionary<Value, IReadOnlyDictionary<Value, Name>>
		{
			[0] = new Dictionary<Value, Name>
			{
				[0] = 0,
				[upValue] = Name.Up,
				[downValue] = Name.Down
			},
			[leftValue] = new Dictionary<Value, Name>
			{
				[0] = Name.Left,
				[upValue] = Name.LeftUp,
				[downValue] = Name.LeftDown
			},
			[rightValue] = new Dictionary<Value, Name>
			{
				[0] = Name.Right,
				[upValue] = Name.RightUp,
				[downValue] = Name.RightDown
			}
		};


		public Value x
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_X;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => m_Name = XY_TO_NAME[m_X = value][m_Y];
		}


		public Value y
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Y;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => m_Name = XY_TO_NAME[m_X][m_Y = value];
		}


		[DataMember]
		public Name name
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Name;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => (m_X, m_Y) = NAME_TO_XY[m_Name = value];
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString() => $"({m_Name}, x= {m_X}, y= {m_Y})";


		private static readonly IReadOnlyDictionary<Name, Vector2> NAME_TO_VECTOR = new Dictionary<Name, Vector2>
		{
			[0] = Vector2.zero,
			[Name.Up] = Vector2.up,
			[Name.RightUp] = Vector2.right + Vector2.up,
			[Name.Right] = Vector2.right,
			[Name.RightDown] = Vector2.right + Vector2.down,
			[Name.Down] = Vector2.down,
			[Name.LeftDown] = Vector2.left + Vector2.down,
			[Name.Left] = Vector2.left,
			[Name.LeftUp] = Vector2.left + Vector2.up
		};

		public Vector2 ToVector2(float xMagnitude, float yMagnitude)
		{
#if DEBUG
			if (xMagnitude < 0 || yMagnitude < 0) throw new ArgumentOutOfRangeException();
#endif
			var result = NAME_TO_VECTOR[m_Name];
			result.x *= xMagnitude;
			result.y *= yMagnitude;
			return result;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Direction(in Value x, in Value y)
		{
			m_Name = XY_TO_NAME[m_X = x][m_Y = y];
#if UNITY_EDITOR
			lastName = default;
			lastX = lastY = default;
#endif
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Direction(in Name name)
		{
			(m_X, m_Y) = NAME_TO_XY[m_Name = name];
#if UNITY_EDITOR
			lastName = default;
			lastX = lastY = default;
#endif
		}


		public Direction(in Vector2 vector) : this(
			vector.x > 0 ? Value.Positive : vector.x < 0 ? Value.Negative : 0,
			vector.y > 0 ? Value.Positive : vector.y < 0 ? Value.Negative : 0)
		{ }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Direction(in Vector2 v) => new Direction(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Direction(in Name name) => new Direction(name);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Direction(in (Value x, Value y) value) => new Direction(value.x, value.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Name(in Direction direction) => direction.name;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator (Value x, Value y)(in Direction direction) => (direction.x, direction.y);


		public static readonly Direction up = new Direction(Name.Up),
			right = new Direction(Name.Right),
			down = new Direction(Name.Down),
			left = new Direction(Name.Left);

		public const Value upValue = Value.Positive, rightValue = Value.Positive,
			downValue = Value.Negative, leftValue = Value.Negative;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(in Value newX, in Value newY) => m_Name = XY_TO_NAME[m_X = newX][m_Y = newY];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Direction other) => m_Name == other.m_Name;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => m_Name.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(in Direction a, in Direction b) => a.m_Name == b.m_Name;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(in Direction a, in Direction b) => !(a == b);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => obj is Direction direction && Equals(direction);

		public static Direction operator +(in Direction a, in Direction b)
		{
			return new Direction(AddValue(a.x, b.x), AddValue(a.y, b.y));


			static Value AddValue(Value a, Value b) => a == b || b == 0 ? a : a == 0 ? b : 0;
		}
	}
}