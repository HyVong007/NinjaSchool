using UnityEngine;


namespace NinjaSchool
{
	/// <summary>
	/// Vật thể có khả năng va chạm với tilemap<br/>
	/// Vận tốc di chuyển: vx &lt;= 0.5 và vy &lt;= 0.5<br/>
	/// Pivot = (0.5; 0)
	/// </summary>
	public interface ICollider
	{
		public Transform transform { get; }

		public Vector2 size { get; }
	}
}