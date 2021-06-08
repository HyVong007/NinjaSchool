using System;
using UnityEngine;


namespace NinjaSchool
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Animator))]
	public sealed class AnimEvent : MonoBehaviour
	{
		[Flags]
		public enum CallbackMode
		{
			Instance = 1 << 0,
			Static = 1 << 1
		}

		[Tooltip("Instance: chỉ instance event được gọi. Static: chỉ static event được gọi.")]
		public CallbackMode mode = CallbackMode.Instance;


		#region Events
		public static event Action<AnimEvent, string> onSEvent_String;
		public event Action<string> onEvent_String;
		public void OnEvent_String(string value)
		{
			if ((mode & CallbackMode.Instance) != 0) onEvent_String?.Invoke(value);
			if ((mode & CallbackMode.Static) != 0) onSEvent_String?.Invoke(this, value);
		}

		public static event Action<AnimEvent, int> onSEvent_Int;
		public event Action<int> onEvent_Int;
		public void OnEvent_Int(int value)
		{
			if ((mode & CallbackMode.Instance) != 0) onEvent_Int?.Invoke(value);
			if ((mode & CallbackMode.Static) != 0) onSEvent_Int?.Invoke(this, value);
		}

		public static event Action<AnimEvent, float> onSEvent_Float;
		public event Action<float> onEvent_Float;
		public void OnEvent_Float(float value)
		{
			if ((mode & CallbackMode.Instance) != 0) onEvent_Float?.Invoke(value);
			if ((mode & CallbackMode.Static) != 0) onSEvent_Float?.Invoke(this, value);
		}

		public static event Action<AnimEvent, UnityEngine.Object> onSEvent_Object;
		public event Action<UnityEngine.Object> onEvent_Object;
		public void OnEvent_Object(UnityEngine.Object value)
		{
			if ((mode & CallbackMode.Instance) != 0) onEvent_Object?.Invoke(value);
			if ((mode & CallbackMode.Static) != 0) onSEvent_Object?.Invoke(this, value);
		}

		public static event Action<AnimEvent, AnimationEvent> onSEvent_AnimationEvent;
		public event Action<AnimationEvent> onEvent_AnimationEvent;
		public void OnEvent_AnimationEvent(AnimationEvent value)
		{
			if ((mode & CallbackMode.Instance) != 0) onEvent_AnimationEvent?.Invoke(value);
			if ((mode & CallbackMode.Static) != 0) onSEvent_AnimationEvent?.Invoke(this, value);
		}
		#endregion
	}
}