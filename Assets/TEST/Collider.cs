using Cysharp.Threading.Tasks;
using NinjaSchool;
using NinjaSchool.Tilemaps;
using UnityEngine;
using UnityEngine.InputSystem;


public class Collider : MonoBehaviour, ICollider
{
	[field: SerializeField]
	public Vector2 size { get; private set; }
	[SerializeField] private Vector2 vMagnitude;
	private UniTask task;
	public bool continousInput;
	[SerializeField] private int delay;
	public GameObject image;
	public SpriteRenderer spriteRenderer;

	private void Update()
	{
		if (!Application.isFocused) return;
		var k = Keyboard.current;
		if (k.spaceKey.wasPressedThisFrame)
		{
			image.SetActive(!image.activeSelf);
			spriteRenderer.sortingOrder = spriteRenderer.sortingOrder == -1 ? 1 : -1;
		}
		if (task.isRunning()) return;

		var d = new Direction();
		if (continousInput)
		{
			d.x = k.leftArrowKey.isPressed || k.numpad7Key.isPressed || k.numpad4Key.isPressed || k.numpad1Key.isPressed ? Direction.Value.Negative
					: k.rightArrowKey.isPressed || k.numpad9Key.isPressed || k.numpad6Key.isPressed || k.numpad3Key.isPressed ? Direction.Value.Positive : 0;
			d.y = k.downArrowKey.isPressed || k.numpad1Key.isPressed || k.numpad2Key.isPressed || k.numpad3Key.isPressed ? Direction.Value.Negative
						: k.upArrowKey.isPressed || k.numpad7Key.isPressed || k.numpad8Key.isPressed || k.numpad9Key.isPressed ? Direction.Value.Positive : 0;
		}
		else
		{
			d.x = k.leftArrowKey.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame ? Direction.Value.Negative
					: k.rightArrowKey.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame || k.numpad6Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame ? Direction.Value.Positive : 0;
			d.y = k.downArrowKey.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame ? Direction.Value.Negative
					: k.upArrowKey.wasPressedThisFrame || k.numpad7Key.wasPressedThisFrame || k.numpad8Key.wasPressedThisFrame || k.numpad9Key.wasPressedThisFrame ? Direction.Value.Positive : 0;
		}

		if (d != default)
		{
			transform.position = Tilemap.FindMoveDestination(this, d.ToVector2(vMagnitude.x, vMagnitude.y));
			task = UniTask.Delay(delay);
		}
	}
}